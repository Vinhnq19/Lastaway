"""PC entrypoint for private GHCR releases; no Start/Stop, Git writes or secret logging."""
from __future__ import annotations
import argparse
import base64
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import secrets
import shlex
import shutil
import socket
import subprocess
import sys
import tarfile
import tempfile
import time
import threading
import urllib.parse
import urllib.request
import urllib.error
from registry_release import (PACKAGES, RELEASE_ID, SOURCE, build_hash, client_source_hash, editor_version, fingerprint,
    read_json, required_tier, run, seal, sha, tree_hash, validate_manifest, validate_metrics, verify, write_json)

ROOT = Path(__file__).resolve().parents[2]
BACKEND = ROOT / 'Backend'
DEPLOY = BACKEND / 'Deploy'
PS = 'powershell.exe' if os.name == 'nt' else 'pwsh'


def credential():
    """Docker Desktop credential helper -> memory only, never model output or manifest."""
    cfg = read_json(Path(os.environ.get('DOCKER_CONFIG', str(Path.home() / '.docker'))) / 'config.json')
    helper = cfg.get('credHelpers', {}).get('ghcr.io', cfg.get('credsStore', ''))
    if not re.fullmatch(r'[a-zA-Z0-9_-]+(?:\.exe)?', helper): raise RuntimeError('Docker credential store required; run setup-ghcr.ps1 on PC')
    result = subprocess.run(['docker-credential-' + helper, 'get'], input='ghcr.io\n', text=True,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=30)
    if result.returncode: raise RuntimeError('GHCR login missing/expired; run setup-ghcr.ps1')
    value = json.loads(result.stdout)
    return value['Secret']


def package_private(service, token, allow_missing=False):
    name = PACKAGES[service].rsplit('/', 1)[-1]
    req = urllib.request.Request('https://api.github.com/user/packages/container/' + name,
        headers={'Authorization': 'Bearer ' + token, 'Accept': 'application/vnd.github+json', 'User-Agent': 'Lastaway-Deploy'})
    try:
        with urllib.request.urlopen(req, timeout=20) as response: data = json.load(response)
    except urllib.error.HTTPError as exc:
        if exc.code == 404 and allow_missing: return
        raise RuntimeError('Cannot verify GHCR package permission/visibility; check classic PAT and account') from None
    if data.get('visibility') != 'private' or data.get('repository', {}).get('html_url', '').lower() != SOURCE.lower():
        raise RuntimeError('GHCR package must be private and linked to Lastaway repository: ' + name)


def bases():
    result = {}
    for key, ref in [('build', 'mcr.microsoft.com/dotnet/sdk:8.0'), ('runtime', 'mcr.microsoft.com/dotnet/aspnet:8.0')]:
        value = json.loads(run(['docker', 'buildx', 'imagetools', 'inspect', '--raw', ref]))
        if 'manifests' not in value: raise RuntimeError('Expected base-image platform index')
        digest = next(m['digest'] for m in value['manifests'] if m.get('platform', {}).get('os') == 'linux' and m['platform'].get('architecture') == 'amd64')
        result[key] = ref + '@' + digest
    return result


def connection_hash():
    files = [BACKEND / 'compose.aws.yaml', DEPLOY / 'Aws/Caddyfile']
    files += sorted((DEPLOY / 'Aws/scripts').glob('*'))
    files += [DEPLOY / n for n in ('registry_release.py', 'deploy_registry.py', 'deploy-aws.ps1')]
    return tree_hash(ROOT, [p for p in files if p.is_file()])


def clean_commit():
    if run(['git', 'status', '--porcelain', '--untracked-files=all'], cwd=ROOT).strip():
        raise RuntimeError('Publishing requires a clean working tree. Review and commit changes first; this tool never commits/pushes Git.')
    return run(['git', 'rev-parse', 'HEAD'], cwd=ROOT).strip()


def backend_checks(output):
    print('Local: backend build and regressions', flush=True)
    run(['dotnet', 'build', BACKEND / 'JungleBackend.sln', '-c', 'Release', '-v', 'quiet'], timeout=300)
    for name in ('NetworkChecks', 'DualTransportChecks', 'MapCatalogChecks', 'BotChecks', 'MatchResultChecks', 'CombatChecks', 'InventoryChecks', 'WorldChecks', 'MatchChecks'):
        print('Local check: ' + name, flush=True)
        text = run(['dotnet', 'run', '--project', BACKEND / f'Tests/{name}/{name}.csproj', '-c', 'Release', '--no-restore'], timeout=180)
        (output / (name + '.log')).write_text(text, encoding='utf-8')
    run([sys.executable, BACKEND / 'Tests/test_registry_deploy.py'], timeout=120)


def unity(args, output, full, local=False):
    if not args.player: raise RuntimeError('Full acceptance requires a current Windows player')
    output.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    for name in list(env):
        if name.startswith('NET_'): del env[name]
    # Remote runner owns clients/room cleanup and restores credential variables in finally.
    env['NET_TEST_BOT_COUNT'] = '9' if full else '4'
    scenario = DEPLOY / ('registry-wss10.json' if full else 'registry-auto2.json')
    command = [PS, '-NoProfile', '-File', DEPLOY / 'test-unity-remote.ps1', '-Player', args.player,
        '-Scenario', scenario, '-OutputDirectory', output, '-Clients', '1' if full else '2',
        '-ApiBaseUrl', args.local_api if local else 'https://' + args.domain]
    if getattr(args, 'force_d3d11', False): command += ['-ForceD3D11']
    if local: command += ['-Local']
    if full: env['NET_TRANSPORT'] = 'Wss'  # One full 300s WSS measurement, not a connection-only test.
    else: command += ['-ForceAutoFallback']; env.pop('NET_TRANSPORT', None)
    run(command, timeout=660 if full else 290, env=env)
    report = read_json(output / 'players/report.json')
    if report.get('status') != 'PASS': raise RuntimeError('Unity gameplay acceptance did not pass')
    if full and any(c.get('selectedTransport') != 'Wss' or c.get('connectedSeconds', 0) < 300 for c in report['clients']):
        raise RuntimeError('Full measurement must be WSS with >=300 seconds connected for every client')
    if report.get('playerFingerprint') != build_hash(Path(args.player).resolve()): raise RuntimeError('Player changed during test')
    return report


def verify_local(args, output):
    if (output / 'wss-10/players').exists() or (output / 'auto-2/players').exists():
        raise RuntimeError('Use a NEW OutputDirectory for Local acceptance; existing evidence is never overwritten')
    if not args.player: raise RuntimeError('Pass -Player pointing to a freshly built Windows player')
    print('Local: verify Windows source/build provenance', flush=True)
    source_before = client_source_hash(ROOT)
    provenance = read_json(Path(args.player).resolve().parent / 'registry-build.json')
    if provenance.get('schemaVersion') != 1 or provenance.get('sourceHash') != source_before or provenance.get('playerFingerprint') != build_hash(Path(args.player).resolve()) or provenance.get('unityVersion') != editor_version(ROOT):
        raise RuntimeError('Windows build is stale or lacks Unity build provenance; rebuild using ' + editor_version(ROOT))
    backend_checks(output)
    print('Local: resolve pinned base images and build Docker', flush=True)
    pinned = bases()
    run(['docker', 'compose', '--profile', 'account', 'build', '--build-arg', 'BUILD_IMAGE=' + pinned['build'],
        '--build-arg', 'RUNTIME_IMAGE=' + pinned['runtime']], cwd=BACKEND, timeout=600)
    acceptance_env = os.environ.copy(); acceptance_env['GAME_MEMORY_LIMIT'] = '256m'
    try:
        run(['docker', 'compose', '--profile', 'account', 'up', '-d', '--no-build', '--wait'], cwd=BACKEND, timeout=180, env=acceptance_env)
        for _ in range(30):
            try:
                if api(args.local_api, 'GET', '/health')[0] == 200: break
            except Exception: pass
            time.sleep(2)
        else: raise RuntimeError('Local API did not become ready')
        container = run(['docker', 'compose', 'ps', '-q', 'game-server'], cwd=BACKEND).strip()
        initial = json.loads(run(['docker', 'inspect', container]))[0]
        if initial['HostConfig']['Memory'] != 256 * 1024 ** 2: raise RuntimeError('Local acceptance requires a real 256MiB container limit')
        since = int(time.time()); memory = []; sample_errors = []; stop = threading.Event()
        def sample_memory():
            next_sample = time.monotonic()
            while not stop.is_set():
                try:
                    row = json.loads(run(['docker', 'stats', '--no-stream', '--format', '{{json .}}', container], timeout=15))
                    memory.append(float(row['MemPerc'].rstrip('%')))
                except Exception: sample_errors.append('Docker memory sample failed')
                next_sample += 5
                if stop.wait(max(0, next_sample - time.monotonic())): break
        sampler = threading.Thread(target=sample_memory, daemon=True); sampler.start()
        try:
            print('Local: WSS one human +9 bots, at least 300s', flush=True)
            one = unity(args, output / 'wss-10', True, local=True)
            print('Local: Auto two clients +4 bots, result/history/replay', flush=True)
            two = unity(args, output / 'auto-2', False, local=True)
            quick(args, output / 'quick', args.local_api)
            final = json.loads(run(['docker', 'inspect', container]))[0]
            logs = run(['docker', 'logs', '--since', str(since), container])
            (output / 'server-metrics.log').write_text(logs, encoding='utf-8')
            samples = [dict(re.findall(r'(\w+)=([^ ]+)', line)) for line in logs.splitlines() if '[metrics]' in line]
            if sample_errors: raise RuntimeError('Local memory sampler lost coverage')
            metric_result = validate_metrics(samples, memory, final['RestartCount'] - initial['RestartCount'], final['State'].get('OOMKilled', False))
            write_json(output / 'server-metrics.json', {'summary': metric_result, 'samples': samples, 'memoryPercent': memory, 'containerLimitBytes': initial['HostConfig']['Memory']})
        finally:
            stop.set(); sampler.join(timeout=20)
            # Failed client runs are evidence too; preserve metrics before restoring/recreating the dev container.
            if not (output / 'server-metrics.log').exists():
                try:
                    (output / 'server-metrics.log').write_text(run(['docker','logs','--since',str(since),container]),encoding='utf-8')
                    write_json(output / 'server-memory.json', {'memoryPercent':memory,'errors':sample_errors})
                except Exception: pass
    finally:
        # Restore even when startup/readiness fails; never restart/delete Mongo or its volume.
        run(['docker', 'compose', 'up', '-d', '--no-build', '--no-deps', 'game-server'], cwd=BACKEND, timeout=120)
    if source_before != client_source_hash(ROOT): raise RuntimeError('Source/data changed during rehearsal')
    value = {'schemaVersion': 1, 'status': 'PASS', 'unityVersion': provenance['unityVersion'], 'sourceHash': source_before,
        'playerFingerprint': build_hash(Path(args.player).resolve()), 'bases': pinned,
        'services': {s: fingerprint(ROOT, s, pinned) for s in PACKAGES},
        'connectionConfigHash': connection_hash(), 'physicalTwoPc': 'NOT_TESTED', 'serverMetrics': metric_result,
        'checks': {'wss300': one['status'], 'auto2ResultHistoryReplay': two['status'], 'udp': 'PASS'}}
    write_json(output / 'local-evidence.json', value)
    return value


def local_gate(args, manifest, check_workspace=True):
    if not args.local_evidence or not args.player: raise RuntimeError('Full release needs -Player and -LocalEvidence from -VerifyLocal; old build/rehearsal cannot be reused')
    value = read_json(Path(args.local_evidence))
    if value.get('status') != 'PASS': raise RuntimeError('Local evidence did not pass')
    if check_workspace and value.get('sourceHash') != client_source_hash(ROOT): raise RuntimeError('Local evidence source hash is stale')
    provenance = read_json(Path(args.player).resolve().parent / 'registry-build.json')
    if provenance.get('sourceHash') != value.get('sourceHash') or provenance.get('unityVersion') != editor_version(ROOT): raise RuntimeError('Windows build provenance does not match Local evidence')
    if manifest.get('localEvidence', {}).get('sourceHash', value.get('sourceHash')) != value.get('sourceHash'): raise RuntimeError('Release expects a different client source build')
    if value.get('playerFingerprint') != build_hash(Path(args.player).resolve()): raise RuntimeError('Windows player fingerprint mismatch')
    if value.get('services') != {s: m['fingerprint'] for s, m in manifest['services'].items()}: raise RuntimeError('Local backend/base-image fingerprints mismatch')
    if value.get('connectionConfigHash') != manifest['connectionConfigHash']: raise RuntimeError('Local deployment-config evidence is stale')
    if value.get('serverMetrics', {}).get('status') != 'PASS': raise RuntimeError('Local Full server performance evidence missing')
    if value.get('checks') != {'wss300': 'PASS', 'auto2ResultHistoryReplay': 'PASS', 'udp': 'PASS'}: raise RuntimeError('Incomplete Local Full evidence')
    return value


def publish(args, output):
    commit = clean_commit()
    backend_checks(output)
    base = bases()
    ledger_path = BACKEND / 'out/registry/published.json'
    previous = read_json(ledger_path) if ledger_path.exists() else None
    release_id = args.release_id or ('ghcr-' + datetime.now(timezone.utc).strftime('%Y%m%d-%H%M%S') + '-' + commit[:8])
    if not RELEASE_ID.fullmatch(release_id): raise ValueError('Invalid release ID')
    manifest = {'schemaVersion': 1, 'format': 'ghcr', 'releaseId': release_id, 'commit': commit,
        'platform': 'linux/amd64', 'source': SOURCE, 'bases': base, 'services': {},
        'connectionConfigHash': connection_hash(), 'data': {'matchVersion': read_json(BACKEND / 'Shared/GameData/match_settings.json')['version'], 'worldVersion': read_json(BACKEND / 'Shared/GameData/world_settings.json')['version'], 'catalogGeneration': read_json(BACKEND / 'Shared/GameData/Maps/catalog.json')['generation']},
        'protocol': {'accountHandshake': 4, 'gameplay': int(re.search(r'MatchProtocol.*?Version = (\d+)', (BACKEND / 'Shared/Protocol/Gameplay/MatchPackets.cs').read_text()).group(1))},
        'protocolSourceHash': tree_hash(ROOT, sorted((BACKEND / 'Shared/Protocol').rglob('*.cs')))}
    for service in PACKAGES:
        manifest['services'][service] = {'fingerprint': fingerprint(ROOT, service, base)}
    tier = required_tier(previous, manifest)
    if tier == 'Full' or args.test_mode == 'Full': manifest['localEvidence'] = local_gate(args, manifest)
    token = credential()
    stage = output / release_id
    stage.mkdir(parents=True, exist_ok=False)
    steps = []
    for service, package in PACKAGES.items():
        t = time.monotonic(); item = manifest['services'][service]
        package_private(service, token, allow_missing=True)
        old = (previous or {}).get('services', {}).get(service)
        if old and old['fingerprint'] == item['fingerprint']:
            run(['docker', 'buildx', 'imagetools', 'inspect', old['image']])
            item.update({k: v for k, v in old.items() if k != 'fingerprint'})
            reused = True
        else:
            tag = package + ':' + commit[:12] + '-' + item['fingerprint'][:12]
            metadata = output / (service + '-build.json')
            folder = 'Api' if service == 'api' else 'GameServer'
            run(['docker', 'buildx', 'build', '--platform', 'linux/amd64', '--provenance=false', '--sbom=false',
                '--build-arg', 'BUILD_IMAGE=' + base['build'], '--build-arg', 'RUNTIME_IMAGE=' + base['runtime'],
                '--label', 'org.opencontainers.image.source=' + SOURCE, '--label', 'org.opencontainers.image.revision=' + commit,
                '--metadata-file', metadata, '--push', '-t', tag, '-f', BACKEND / folder / 'Dockerfile', BACKEND], timeout=900)
            digest = read_json(metadata)['containerimage.digest']
            item.update({'image': package + '@' + digest, 'digest': digest, 'tag': tag})
            reused = False
        package_private(service, token)
        raw = json.loads(run(['docker', 'buildx', 'imagetools', 'inspect', '--raw', item['image']]))
        if 'layers' not in raw: raise RuntimeError('Expected single-platform image manifest (attestations disabled)')
        item['layers'] = [{'digest': x['digest'], 'size': x['size']} for x in raw['layers']]
        old_layers = {l['digest'] for l in (old or {}).get('layers', [])}
        steps.append({'service': service, 'reused': reused, 'seconds': round(time.monotonic() - t, 3),
            'newLayerBytes': sum(l['size'] for l in item['layers'] if l['digest'] not in old_layers), 'byteMeasure': 'compressed OCI layer delta, excludes TLS/retries; not wire traffic'})
    token = None
    manifest['testPolicy'] = tier
    manifest['publishSteps'] = steps
    validate_manifest(manifest)
    # The small bundle contains config/scripts, never the Docker images or private environment.
    shutil.copyfile(BACKEND / 'compose.aws.yaml', stage / 'compose.aws.yaml')
    shutil.copyfile(DEPLOY / 'Aws/Caddyfile', stage / 'Caddyfile')
    shutil.copytree(DEPLOY / 'Aws/scripts', stage / 'scripts', ignore=shutil.ignore_patterns('__pycache__'))
    shutil.copyfile(DEPLOY / 'registry_release.py', stage / 'scripts/registry_release.py')
    (stage / 'release.env').write_text(f'RELEASE_ID={release_id}\nRELEASE_FORMAT=ghcr\nAPI_IMAGE={manifest["services"]["api"]["image"]}\nGAME_IMAGE={manifest["services"]["game-server"]["image"]}\n', encoding='utf-8')
    # Normalize scripts originating on Windows, before sealing checksums.
    for path in stage.rglob('*'):
        if path.is_file(): path.write_bytes(path.read_bytes().replace(b'\r\n', b'\n'))
    write_json(stage / 'manifest.json', manifest); seal(stage); verify(stage)
    if clean_commit() != commit: raise RuntimeError('Source commit changed while publishing; bundle not accepted')
    archive = output / (release_id + '.tar.gz')
    with tarfile.open(archive, 'w:gz') as tar:
        for p in sorted(stage.rglob('*')):
            if p.is_file(): tar.add(p, arcname=p.relative_to(stage).as_posix())
    (Path(str(archive) + '.sha256')).write_text(sha(archive.read_bytes()) + '  ' + archive.name + '\n', encoding='utf-8')
    write_json(ledger_path, manifest)
    return stage, manifest, archive


def api(origin, method, path, body=None, token=''):
    headers = {'Content-Type': 'application/json'}
    if token: headers['Authorization'] = 'Bearer ' + token
    request = urllib.request.Request(origin + path, method=method, headers=headers,
        data=None if body is None else json.dumps(body).encode())
    try:
        with urllib.request.urlopen(request, timeout=15) as response:
            data = response.read(); return response.status, json.loads(data) if data else None
    except urllib.error.HTTPError as exc: return exc.code, None


def quick(args, output, origin):
    output.mkdir(parents=True, exist_ok=True)
    if api(origin, 'GET', '/health')[0] != 200: raise RuntimeError('HTTPS/API health failed')
    for token in ('', 'bad.jwt.signature'):
        if api(origin, 'POST', '/api/rooms', {'name': 'Denied', 'maxPlayers': 10}, token)[0] != 401: raise RuntimeError('JWT guard failed')
    username, password = 'reg' + secrets.token_hex(8), secrets.token_urlsafe(24)
    if api(origin, 'POST', '/api/auth/register', {'username': username, 'password': password})[0] != 201: raise RuntimeError('Account registration failed')
    code, login = api(origin, 'POST', '/api/auth/login', {'username': username, 'password': password})
    if code != 200: raise RuntimeError('Account login failed')
    token = login['accessToken']; room = None
    env = os.environ.copy()
    # Endpoint and auth are owned by this test; inherited dev overrides must not reroute it.
    for name in list(env):
        if name.startswith('NET_'): del env[name]
    try:
        code, room = api(origin, 'POST', '/api/rooms', {'name': 'Registry smoke', 'maxPlayers': 10}, token)
        if code != 201: raise RuntimeError('JWT room creation failed')
        if api(origin, 'GET', '/api/rooms', token=token)[0] != 200: raise RuntimeError('Room listing failed')
        env.update({'NET_ROOM_CODE': room['roomCode'], 'NET_ACCESS_TOKEN': token})
        host = urllib.parse.urlparse(origin).hostname
        for route in ('Udp', 'Wss'):
            config = {'mode': 'Local' if origin.startswith('http://') else 'Online', 'apiBaseUrl': origin,
                'gameServerHost': host, 'gameServerPort': 7777, 'authMode': 'RoomCode', 'protocolVersion': 4,
                'transport': route, 'connectTimeoutMs': 5000, 'webSocketUrl': ('ws://127.0.0.1:7781/game' if origin.startswith('http://') else 'wss://' + args.domain + '/game')}
            write_json(output / 'client.json', config)
            report_path = output / (route + '.json')
            run(['dotnet', 'run', '--no-build', '-c', 'Release', '--project', BACKEND / 'NetSmokeClient/NetSmokeClient.csproj', '--',
                '--config', output / 'client.json', '--name', 'RegistrySmoke', '--duration', '5', '--quiet', '--report', report_path], timeout=35, env=env)
            report = read_json(report_path)
            if report.get('joined') != 1 or report.get('failed') != 0: raise RuntimeError(route + ' join failed')
    finally:
        if room:
            for attempt in range(6):
                code, _ = api(origin, 'DELETE', '/api/rooms/' + room['id'], token=token)
                if code in (204, 404): break
                time.sleep(2)
            else: raise RuntimeError('Test room cleanup failed')


def aws(args, *command):
    return json.loads(run([args.aws_cli, *command, '--profile', args.profile, '--region', args.region, '--output', 'json']))


def ssh(args, command, timeout=180):
    # Existing alias uses SSM ProxyCommand; no public SSH rule or tunnel is added.
    return run(['ssh', '-o', 'BatchMode=yes', '-o', 'ConnectTimeout=15', args.ssh_alias, command], timeout=timeout)


def deploy(args, folder, manifest, archive, output):
    started = time.monotonic(); deadline = started + args.budget_minutes * 60
    instance = aws(args, 'ec2', 'describe-instances', '--instance-ids', args.instance_id)['Reservations'][0]['Instances'][0]
    if instance['State']['Name'] != 'running': raise RuntimeError('EC2 is not Running; start it manually then rerun Deploy (tool never starts it)')
    if instance.get('InstanceType') != 't3.small' or instance.get('Architecture') != 'x86_64': raise RuntimeError('Unexpected EC2 target configuration')
    ssm = aws(args, 'ssm', 'describe-instance-information', '--filters', 'Key=InstanceIds,Values=' + args.instance_id)
    if not ssm['InstanceInformationList'] or ssm['InstanceInformationList'][0]['PingStatus'] != 'Online': raise RuntimeError('SSM is not online')
    if args.mode != 'Rollback':
        resolved = {x[4][0] for x in socket.getaddrinfo(args.domain, 443, type=socket.SOCK_STREAM)}
        if instance.get('PublicIpAddress') not in resolved: raise RuntimeError('Public DNS/PC hosts does not match current EC2 IP; fix DuckDNS/old hosts entry first')
    if args.mode != 'Rollback' and api('https://' + args.domain, 'GET', '/health')[0] != 200: raise RuntimeError('Current HTTPS/TLS readiness failed; no deploy')
    metadata_code = """import json,pathlib,urllib.request
p=pathlib.Path('/opt/junglemaze/current')
t=urllib.request.urlopen(urllib.request.Request('http://169.254.169.254/latest/api/token',method='PUT',headers={'X-aws-ec2-metadata-token-ttl-seconds':'60'}),timeout=3).read().decode()
i=urllib.request.urlopen(urllib.request.Request('http://169.254.169.254/latest/meta-data/instance-id',headers={'X-aws-ec2-metadata-token':t}),timeout=3).read().decode()
v=pathlib.Path('/opt/junglemaze/previous').resolve() if ROLLBACK else p.resolve()
print(json.dumps({'instanceId':i,'current':json.loads((p/'manifest.json').read_text()),'targetRelease':v.name,'target':json.loads((v/'manifest.json').read_text())}))
""".replace('ROLLBACK', str(args.mode == 'Rollback'))
    remote_meta = json.loads(ssh(args, 'sudo python3 -c ' + shlex.quote(metadata_code)))
    if remote_meta['instanceId'] != args.instance_id: raise RuntimeError('SSH alias points to another EC2 instance')
    tier = 'Quick' if args.mode == 'Rollback' else ('Full' if args.test_mode == 'Full' else required_tier(remote_meta['current'], manifest))
    target_release = remote_meta['targetRelease'] if args.mode == 'Rollback' else manifest['releaseId']
    if tier == 'Full': local_gate(args, manifest, check_workspace=False)
    if tier == 'Full' and deadline - time.monotonic() < 1050: raise RuntimeError('Need >=1050 seconds remaining for Full, upload and rollback; no services changed')
    remote = '/tmp/lastaway-registry-' + secrets.token_hex(8)
    remote_script = remote + '/scripts/registry_host.py'
    transaction_id = secrets.token_hex(16)
    unit = 'lastaway-deploy-' + transaction_id[:16]
    state = None; transaction_started = False; finished = False
    try:
        ssh(args, 'mkdir -m 700 ' + remote)
        run(['scp', '-q', archive, args.ssh_alias + ':' + remote + '/release.tar.gz'], timeout=90)
        expected = sha(archive.read_bytes())
        extract_code = """import hashlib,pathlib,tarfile
p=pathlib.Path(REMOTE); a=p/'release.tar.gz'
assert hashlib.sha256(a.read_bytes()).hexdigest()==EXPECTED
with tarfile.open(a) as t:
 m=t.getmembers()
 assert len(m)<300 and sum(x.size for x in m)<12582912
 assert len({x.name for x in m})==len(m)
 assert all(x.isfile() and not pathlib.PurePosixPath(x.name).is_absolute() and '..' not in pathlib.PurePosixPath(x.name).parts and '\\\\' not in x.name for x in m)
 t.extractall(p,filter='data')
a.unlink()
""".replace('REMOTE', repr(remote)).replace('EXPECTED', repr(expected))
        ssh(args, 'python3 -c ' + shlex.quote(extract_code))
        seconds = min(1200, int(deadline - time.monotonic()))
        command = 'sudo systemd-run --quiet --collect --unit=' + unit + ' --property=RuntimeMaxSec=' + str(seconds + 200) + ' --property=TimeoutStopSec=10 /usr/bin/python3 ' + remote_script
        command += (' rollback' if args.mode == 'Rollback' else ' deploy --bundle ' + remote)
        command += ' --transaction-id ' + transaction_id + ' --seconds ' + str(seconds) + ' --min-test-seconds ' + ('790' if tier == 'Full' else '85')
        if args.confirm_empty_migration: command += ' --confirm-empty-migration'
        # Transient root service survives a PC sleep/SSH disconnect, and rolls back on missing decision.
        ssh(args, command); transaction_started = True
        while deadline - time.monotonic() > 200:
            try: candidate = json.loads(ssh(args, 'sudo python3 ' + remote_script + ' status', timeout=25))
            except Exception: candidate = {}
            if candidate.get('releaseId') == target_release and candidate.get('transactionId') == transaction_id:
                if candidate.get('status') == 'READY_FOR_PC_TEST': state = candidate; break
                if candidate.get('status') == 'PASS' and args.mode == 'Rollback':
                    state = candidate; finished = True; break
                if candidate.get('status') in ('BLOCKED', 'ROLLED_BACK', 'ROLLBACK_FAILED'): raise RuntimeError('Remote ' + candidate['status'])
            try: active = ssh(args, 'systemctl show ' + unit + '.service -p ActiveState --value', timeout=25).strip()
            except RuntimeError: active = ''  # --collect may already have removed a successful unit.
            if active not in ('active', 'activating'):
                # A fast rollback can finish after the status read but before the unit-state read.
                terminal = json.loads(ssh(args, 'sudo python3 ' + remote_script + ' status', timeout=25))
                if (args.mode == 'Rollback' and terminal.get('releaseId') == target_release
                        and terminal.get('transactionId') == transaction_id and terminal.get('status') == 'PASS'):
                    state = terminal; finished = True; break
                finished = True
                raise RuntimeError('Remote deployment failed before readiness; inspect journalctl -u ' + unit)
            time.sleep(3)
        if state is None: raise RuntimeError('Remote readiness deadline; remote rollback guard remains active')
        write_json(output / 'remote-ready.json', state)
        if args.mode == 'Rollback':
            write_json(output / 'remote-final.json', state)
            write_json(output / 'deployment.json', {'status': 'PASS', 'action': 'Rollback', 'release': target_release,
                'serverHealth': 'PASS', 'publicAcceptance': 'NOT_TESTED', 'ec2Lifecycle': 'unchanged', 'seconds': time.monotonic() - started})
            return
        previous_deadline = os.environ.get('AWS_ROOM_DEADLINE_UTC')
        os.environ['AWS_ROOM_DEADLINE_UTC'] = datetime.fromtimestamp(time.time() + max(0, deadline - time.monotonic() - 205), timezone.utc).isoformat()
        try:
            if tier == 'Full':
                if deadline - time.monotonic() < 990: raise RuntimeError('Not enough time for full 300s WSS, Auto round and rollback')
                unity(args, output / 'aws-wss-10', True)
                unity(args, output / 'aws-auto-2', False)
            quick(args, output / 'aws-quick', 'https://' + args.domain)
        finally:
            if previous_deadline is None: os.environ.pop('AWS_ROOM_DEADLINE_UTC', None)
            else: os.environ['AWS_ROOM_DEADLINE_UTC'] = previous_deadline
        sampled = json.loads(ssh(args, 'sudo python3 ' + remote_script + ' metrics --since ' + str(state['measurementSince'])))
        state = json.loads(ssh(args, 'sudo python3 ' + remote_script + ' status'))
        sampled.update({k: state.get(k, 0) for k in ('memorySamples', 'peakContainerMemoryPercent')})
        write_json(output / 'server-metrics.json', sampled)
        if tier == 'Full':
            samples = sampled['samples']
            if not samples or not 29 <= sum(float(x['ticksPerSecond']) for x in samples) / len(samples) <= 31: raise RuntimeError('Invalid tick window/rate')
            if len(samples) < 55 or any(int(x['droppedTicks']) or float(x['cpuOneCorePercent']) > 85 for x in samples): raise RuntimeError('Dropped ticks/CPU budget/sample coverage failed')
            if sampled['memorySamples'] < 55 or sampled['peakContainerMemoryPercent'] > 80 or sampled['oom'] or sampled['restartCount']: raise RuntimeError('RAM/OOM/restart gate failed')
        ssh(args, 'sudo python3 ' + remote_script + ' decision --transaction-id ' + state['transactionId'] + ' --accept')
        result = wait_remote(args, remote_script, time.monotonic() + 180, transaction_id)
        finished = True
        write_json(output / 'remote-final.json', result)
        if result['status'] != 'PASS': raise RuntimeError('Deployment acceptance failed: ' + result['status'])
        final_instance = aws(args, 'ec2', 'describe-instances', '--instance-ids', args.instance_id)['Reservations'][0]['Instances'][0]
        write_json(output / 'deployment.json', {'status': 'PASS', 'tier': tier, 'seconds': time.monotonic() - started,
            'bundleBytes': archive.stat().st_size, 'imageLayerDeltaBytes': sum(x.get('newLayerBytes', 0) for x in manifest.get('publishSteps', [])),
            'wireBytes': 'NOT_MEASURED: OCI compressed layer sizes exclude protocol overhead/cache/retries',
            'untested': [] if tier == 'Full' else ['combat', 'results/history', 'performance'],
            'physicalTwoPc': 'NOT_TESTED', 'ec2': final_instance['State']['Name'], 'ec2Lifecycle': 'unchanged'})
    finally:
        if transaction_started and not finished:
            if state and state.get('status') == 'READY_FOR_PC_TEST':
                try: ssh(args, 'sudo python3 ' + remote_script + ' decision --transaction-id ' + state['transactionId'])
                except Exception: pass
            try:
                result = wait_remote(args, remote_script, min(deadline, time.monotonic() + 195), transaction_id)
                write_json(output / 'remote-final.json', result); finished = True
            except Exception: pass  # Root guard survives loss of PC, SSH or AWS CLI credentials.
        if not transaction_started or finished:
            # Only our random /tmp staging, never releases, credentials, images or data volumes.
            try: ssh(args, 'sudo rm -rf -- ' + remote)
            except Exception: pass
        else:
            write_json(output / 'recovery.json', {'status': 'BLOCKED', 'guardUnit': unit,
                'stagingRetained': remote, 'reason': 'Cannot confirm remote outcome; inspect via SSM before another deploy'})


def wait_remote(args, script, until, transaction_id):
    while time.monotonic() < until:
        value = json.loads(ssh(args, 'sudo python3 ' + script + ' status', timeout=20))
        if value.get('transactionId') == transaction_id and value.get('status') in ('PASS', 'BLOCKED', 'ROLLED_BACK', 'ROLLBACK_FAILED'): return value
        time.sleep(2)
    raise RuntimeError('Remote outcome not confirmed; inspect SSM transaction guard')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--mode', choices=['PrepareOnly', 'Deploy', 'Rollback'], default='PrepareOnly')
    parser.add_argument('--release'); parser.add_argument('--release-id'); parser.add_argument('--player'); parser.add_argument('--local-evidence')
    parser.add_argument('--force-d3d11', action='store_true', help='Use Direct3D11 for test players; does not change the delivered player default')
    parser.add_argument('--verify-local', action='store_true'); parser.add_argument('--test-mode', choices=['Auto', 'Full'], default='Auto')
    parser.add_argument('--domain', default='junglemaze.duckdns.org'); parser.add_argument('--local-api', default='http://127.0.0.1:5000')
    parser.add_argument('--aws-cli', default='D:/AWS/ConnectionSetup/AWSCLI-portable/aws.exe'); parser.add_argument('--profile', default='junglemaze-admin')
    parser.add_argument('--region', default='ap-southeast-2'); parser.add_argument('--instance-id', default='i-0e617f8072fbb9878')
    parser.add_argument('--ssh-alias', default='junglemaze'); parser.add_argument('--budget-minutes', type=int, default=20)
    parser.add_argument('--confirm-empty-migration', action='store_true'); parser.add_argument('--output')
    args = parser.parse_args()
    if not re.fullmatch(r'[a-z0-9-]+\.duckdns\.org', args.domain): raise ValueError('Invalid DuckDNS hostname')
    if not re.fullmatch(r'[a-zA-Z0-9_-]+(?:\.exe)?', args.ssh_alias): raise ValueError('Invalid SSH alias')
    if not 5 <= args.budget_minutes <= 20: raise ValueError('Budget must be 5..20 minutes')
    local_origin = urllib.parse.urlparse(args.local_api)
    if args.verify_local and (local_origin.scheme != 'http' or local_origin.hostname not in ('localhost','127.0.0.1','::1') or local_origin.username or local_origin.password or local_origin.query or local_origin.fragment or local_origin.path not in ('','/')):
        raise ValueError('VerifyLocal requires an HTTP loopback API origin; it must never test EC2')
    args.local_api = args.local_api.rstrip('/')
    output = Path(args.output).resolve() if args.output else BACKEND / 'out/registry' / datetime.now(timezone.utc).strftime('%Y%m%d-%H%M%S')
    output.mkdir(parents=True, exist_ok=True)
    try:
        if args.verify_local:
            verify_local(args, output); print('Local evidence: ' + str(output / 'local-evidence.json')); return
        if args.release:
            folder = Path(args.release).resolve()
            manifest = verify(folder)
            archive = output / (manifest['releaseId'] + '.tar.gz')
            with tarfile.open(archive, 'w:gz') as tar:
                for p in sorted(folder.rglob('*')):
                    if p.is_file() and p.name != 'base-images.env': tar.add(p, arcname=p.relative_to(folder).as_posix())
        else:
            if args.mode == 'Rollback': raise RuntimeError('Rollback requires -Release with the installed registry tool bundle; previous image is selected on EC2')
            folder, manifest, archive = publish(args, output)
        if args.mode != 'PrepareOnly': deploy(args, folder, manifest, archive, output)
        print('Release: ' + str(folder)); print('Report: ' + str(output))
    except Exception as exc:
        write_json(output / 'failure.json', {'status': 'BLOCKED' if isinstance(exc, (RuntimeError, ValueError)) else 'FAIL',
            'reason': str(exc) if isinstance(exc, (RuntimeError, ValueError)) else type(exc).__name__, 'ec2Lifecycle': 'unchanged'})
        raise

if __name__ == '__main__':
    try: main()
    except Exception as exc:
        print(str(exc) if isinstance(exc, (RuntimeError, ValueError)) else type(exc).__name__, file=sys.stderr); sys.exit(1)

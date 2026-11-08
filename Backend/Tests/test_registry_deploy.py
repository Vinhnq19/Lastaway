"""Local fault/transaction tests; no registry, AWS, Docker mutations or credentials."""
import copy
import importlib.util
import io
import json
import os
import subprocess
from pathlib import Path
import sys
import tarfile
import tempfile
import types
import unittest
from unittest.mock import patch

DEPLOY = Path(__file__).resolve().parents[1] / 'Deploy'
sys.path.insert(0, str(DEPLOY))
import registry_release as release
import deploy_registry as pc
spec = importlib.util.spec_from_file_location('registry_host', DEPLOY / 'Aws/scripts/registry_host.py')
host = importlib.util.module_from_spec(spec); spec.loader.exec_module(host)


def manifest(name='test-1'):
    return {'schemaVersion': 1, 'format': 'ghcr', 'releaseId': name, 'commit': 'a' * 40,
        'platform': 'linux/amd64', 'connectionConfigHash': 'c' * 64,
        'services': {s: {'image': package + '@sha256:' + 'b' * 64, 'digest': 'sha256:' + 'b' * 64,
            'fingerprint': 'd' * 64} for s, package in release.PACKAGES.items()}}


def bundle(path):
    path.mkdir(parents=True, exist_ok=True)
    release.write_json(path / 'manifest.json', manifest())
    (path / 'release.env').write_text('RELEASE_ID=test-1\nRELEASE_FORMAT=ghcr\n' + ''.join(k+'='+manifest()['services'][v]['image']+'\n' for k,v in [('API_IMAGE','api'),('GAME_IMAGE','game-server')]))
    (path / 'compose.aws.yaml').write_text('services: {}\n')
    (path / 'Caddyfile').write_text('same-proxy\n')
    release.seal(path)


class ReleaseTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.root = Path(self.tmp.name)
    def tearDown(self): self.tmp.cleanup()

    def test_editor_version_requires_an_exact_project_pin(self):
        settings = self.root / 'ProjectSettings'; settings.mkdir()
        pin = settings / 'ProjectVersion.txt'
        pin.write_text('m_EditorVersion: 6000.3.21f1\n')
        self.assertEqual(release.editor_version(self.root), '6000.3.21f1')
        pin.write_text('m_EditorVersion: 6000.3.21f1-unverified\n')
        with self.assertRaises(ValueError): release.editor_version(self.root)

    def test_local_gate_rejects_old_editor_even_when_other_evidence_matches(self):
        settings = self.root / 'ProjectSettings'; settings.mkdir()
        (settings / 'ProjectVersion.txt').write_text('m_EditorVersion: 6000.3.21f1\n')
        player = self.root / 'Lastaway.exe'; player.write_bytes(b'test fixture')
        local = {'status': 'PASS', 'sourceHash': 'a' * 64, 'playerFingerprint': 'b' * 64,
                 'services': {s: 'd' * 64 for s in release.PACKAGES}, 'connectionConfigHash': 'c' * 64,
                 'serverMetrics': {'status': 'PASS'},
                 'checks': {'wss300': 'PASS', 'auto2ResultHistoryReplay': 'PASS', 'udp': 'PASS'}}
        evidence = self.root / 'local.json'; release.write_json(evidence, local)
        receipt = self.root / 'registry-build.json'
        args = types.SimpleNamespace(player=str(player), local_evidence=str(evidence))
        with patch.object(pc, 'ROOT', self.root), patch.object(pc, 'build_hash', return_value='b' * 64):
            release.write_json(receipt, {'sourceHash': 'a' * 64, 'unityVersion': '6000.3.15f1'})
            with self.assertRaisesRegex(RuntimeError, 'provenance'):
                pc.local_gate(args, manifest(), check_workspace=False)
            release.write_json(receipt, {'sourceHash': 'a' * 64, 'unityVersion': '6000.3.21f1'})
            self.assertEqual(pc.local_gate(args, manifest(), check_workspace=False), local)

    def test_checksum_tamper_and_extra_file(self):
        bundle(self.root)
        self.assertEqual(release.verify(self.root)['releaseId'], 'test-1')
        (self.root / 'Caddyfile').write_text('changed')
        with self.assertRaises(ValueError): release.verify(self.root)
        bundle(self.root); (self.root / '.env.aws').write_text('secret')
        with self.assertRaises(ValueError): release.verify(self.root)

    def test_sealed_host_bundle_import_does_not_create_bytecode(self):
        bundle(self.root)
        scripts = self.root / 'scripts'; scripts.mkdir()
        for source in (DEPLOY / 'Aws/scripts/registry_host.py', DEPLOY / 'registry_release.py'):
            (scripts / source.name).write_bytes(source.read_bytes())
        release.seal(self.root)
        # Fresh process without -B reproduces the real Linux systemd entrypoint.
        env = os.environ.copy(); env.pop('PYTHONDONTWRITEBYTECODE', None)
        code = "import runpy; runpy.run_path(" + repr(str(scripts / 'registry_host.py')) + ")"
        subprocess.run([sys.executable, '-c', code], env=env, check=True, capture_output=True)
        self.assertFalse((scripts / '__pycache__').exists())
        self.assertEqual(release.verify(self.root)['releaseId'], 'test-1')

    def test_full_metrics_require_coverage_and_real_thresholds(self):
        rows=[{'ticksPerSecond':'30','droppedTicks':'0','cpuOneCorePercent':'20'} for _ in range(60)]
        self.assertEqual(release.validate_metrics(rows,[50]*60,0,False)['status'],'PASS')
        for memory,restarts,oom in [([81]*60,0,False),([50]*10,0,False),([50]*60,1,False),([50]*60,0,True)]:
            with self.assertRaises(RuntimeError): release.validate_metrics(rows,memory,restarts,oom)
        rows[5]['droppedTicks']='1'
        with self.assertRaises(RuntimeError): release.validate_metrics(rows,[50]*60,0,False)

    def test_release_env_cannot_override_digest_even_after_resealing(self):
        bundle(self.root)
        p = self.root / 'release.env'; p.write_text(p.read_text().replace('API_IMAGE=ghcr.io/vinhnq19/lastaway-api@sha256:', 'API_IMAGE=wrong-api@sha256:'))
        release.seal(self.root)
        with self.assertRaisesRegex(ValueError, 'immutable'): release.verify(self.root)

    def test_windows_source_provenance_ignores_only_inactive_importer_defaults(self):
        p = self.root / 'sample.cs.meta'; p.write_text('guid: retained\n')
        before = release.source_bytes(p)
        block = '  - serializedVersion: 4\n    buildTarget: WebGL\n    overridden: 0\n'
        p.write_text(p.read_text() + block)
        self.assertEqual(release.source_bytes(p), before)
        p.write_text(p.read_text().replace('overridden: 0', 'overridden: 1'))
        self.assertNotEqual(release.source_bytes(p), before)

    def test_digest_platform_and_repository_guard(self):
        for field, value in [('platform', 'linux/arm64'), ('commit', 'dirty'), ('format', 'unknown')]:
            data = manifest(); data[field] = value
            with self.assertRaises(ValueError): release.validate_manifest(data)
        data = manifest(); data['services']['api']['image'] = 'evil.example/api@sha256:' + 'b' * 64
        with self.assertRaises(ValueError): release.validate_manifest(data)
        data = manifest(); data['services']['api']['digest'] = 'latest'
        with self.assertRaises(ValueError): release.validate_manifest(data)

    def test_archive_traversal_and_links(self):
        archive = self.root / 'bad.tar.gz'
        for name, kind in [('../outside', tarfile.REGTYPE), ('link', tarfile.SYMTYPE)]:
            with tarfile.open(archive, 'w:gz') as tar:
                entry = tarfile.TarInfo(name); entry.type = kind; tar.addfile(entry)
            with self.assertRaises(ValueError): release.unpack(archive, self.root / 'out')

    def test_effective_legacy_base_images_and_secret_literal(self):
        (self.root / 'base-images.env').write_text('MONGO_IMAGE=installed-mongo\nMONGO_MAJOR=7\nCADDY_IMAGE=installed-caddy\n')
        (self.root / 'release.env').write_text('APPS_ONLY=1\nAPI_IMAGE=old\n')
        cfg = self.root / 'private'; cfg.write_text("SWAGGER_HASH='$2a$12$literal'\n")
        effective = release.effective_env(self.root, cfg)
        self.assertEqual(effective['MONGO_MAJOR'], '7')
        self.assertEqual(effective['SWAGGER_HASH'], '$2a$12$literal')

    def test_input_fingerprint_includes_only_compiled_shared_api_contracts(self):
        for folder in ('Api', 'GameServer', 'Shared/Protocol', 'Shared/World', 'Shared/GameData'):
            p = self.root / 'Backend' / folder; p.mkdir(parents=True)
            (p / 'data.cs').write_text('one')
        for name in ('RoomContracts.cs', 'MatchResultContracts.cs'):
            (self.root / 'Backend/Shared' / name).write_text('contract')
        (self.root / 'Backend/.dockerignore').write_text('**/obj')
        a = release.fingerprint(self.root, 'api', {'runtime': 'pin1'})
        g = release.fingerprint(self.root, 'game-server', {'runtime': 'pin1'})
        (self.root / 'Backend/Shared/Protocol/data.cs').write_text('protocol changed')
        self.assertEqual(a, release.fingerprint(self.root, 'api', {'runtime': 'pin1'}))
        self.assertNotEqual(g, release.fingerprint(self.root, 'game-server', {'runtime': 'pin1'}))
        self.assertNotEqual(a, release.fingerprint(self.root, 'api', {'runtime': 'pin2'}))
        (self.root / 'Backend/Shared/RoomContracts.cs').write_text('contract changed')
        self.assertNotEqual(a, release.fingerprint(self.root, 'api', {'runtime': 'pin1'}))

    def test_tier_cannot_quick_downgrade_server_or_connection_change(self):
        current = manifest(); changed = copy.deepcopy(current)
        self.assertEqual(release.required_tier(None, current), 'Full')
        changed['services']['api']['fingerprint'] = 'e' * 64
        self.assertEqual(release.required_tier(current, changed), 'Quick')
        changed['services']['game-server']['fingerprint'] = 'e' * 64
        self.assertEqual(release.required_tier(current, changed), 'Full')
        changed = copy.deepcopy(current); changed['connectionConfigHash'] = 'other'
        self.assertEqual(release.required_tier(current, changed), 'Full')

    def test_dirty_publish_denied_without_git_write(self):
        calls = []
        with patch.object(pc, 'run', side_effect=lambda args, **kw: calls.append(args) or ' M Api.cs'):
            with self.assertRaisesRegex(RuntimeError, 'clean working tree'): pc.clean_commit()
        self.assertEqual(calls[0][:2], ['git', 'status'])

    def test_registry_auth_and_private_visibility(self):
        import urllib.error
        with patch.object(pc.urllib.request, 'urlopen', side_effect=urllib.error.HTTPError('x', 401, 'denied', {}, None)):
            with self.assertRaises(RuntimeError): pc.package_private('api', 'fake-memory-only')
        for data in ({'visibility': 'public', 'repository': {'html_url': release.SOURCE}}, {'visibility': 'private', 'repository': {}}):
            response = unittest.mock.MagicMock(); response.__enter__.return_value = io.BytesIO(json.dumps(data).encode())
            with patch.object(pc.urllib.request, 'urlopen', return_value=response):
                with self.assertRaises(RuntimeError): pc.package_private('api', 'fake-memory-only')

    def test_plan_selective_and_idempotent(self):
        before = {s: {'image': s, 'volumes': []} for s in ('mongo', 'api', 'game-server', 'caddy')}
        a, b = self.root / 'a', self.root / 'b'; a.mkdir(); b.mkdir()
        for p in (a,b): (p / 'Caddyfile').write_text('same')
        for service in ('api', 'game-server', None):
            after = copy.deepcopy(before)
            if service: after[service]['image'] = 'new'
            with patch.object(host, 'configured_services', side_effect=[before, after]):
                self.assertEqual(host.plan(a, b), [service] if service else [])
        after = copy.deepcopy(before); after['mongo']['image'] = 'different-major'
        with patch.object(host, 'configured_services', side_effect=[before, after]):
            with self.assertRaisesRegex(RuntimeError, 'Mongo'): host.plan(a,b)

    def test_rendered_compose_cannot_override_manifest_images(self):
        old, target = self.root/'old', self.root/'target'
        old.mkdir(); bundle(target)
        before = {s: {'image': s, 'volumes': []} for s in ('mongo','api','game-server','caddy')}
        (old/'Caddyfile').write_bytes((target/'Caddyfile').read_bytes())
        valid = copy.deepcopy(before)
        for name in ('api','game-server'): valid[name]['image'] = manifest()['services'][name]['image']
        with patch.object(host,'configured_services',side_effect=[before,valid]):
            self.assertEqual(host.plan(old,target), ['api','game-server'])
        for service in ('api','game-server'):
            after = copy.deepcopy(valid)
            after[service]['image'] = 'legacy-image:latest'
            with patch.object(host,'configured_services',side_effect=[before,after]):
                with self.assertRaisesRegex(RuntimeError,'overrides immutable image: '+service): host.plan(old,target)

    def test_occupied_snapshot_denies_without_exposing_secret(self):
        with patch.object(host, 'status', return_value={'schemaVersion': 1, 'safeToDeploy': False}):
            with self.assertRaisesRegex(RuntimeError, 'unfinished match'): host.require_empty(self.root)

    def test_wrong_architecture_and_missing_digest(self):
        image = {'Os': 'linux', 'Architecture': 'arm64', 'Id': 'id', 'RepoDigests': []}
        with patch.object(host, 'run', return_value=json.dumps([image])):
            with self.assertRaisesRegex(RuntimeError, 'AMD64'): host.cached_image('ghcr.io/x@sha256:'+'f'*64)
        image['Architecture'] = 'amd64'
        with patch.object(host, 'run', return_value=json.dumps([image])):
            with self.assertRaisesRegex(RuntimeError, 'digest mismatch'): host.cached_image('ghcr.io/x@sha256:'+'f'*64)

    def transaction(self, *, pull_fail=False, health_fail=False, rollback_fail=False, busy=False, no_change=False, accept_fail=False):
        root = self.root / 'host'; (root / 'releases/old').mkdir(parents=True)
        old = root / 'releases/old'; bundle(old)
        (old / 'release.env').write_text('API_IMAGE=api-old\nGAME_IMAGE=game-old\nMONGO_MAJOR=7\nMONGO_IMAGE=mongo-stays\nCADDY_IMAGE=caddy-stays\n')
        def test_link(name, target):
            pointer = root / name
            if os.name == 'nt':
                # Unprivileged test-only junction; rmdir removes only the junction, not target data.
                if pointer.exists():
                    if pointer.parent.resolve() != root.resolve(): raise AssertionError('Invalid fixture path')
                    pointer.rmdir()
                subprocess.run(['cmd', '/d', '/c', 'mklink', '/J', str(pointer), str(target)], check=True, capture_output=True)
            else:
                pointer.unlink(missing_ok=True); pointer.symlink_to(target, target_is_directory=True)
        test_link('current', old)
        cfg = root / 'private'; cfg.write_text('ROOM_CONTROL_TOKEN=unit-test-placeholder\n')
        new = self.root / 'bundle'; bundle(new)
        operations = []
        fake_lock = types.SimpleNamespace(LOCK_EX=1, LOCK_NB=2, LOCK_UN=3, flock=lambda *a: None)
        def compose(folder, *args): operations.append((folder.name, args)); return ''
        def fail_pull(*a, **kw):
            if pull_fail: raise RuntimeError('Registry auth expired')
            return ''
        candidate_health_calls = []
        def health(folder, services):
            if folder.name != 'old': candidate_health_calls.append(folder.name)
            if health_fail and (folder.name != 'old' or rollback_fail) or accept_fail and folder.name != 'old' and len(candidate_health_calls) == 2:
                raise RuntimeError('Injected readiness failure')
        def sleep(_):
            state = release.read_json(root / 'deploy-state/state.json')
            release.write_json(root / 'deploy-state/decision.json', {'transactionId': state['transactionId'], 'accept': True})
        with patch.multiple(host, ROOT=root, CONFIG=cfg, fcntl=fake_lock), \
             patch.object(host, 'plan', return_value=[] if no_change else ['api']), \
             patch.object(host, 'compose', side_effect=compose), patch.object(host, 'health', side_effect=health), patch.object(host, 'link', side_effect=test_link), \
             patch.object(host, 'metrics', side_effect=lambda *a: operations.append(('metrics', ())) or {'candidateSamplesRetained':True}), \
             patch.object(host, 'cached_image', return_value='id'), patch.object(host, 'run', side_effect=fail_pull), patch.object(host, 'service_id', side_effect=lambda folder, service: service), patch.object(host, 'inspect', return_value={'RestartCount':0}), \
             patch.object(host.time, 'sleep', side_effect=sleep), \
             patch.object(host.shutil, 'disk_usage', return_value=types.SimpleNamespace(free=2*1024**3)):
            if pull_fail or health_fail or accept_fail:
                with self.assertRaises(RuntimeError): host.transaction(new)
            else: host.transaction(new)
        return root, operations, release.read_json(root / 'deploy-state/state.json')

    def test_pull_failure_keeps_running_release(self):
        root, operations, state = self.transaction(pull_fail=True)
        self.assertEqual(operations, []); self.assertEqual((root/'current').resolve().name, 'old')
        self.assertEqual(state['status'], 'BLOCKED')

    def test_selective_switch_and_rollback_health(self):
        root, operations, state = self.transaction(health_fail=True)
        self.assertEqual([x[0] for x in operations], ['test-1','old'])
        self.assertTrue(all(x[1][-1] == 'api' and '--no-deps' in x[1] for x in operations))
        self.assertEqual(state['status'], 'ROLLED_BACK'); self.assertEqual((root/'current').resolve().name, 'old')

    def test_failure_retains_candidate_metrics_before_recreate(self):
        _, operations, state = self.transaction(accept_fail=True)
        self.assertEqual(state['status'], 'ROLLED_BACK')
        self.assertEqual(state['failureMetrics'], {'candidateSamplesRetained':True})
        self.assertEqual([x[0] for x in operations], ['test-1', 'metrics', 'old'])

    def test_rollback_failure_reported_separately(self):
        _, _, state = self.transaction(health_fail=True, rollback_fail=True)
        self.assertEqual(state['status'], 'ROLLBACK_FAILED')

    def test_success_and_same_release_no_recreate(self):
        root, operations, state = self.transaction(no_change=True)
        self.assertEqual(state['status'], 'PASS'); self.assertEqual(operations, [])
        self.assertEqual((root/'current').resolve().name, 'test-1')

    def rollback_exit_race(self, stale=False, wrong_release=False, unit_collected=False):
        output=self.root/'pc-output';output.mkdir()
        archive=self.root/'release.tar.gz';archive.write_bytes(b'fixture')
        args=types.SimpleNamespace(budget_minutes=20,instance_id='instance',mode='Rollback',
            test_mode='Auto',ssh_alias='fixture',confirm_empty_migration=False)
        status_reads=0;transaction_id='1'*32
        terminal={'status':'PASS','releaseId':'old','transactionId':transaction_id}
        if stale:terminal['transactionId']='2'*32
        if wrong_release:terminal['releaseId']='other'
        def ssh(_,command,**kwargs):
            nonlocal status_reads
            if command.startswith('sudo python3 -c '):
                return json.dumps({'instanceId':'instance','current':manifest('current'),
                    'targetRelease':'old','target':manifest('old')})
            if command.endswith(' status'):
                status_reads+=1
                return json.dumps({'transactionId':'earlier','status':'PASS'} if status_reads==1 else terminal)
            if command.startswith('systemctl show '):
                if unit_collected:raise RuntimeError('Unit collected')
                return 'inactive'
            return ''
        def aws(_,service,*args):
            if service=='ec2':return {'Reservations':[{'Instances':[{'State':{'Name':'running'},'InstanceType':'t3.small','Architecture':'x86_64'}]}]}
            return {'InstanceInformationList':[{'PingStatus':'Online'}]}
        with patch.object(pc,'ssh',side_effect=ssh),patch.object(pc,'aws',side_effect=aws), \
             patch.object(pc,'run',return_value=''),patch.object(pc.secrets,'token_hex',side_effect=['a'*16,transaction_id]):
            pc.deploy(args,self.root,manifest(),archive,output)
        self.assertEqual(status_reads,2)
        self.assertEqual(release.read_json(output/'remote-final.json'),terminal)
        self.assertEqual(release.read_json(output/'deployment.json')['status'],'PASS')

    def test_rollback_exit_race_rechecks_matching_terminal_state(self):
        self.rollback_exit_race()

    def test_collected_rollback_unit_requires_matching_terminal_result(self):
        self.rollback_exit_race(unit_collected=True)

    def test_inactive_worker_rejects_stale_transaction(self):
        with self.assertRaisesRegex(RuntimeError,'before readiness'):
            self.rollback_exit_race(stale=True)

    def test_inactive_worker_rejects_wrong_release(self):
        with self.assertRaisesRegex(RuntimeError,'before readiness'):
            self.rollback_exit_race(wrong_release=True)

    def test_duplicate_admin_flock(self):
        root = self.root / 'lockhost'
        fake_lock = types.SimpleNamespace(LOCK_EX=1, LOCK_NB=2, LOCK_UN=3, flock=unittest.mock.Mock(side_effect=BlockingIOError))
        with patch.multiple(host, ROOT=root, fcntl=fake_lock):
            with self.assertRaisesRegex(RuntimeError, 'administrator'): host.transaction(self.root)

if __name__ == '__main__': unittest.main(verbosity=2)

#!/usr/bin/env python3
"""Root-only selective transaction. A lost PC/expired deadline rolls back; EC2 is never stopped."""
from __future__ import annotations
import argparse
try:
    import fcntl
except ImportError:
    fcntl = None  # Windows imports helpers for unit tests; transactions require Linux.
import json
import os
from pathlib import Path
import shutil
import sys
import time
import tempfile
import urllib.request
import urllib.error

# The sealed bundle must remain byte-for-byte unchanged when Python imports its helper.
sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).parent))
from registry_release import effective_env, read_json, run, verify, write_json, sha

ROOT = Path("/opt/junglemaze")
CONFIG = ROOT / "config/.env.aws"


def compose(release, *args):
    env = os.environ.copy(); env.update(effective_env(release, CONFIG))
    return run(["docker", "compose", "--project-name", "junglemaze-aws", "--project-directory", release,
        "--env-file", release / "release.env", "--env-file", CONFIG,
        "-f", release / "compose.aws.yaml", *args], env=env, timeout=40)


def service_id(release, service):
    value = compose(release, "ps", "-aq", service).strip()
    if not value or "\n" in value: raise RuntimeError("Service container missing/ambiguous: " + service)
    return value


def inspect(release, service):
    return json.loads(run(["docker", "inspect", service_id(release, service)]))[0]


def status(release):
    data = inspect(release, "game-server")
    networks = data["NetworkSettings"]["Networks"]
    ip = next(n["IPAddress"] for n in networks.values() if n.get("IPAddress"))
    token = effective_env(release, CONFIG).get("ROOM_CONTROL_TOKEN", "")
    if len(token) < 32: raise RuntimeError("Room control unavailable")
    req = urllib.request.Request(f"http://{ip}:7780/internal/deployment-status", headers={"X-Room-Control-Key": token})
    with urllib.request.urlopen(req, timeout=5) as response:
        return json.load(response)


def require_empty(release, migration=False):
    try:
        value = status(release)
    except urllib.error.HTTPError as exc:
        if exc.code != 404 or not migration: raise RuntimeError("Deployment-status unavailable; explicit first-migration empty-window confirmation required") from None
        # Older image has no phase endpoint. Observe empty metrics beyond Ending cleanup.
        for index in range(2):
            logs = run(["docker", "logs", "--since", "20s", service_id(release, "game-server")])
            rows = [s for s in logs.splitlines() if "[metrics]" in s]
            import re
            if not rows: raise RuntimeError("No recent legacy occupancy metrics")
            fields = dict(re.findall(r"(\w+)=([^ ]+)", rows[-1]))
            if any(fields.get(k) != "0" for k in ("peers", "active", "pending", "bots")):
                raise RuntimeError("Legacy server occupied; no service changed")
            if index == 0: time.sleep(16)
        return
    if value.get("schemaVersion") != 1 or value.get("safeToDeploy") is not True:
        raise RuntimeError("Server has human pending/active/grace, bots, entities or unfinished match")


def configured_services(release):
    return json.loads(compose(release, "config", "--format", "json"))["services"]


def plan(old, target):
    before = configured_services(old); after = configured_services(target)
    # Never silently migrate Mongo, storage or base image, including inherited AppsOnly env.
    if before["mongo"] != after["mongo"]: raise RuntimeError("Mongo configuration changed; separate migration required")
    changed = []
    for service in ("api", "game-server", "caddy"):
        a, b = before[service].copy(), after[service].copy()
        if service == "caddy":
            for value in (a, b):
                value["volumes"] = [dict(v, source="RELEASE_CADDYFILE") if v.get("target") == "/etc/caddy/Caddyfile" else v for v in value.get("volumes", [])]
        if a != b or (service == "caddy" and (old / "Caddyfile").read_bytes() != (target / "Caddyfile").read_bytes()):
            changed.append(service)
    return changed


def cached_image(reference):
    image = json.loads(run(["docker", "image", "inspect", reference]))[0]
    if image.get("Os") != "linux" or image.get("Architecture") != "amd64": raise RuntimeError("Image is not Linux AMD64")
    if "@sha256:" in reference and reference not in image.get("RepoDigests", []): raise RuntimeError("Image digest mismatch")
    return image["Id"]


def health(release, services):
    stop = time.monotonic() + 60
    while True:
        ok = True
        for service in services:
            value = inspect(release, service)
            expected = effective_env(release, CONFIG)[{"api": "API_IMAGE", "game-server": "GAME_IMAGE", "caddy": "CADDY_IMAGE"}[service]]
            if value["Image"] != cached_image(expected): ok = False
            state = value["State"]
            if not state.get("Running") or state.get("OOMKilled"): ok = False
            if state.get("Health", {}).get("Status", "healthy") != "healthy": ok = False
        if ok:
            try:
                if "game-server" in services:
                    try: status(release)
                    except urllib.error.HTTPError as exc:
                        if exc.code != 404 or (release / "manifest.json").is_file() and read_json(release / "manifest.json").get("format") == "ghcr": raise
                        logs = run(["docker", "logs", "--tail", "80", service_id(release, "game-server")])
                        if "[ready]" not in logs: raise RuntimeError("Legacy rollback image has not reported readiness")
                return
            except Exception: ok = False
        if time.monotonic() >= stop: raise RuntimeError("Service readiness/digest check failed")
        time.sleep(2)


def metrics(release, since):
    import re
    rows = run(["docker", "logs", "--since", str(since), service_id(release, "game-server")]).splitlines()
    samples = [dict(re.findall(r"(\w+)=([^ ]+)", line)) for line in rows if "[metrics]" in line]
    state = inspect(release, "game-server")
    # Docker CLI MemPerc excludes inactive file cache; it is not process RSS or total cgroup usage.
    stats = json.loads(run(["docker", "stats", "--no-stream", "--format", "{{json .}}", state["Id"]]))
    mem = float(stats["MemPerc"].rstrip("%"))
    return {"samples": samples, "containerMemoryPercent": mem, "restartCount": state.get("RestartCount", 0) - read_json(ROOT / "deploy-state/state.json").get("restartCounts", {}).get("game-server", 0), "oom": state["State"].get("OOMKilled", False)}


def link(name, target):
    temp = ROOT / (name + ".next")
    temp.unlink(missing_ok=True); temp.symlink_to(target, target_is_directory=True); temp.replace(ROOT / name)


def transaction(bundle, rollback=False, migration=False, seconds=1200, min_test_seconds=85, transaction_id=None):
    ROOT.mkdir(parents=True, exist_ok=True)
    if fcntl is None: raise RuntimeError("Remote transactions require Linux flock")
    lock = (ROOT / "deploy.lock").open("a")
    try: fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except BlockingIOError:
        lock.close()
        raise RuntimeError("Another administrator holds the deployment lock") from None
    old = (ROOT / "current").resolve(strict=True)
    if old.parent != ROOT / "releases": raise RuntimeError("Invalid current release link")
    oldenv = effective_env(old, CONFIG)
    if rollback:
        target = (ROOT / "previous").resolve(strict=True)
        if target.parent != ROOT / "releases": raise RuntimeError("Invalid previous release link")
    else:
        manifest = verify(bundle)
        target = ROOT / "releases" / manifest["releaseId"]
        # First registry release inherits the installed base images, never the template defaults.
        if not target.exists():
            with tempfile.TemporaryDirectory(prefix=".stage-", dir=ROOT / "releases") as temporary:
                staged = Path(temporary) / "release"
                shutil.copytree(bundle, staged)
                (staged / "base-images.env").write_text("".join(f"{k}={oldenv[k]}\n" for k in ("MONGO_IMAGE", "MONGO_MAJOR", "CADDY_IMAGE")), encoding="utf-8")
                verify(staged)
                staged.rename(target)  # A partial copy never becomes an installed release.
        verify(target)
        if (target / "SHA256SUMS").read_bytes() != (bundle / "SHA256SUMS").read_bytes(): raise RuntimeError("Release ID already has different contents")
    if effective_env(target, CONFIG).get("MONGO_MAJOR") != oldenv.get("MONGO_MAJOR"):
        raise RuntimeError("Rollback/deploy across Mongo major blocked")
    changed = plan(old, target)
    if "caddy" in changed: cached_image(oldenv["CADDY_IMAGE"])
    control = ROOT / "deploy-state"; control.mkdir(mode=0o700, exist_ok=True)
    result_path, decision_path = control / "state.json", control / "decision.json"
    decision_path.unlink(missing_ok=True)
    transaction_id = transaction_id or os.urandom(16).hex()
    result = {"transactionId": transaction_id, "releaseId": target.name, "previous": old.name,
        "changedServices": changed, "status": "PREFLIGHT", "steps": {}, "ec2Lifecycle": "unchanged"}
    def emit(state):
        result["status"] = state; write_json(result_path, result)
    start = time.monotonic(); applied = []
    try:
        if shutil.disk_usage(ROOT).free < 1024 ** 3: raise RuntimeError("Less than 1 GiB free; no image/service change")
        if "game-server" in changed: require_empty(old, migration)
        for service in changed:
            cached_image(oldenv[{"api": "API_IMAGE", "game-server": "GAME_IMAGE", "caddy": "CADDY_IMAGE"}[service]])
        config_hash = sha(CONFIG.read_bytes())
        original_ids = {service: service_id(old, service) for service in ("mongo", "api", "game-server", "caddy")}
        restart_counts = {service: inspect(old, service).get("RestartCount", 0) for service in original_ids}
        pull_start = time.monotonic()
        for service, key in (("api", "API_IMAGE"), ("game-server", "GAME_IMAGE")):
            reference = effective_env(target, CONFIG)[key]
            if not rollback and service in changed: run(["docker", "pull", "--platform", "linux/amd64", reference], timeout=240)
            cached_image(reference)
        result["steps"]["pullSeconds"] = round(time.monotonic() - pull_start, 3)
        if shutil.disk_usage(ROOT).free < 512 * 1024 ** 2: raise RuntimeError("Insufficient remaining disk after pull; services unchanged")
        if time.monotonic() - start > seconds - (min_test_seconds + 200): raise RuntimeError("Insufficient budget for switch and rollback")
        if "game-server" in changed: require_empty(old, migration)
        for service in changed:
            applied.append(service)  # Include partial compose failures in rollback.
            compose(target, "up", "-d", "--no-build", "--pull", "never", "--no-deps", "--force-recreate", service)
        health(target, ["api", "game-server", "caddy"])
        if sha(CONFIG.read_bytes()) != config_hash: raise RuntimeError("Private config changed during deployment")
        for service, container_id in original_ids.items():
            if service not in changed and service_id(target, service) != container_id:
                raise RuntimeError("Unchanged service was recreated: " + service)
        expected_restarts = {s: 0 if s in changed else count for s, count in restart_counts.items()}
        if any(inspect(target, s).get("RestartCount", 0) != count for s, count in expected_restarts.items()):
            raise RuntimeError("Unexpected restart during service switch")
        result["restartCounts"] = expected_restarts
        result["preserved"] = {"privateConfig": True, "mongoContainer": True, "unchangedServices": [s for s in original_ids if s not in changed]}
        if rollback:
            if target != old: link("previous", old); link("current", target)
            emit("PASS"); return
        result["measurementSince"] = int(time.time())
        emit("READY_FOR_PC_TEST")
        # Keep flock throughout public PC tests. If the PC dies, time out and restore old services.
        memory_samples = []
        last_memory = time.monotonic()
        while time.monotonic() - start < seconds - 200:
            if time.monotonic() - last_memory >= 5:
                last_memory = time.monotonic()
                raw = json.loads(run(["docker", "stats", "--no-stream", "--format", "{{json .}}", service_id(target, "game-server")]))
                memory_samples.append(float(raw["MemPerc"].rstrip("%")))
                result["memorySamples"] = len(memory_samples)
                result["peakContainerMemoryPercent"] = max(memory_samples)
                emit("READY_FOR_PC_TEST")
            if decision_path.exists():
                decision = read_json(decision_path)
                if decision.get("transactionId") != transaction_id: raise RuntimeError("Stale deployment decision")
                if decision.get("accept") is not True: raise RuntimeError("Public PC checks failed/cancelled")
                health(target, ["api", "game-server", "caddy"])
                if any(inspect(target, s).get("RestartCount", 0) != count for s, count in expected_restarts.items()):
                    raise RuntimeError("Unexpected restart during PC acceptance")
                if sha(CONFIG.read_bytes()) != config_hash: raise RuntimeError("Private config changed during acceptance")
                if service_id(target, "mongo") != original_ids["mongo"]: raise RuntimeError("Mongo was recreated during acceptance")
                if target != old:
                    link("previous", old); link("current", target)
                result["steps"]["totalSeconds"] = round(time.monotonic() - start, 3)
                emit("PASS"); return
            time.sleep(1)
        raise RuntimeError("PC acceptance deadline expired")
    except Exception as exc:
        # No daemon restart, DB mutation, secret rewrite, or instance lifecycle calls.
        result["error"] = str(exc) if isinstance(exc, (RuntimeError, ValueError)) else type(exc).__name__
        # Recreating the old GameServer removes candidate logs; retain them before rollback.
        if applied and result.get("measurementSince"):
            try: result["failureMetrics"] = metrics(target, result["measurementSince"])
            except Exception: result["failureMetrics"] = {"status": "UNAVAILABLE"}
        try:
            for service in reversed(applied):
                compose(old, "up", "-d", "--no-build", "--pull", "never", "--no-deps", "--force-recreate", service)
            if applied: health(old, ["api", "game-server", "caddy"])
            emit("ROLLED_BACK" if applied else "BLOCKED")
        except Exception:
            emit("ROLLBACK_FAILED")
        raise RuntimeError(result["status"] + ": " + result["error"]) from None
    finally:
        fcntl.flock(lock, fcntl.LOCK_UN); lock.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=("deploy", "rollback", "status", "decision", "metrics"))
    parser.add_argument("--bundle", type=Path); parser.add_argument("--transaction-id")
    parser.add_argument("--accept", action="store_true"); parser.add_argument("--confirm-empty-migration", action="store_true")
    parser.add_argument("--min-test-seconds", type=int, default=85)
    parser.add_argument("--seconds", type=int, default=1200); parser.add_argument("--since", type=int, default=0)
    args = parser.parse_args()
    os.umask(0o077)
    if os.geteuid() != 0: raise RuntimeError("Use sudo")
    if args.action == "status": print(json.dumps(read_json(ROOT / "deploy-state/state.json"))); return
    if args.action == "decision":
        current = read_json(ROOT / "deploy-state/state.json")
        if current["transactionId"] != args.transaction_id or current["status"] != "READY_FOR_PC_TEST": raise RuntimeError("Stale/not-ready transaction")
        write_json(ROOT / "deploy-state/decision.json", {"transactionId": args.transaction_id, "accept": args.accept}); return
    if args.action == "metrics": print(json.dumps(metrics((ROOT / "current").resolve(), args.since))); return
    if not 180 <= args.seconds <= 1200: raise ValueError("AWS window must be 180..1200 seconds")
    if args.transaction_id and not __import__("re").fullmatch(r"[0-9a-f]{32}", args.transaction_id): raise ValueError("Invalid transaction ID")
    transaction(args.bundle, args.action == "rollback", args.confirm_empty_migration, args.seconds, args.min_test_seconds, args.transaction_id)

if __name__ == "__main__":
    try: main()
    except Exception as exc:
        print(str(exc) if isinstance(exc, (ValueError, RuntimeError)) else type(exc).__name__, file=sys.stderr); sys.exit(1)

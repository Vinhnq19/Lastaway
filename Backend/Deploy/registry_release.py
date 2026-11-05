"""GHCR release format and deterministic inputs; shared by PC, EC2 and tests (stdlib only)."""
from __future__ import annotations
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import tarfile
import tempfile

PACKAGES = {"api": "ghcr.io/vinhnq19/lastaway-api", "game-server": "ghcr.io/vinhnq19/lastaway-game-server"}
SOURCE = "https://github.com/Vinhnq19/Lastaway"
DIGEST = re.compile(r"sha256:[0-9a-f]{64}\Z")
RELEASE_ID = re.compile(r"[a-z0-9][a-z0-9-]{0,79}\Z")
IGNORED = {"bin", "obj", "__pycache__", ".git"}


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=path.parent, prefix=".write-", delete=False) as stream:
        stream.write(json.dumps(value, indent=2, ensure_ascii=False) + "\n")
        temporary = Path(stream.name)
    try: temporary.replace(path)
    finally: temporary.unlink(missing_ok=True)


def run(args, *, cwd=None, timeout=180, env=None, capture=True):
    # Never include stdout/stderr in exceptions: registry/config errors may contain secrets.
    result = subprocess.run([str(x) for x in args], cwd=cwd, env=env, timeout=timeout,
                            stdout=subprocess.PIPE if capture else None,
                            stderr=subprocess.PIPE if capture else None, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        raise RuntimeError(f"{Path(str(args[0])).name} operation failed (exit {result.returncode}); no service changed unless rollback is reported")
    return result.stdout or ""


def inputs(root: Path, service: str) -> list[Path]:
    backend = root / "Backend"
    folders = [backend / ("Api" if service == "api" else "GameServer")]
    files = [backend / "Shared" / x for x in ("RoomContracts.cs", "MatchResultContracts.cs")]
    if service == "game-server":
        folders += [backend / "Shared" / x for x in ("Protocol", "World", "GameData")]
    for folder in folders:
        files += [p for p in folder.rglob("*") if p.is_file() and not (set(p.relative_to(folder).parts) & IGNORED)]
    for folder in (root, backend):
        for pattern in ("global.json", "Directory.Build.*", "Directory.Packages.*", "NuGet.Config", "nuget.config"):
            files += list(folder.glob(pattern))
    files += [backend / ".dockerignore"]
    return sorted(set(files), key=lambda p: p.relative_to(root).as_posix())


def source_bytes(path: Path) -> bytes:
    raw = path.read_bytes()
    if path.suffix == '.meta':
        # Disabled WebGL/Windows Store entries are generated during Windows64 build, with no effect on that player.
        text = raw.decode('utf-8-sig').replace('\r\n', '\n')
        text = re.sub(r'  - serializedVersion: 4\n    buildTarget: (?:WebGL|WindowsStoreApps)\n(?:    [^\n]*\n)*',
            lambda m: '' if '    overridden: 0\n' in m.group() else m.group(), text)
        raw = text.encode('utf-8')
    return raw


def tree_hash(root: Path, files: list[Path]) -> str:
    return sha("".join(f"{p.relative_to(root).as_posix()}:{sha(source_bytes(p))}\n" for p in files).encode())


def fingerprint(root: Path, service: str, bases: dict) -> str:
    return sha(json.dumps({"version": 1, "inputs": tree_hash(root, inputs(root, service)),
        "platform": "linux/amd64", "bases": bases}, sort_keys=True).encode())


def client_source_hash(root: Path) -> str:
    # Gate network/client code, serialized runtime config and protocol data, not unrelated art binaries/importer defaults.
    extensions = {'.cs', '.asmdef', '.asmref', '.unity', '.prefab', '.asset', '.json', '.shader', '.hlsl', '.cginc', '.uss', '.uxml', '.txt', '.bytes', '.csv', '.xml', '.dll'}
    files = [p for p in (root / 'Assets').rglob('*') if p.is_file() and (p.suffix in extensions or p.name.endswith(('.cs.meta', '.asmdef.meta', '.asmref.meta')))]
    for folder in (root / 'Packages', root / 'ProjectSettings', root / 'Backend/Shared'):
        files += [p for p in folder.rglob('*') if p.is_file() and not set(p.parts) & IGNORED]
    return tree_hash(root, sorted(files, key=str))


def build_hash(player: Path) -> str:
    files = [player]
    for name in ("UnityPlayer.dll", "UnityCrashHandler64.exe"):
        if (player.parent / name).is_file(): files.append(player.parent / name)
    data = player.parent / (player.stem + "_Data")
    files += [p for p in data.rglob("*") if p.is_file()]
    return tree_hash(player.parent, sorted(files, key=str))


def required_tier(previous, manifest):
    if previous is None or "services" not in previous: return "Full"
    if previous["services"]["game-server"]["fingerprint"] != manifest["services"]["game-server"]["fingerprint"]: return "Full"
    if previous.get("connectionConfigHash") != manifest["connectionConfigHash"]: return "Full"
    return "Quick"


def validate_manifest(value):
    if value.get("schemaVersion") != 1 or value.get("format") != "ghcr": raise ValueError("Unknown registry release schema")
    if not RELEASE_ID.fullmatch(value.get("releaseId", "")): raise ValueError("Invalid release ID")
    if not re.fullmatch(r"[a-f0-9]{40,64}", value.get("commit", "")): raise ValueError("Missing source commit")
    if value.get("platform") != "linux/amd64": raise ValueError("Wrong image architecture")
    if set(value.get("services", {})) != set(PACKAGES): raise ValueError("Release must include both services")
    for service, image in value["services"].items():
        if not DIGEST.fullmatch(image.get("digest", "")): raise ValueError("Invalid image digest")
        if image.get("image") != PACKAGES[service] + "@" + image["digest"]: raise ValueError("Unexpected image repository")
        if not re.fullmatch(r"[a-f0-9]{64}", image.get("fingerprint", "")): raise ValueError("Missing fingerprint")
    return value


def checksums(folder: Path):
    return {p.relative_to(folder).as_posix(): sha(p.read_bytes()) for p in sorted(folder.rglob("*"))
        if p.is_file() and p.name not in ("SHA256SUMS", "base-images.env")}


def seal(folder: Path):
    (folder / "SHA256SUMS").write_text("".join(f"{digest}  {name}\n" for name, digest in checksums(folder).items()), encoding="utf-8")


def verify(folder: Path):
    expected = {}
    for line in (folder / "SHA256SUMS").read_text(encoding="utf-8").splitlines():
        digest, name = line.split("  ", 1)
        if not re.fullmatch(r"[a-f0-9]{64}", digest) or name in expected: raise ValueError("Bad checksum list")
        expected[name] = digest
    if expected != checksums(folder): raise ValueError("Release checksum/file inventory mismatch")
    for p in folder.rglob("*"):
        if p.is_symlink(): raise ValueError("Release contains symlink")
        if p.is_file() and (p.name.startswith(".env") or p.name == "images.tar.gz"): raise ValueError("Secrets/image archive forbidden in registry release")
    manifest = validate_manifest(read_json(folder / "manifest.json"))
    env = env_file(folder / "release.env")
    if env != {"RELEASE_ID": manifest["releaseId"], "RELEASE_FORMAT": "ghcr",
               "API_IMAGE": manifest["services"]["api"]["image"], "GAME_IMAGE": manifest["services"]["game-server"]["image"]}:
        raise ValueError("Release environment does not match immutable manifest digests")
    return manifest


def unpack(archive: Path, destination: Path):
    with tarfile.open(archive, "r:gz") as tar:
        members = tar.getmembers()
        if len(members) > 300 or sum(m.size for m in members) > 12 * 1024 * 1024: raise ValueError("Oversized release")
        for m in members:
            name = PurePosixPath(m.name)
            if name.is_absolute() or ".." in name.parts or "\\" in m.name or not (m.isfile() or m.isdir()): raise ValueError("Unsafe release archive")
        tar.extractall(destination, members=members, filter="data")
    return verify(destination)


def env_file(path: Path) -> dict:
    """Read our KEY=value files without executing shell substitutions."""
    import shlex
    result = {}
    if path.is_file():
        for line in path.read_text(encoding="utf-8-sig").splitlines():
            if not line.strip() or line.lstrip().startswith("#"): continue
            key, sep, value = line.partition("=")
            if not sep or not re.fullmatch(r"[A-Z][A-Z0-9_]*", key): raise ValueError("Invalid release/config key")
            parsed = shlex.split(value, comments=False)
            result[key] = " ".join(parsed) if parsed else ""
    return result


def effective_env(release: Path, config: Path | None = None):
    result = env_file(release / "base-images.env")
    result.update(env_file(release / "release.env"))
    if config: result.update(env_file(config))
    return result


def validate_metrics(samples, memory_samples, restart_delta, oom):
    """Shared Local/AWS thresholds; a missing/short sample stream is never PASS."""
    if len(samples) < 55 or len(memory_samples) < 55:
        raise RuntimeError("Insufficient server metric coverage for Full acceptance")
    rate = sum(float(row['ticksPerSecond']) for row in samples) / len(samples)
    if not 29 <= rate <= 31 or any(int(row['droppedTicks']) or float(row['cpuOneCorePercent']) > 85 for row in samples):
        raise RuntimeError("Tick/drop/CPU budget failed")
    if max(memory_samples) > 80 or restart_delta or oom:
        raise RuntimeError("RAM/OOM/restart budget failed")
    return {"status": "PASS", "samples": len(samples), "memorySamples": len(memory_samples),
            "meanTickHz": rate, "maxCpuOneCorePercent": max(float(row['cpuOneCorePercent']) for row in samples),
            "peakContainerMemoryPercent": max(memory_samples), "droppedTicks": 0, "restartDelta": restart_delta, "oom": oom}

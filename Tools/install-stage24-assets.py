#!/usr/bin/env python3
"""Install the matching original Stage24 PNG pack into this Unity project safely."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile

PREFIX = "Assets/StreamingAssets/Investigation/"
REQUIRED = {
    "hotspot-idle.png", "hotspot-focus.png", "hotspot-complete.png",
    "inspect-result.png", "notice-new.png", "notice-repeat.png",
    "notice-updated.png", "icon-door.png", "icon-shadow.png",
    "icon-terminal.png", "icon-recorder.png", "icon-waveform.png",
    "icon-sms.png",
}


def install(archive: Path, root: Path) -> int:
    root = root.resolve()
    if not (root / "Assets/Scripts/Runtime/EchoGame.cs").exists():
        raise ValueError("Target path is not the Echo0317 Unity project root")
    destination = root / PREFIX
    destination.mkdir(parents=True, exist_ok=True)
    with ZipFile(archive) as z:
        names = set(z.namelist())
        manifest_file = PREFIX + "manifest.json"
        if manifest_file not in names:
            raise ValueError("The archive is not a Stage24 investigation art pack")
        manifest = json.loads(z.read(manifest_file))
        assets = manifest["assets"]
        files = {entry["file"]: entry for entry in assets}
        if not REQUIRED.issubset(files):
            raise ValueError("Investigation art archive is incomplete")
        for asset in assets:
            name = asset["file"]
            # Only files declared in the manifest, no absolute or relative paths.
            if name != Path(name).name or not name.endswith(".png"):
                raise ValueError("Unsafe image asset path")
            src = PREFIX + name
            if src not in names:
                raise ValueError(f"Missing image: {name}")
            data = z.read(src)
            actual = hashlib.sha256(data).hexdigest()
            if actual != asset["sha256"] or len(data) != asset["bytes"]:
                raise ValueError(f"Image failed SHA-256 validation: {name}")
            (destination / name).write_bytes(data)
        for name in ("manifest.json", "README.md"):
            item = PREFIX + name
            if item in names:
                (destination / name).write_bytes(z.read(item))
    print(f"Installed {len(assets)} verified Stage24 PNG assets into {destination}")
    return len(assets)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--archive", required=True, type=Path)
    parser.add_argument("--root", default=Path("."), type=Path)
    args = parser.parse_args()
    install(args.archive, args.root)

"""Download and verify runtime art/audio assets on demand.

Assets are intentionally excluded from version control because the
continuous-integration environment cannot transmit binary blobs.  This
module downloads the freely licensed Proper Pixel Art scenery, character
animations from the Universal LPC spritesheet project, the pygame
placeholder audio, and the MedievalSharp font from Google Fonts, writing
everything beneath ``game/td/assets``.
"""

from __future__ import annotations

import argparse
import hashlib
import logging
import sys
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple
from urllib.request import Request, urlopen

from td.util.resources import resource_path

LOGGER = logging.getLogger(__name__)

USER_AGENT = "RandomTDAssetFetcher/1.0"

AssetDef = Dict[str, object]

ASSET_DEFINITIONS: Sequence[AssetDef] = (
    {
        "dest": ("assets", "textures", "background.png"),
        "url": "https://raw.githubusercontent.com/KennethJAllen/proper-pixel-art/refs/heads/main/assets/mountain/mountain.png",
        "sha256": "68ca10292481e3c545b8ee3e1b6da3e31d677a0d572b398aa197c73dacb6a5a2",
    },
    {
        "dest": ("assets", "textures", "path.png"),
        "url": "https://raw.githubusercontent.com/KennethJAllen/proper-pixel-art/refs/heads/main/assets/mountain/mesh.png",
        "sha256": "34c68759b649081317274215d293a89effcfad91bb359c0e9ce735365a7f4048",
    },
    {
        "dest": ("assets", "textures", "tower_cannon.png"),
        "url": "https://raw.githubusercontent.com/KennethJAllen/proper-pixel-art/refs/heads/main/assets/anchor/anchor.png",
        "sha256": "fea329b096f57de5dbf5fb9f098e1666e3cfa12b9598918012b620e5b13e2be4",
    },
    {
        "dest": ("assets", "textures", "tower_slow.png"),
        "url": "https://raw.githubusercontent.com/KennethJAllen/proper-pixel-art/refs/heads/main/assets/ash/ash.png",
        "sha256": "3c8fa4ccb034d7a14cf77b34289eebac7c623a438db864e825b230af44b4a82e",
    },
    {
        "dest": ("assets", "textures", "tower_elite.png"),
        "url": "https://raw.githubusercontent.com/KennethJAllen/proper-pixel-art/refs/heads/main/assets/pumpkin/pumpkin.png",
        "sha256": "11685f5d9f244dfffa85b98ce7dc41b11876d2a1007c19f1603f0495b593287f",
        "optional": True,
    },
    {
        "dest": ("assets", "animations", "enemy_walk.png"),
        "url": "https://raw.githubusercontent.com/jrconway3/Universal-LPC-spritesheet/refs/heads/master/_build/_base/female/all.png",
        "sha256": "2bfe216ed3f095db7711c305565b4735330850d5a170b836b98884398ef58f46",
    },
    {
        "dest": ("assets", "animations", "projectile.gif"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/shot.gif",
        "sha256": "6c5d9e63adbdcd0cef0eef3700aa6f79216126ae46e10a4e13df00d2dbdb3c52",
    },
    {
        "dest": ("assets", "animations", "explosion.gif"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/explosion1.gif",
        "sha256": "59871dc1b66a99875a69a3d8162479be46b4027a781789e7365a524b1ff5c6e8",
    },
    {
        "dest": ("assets", "ui", "button_normal.gif"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/blue.gif",
        "sha256": "86a6e00f309e533d0d1e30101d851123148ea516e91dfe90785721f2ec7f7401",
    },
    {
        "dest": ("assets", "ui", "button_down.gif"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/blue.gif",
        "sha256": "86a6e00f309e533d0d1e30101d851123148ea516e91dfe90785721f2ec7f7401",
    },
    {
        "dest": ("assets", "ui", "panel.gif"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/background.gif",
        "sha256": "fb7919c2df7d3055016c1a3e907bcf65756516b481df85d5350596c5f1dbf7bd",
    },
    {
        "dest": ("assets", "sfx", "shoot.wav"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/punch.wav",
        "sha256": "034175c53f1a219e9a348ff931c32a2f281bd447e6748ccf8b8916914e04c107",
        "optional": True,
    },
    {
        "dest": ("assets", "sfx", "death.wav"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/boom.wav",
        "sha256": "91fa16b345550c61efd0949ae3a9d76411b2c3bd3e8e97cfabf077d6a340fc5f",
        "optional": True,
    },
    {
        "dest": ("assets", "sfx", "ui_click.wav"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/whiff.wav",
        "sha256": "14c58cdd79d8b5c7ba98715740260f8334fec6ef90e032726d97e93e31bc7291",
        "optional": True,
    },
    {
        "dest": ("assets", "music", "loop.mp3"),
        "url": "https://raw.githubusercontent.com/pygame/pygame/main/examples/data/house_lo.mp3",
        "sha256": "4749d9517ca6329fd72cf53c4b5cafb2255e593e8c28bb79463a7e5929d5acb4",
        "optional": True,
    },
    {
        "dest": ("assets", "fonts", "MedievalSharp.ttf"),
        "url": "https://raw.githubusercontent.com/google/fonts/main/ofl/medievalsharp/MedievalSharp.ttf",
        "sha256": "74cb2e6738bd7703adf120802f68fba0c9ddb9147a08e6847f1005b1e55df5a5",
        "optional": True,
    },
)

RECOMMENDED_UI_FILES: Sequence[Tuple[str, ...]] = (
    ("assets", "textures", "background.png"),
    ("assets", "ui", "panel.gif"),
    ("assets", "ui", "button_normal.gif"),
    ("assets", "ui", "button_down.gif"),
)


class AssetDownloadError(RuntimeError):
    """Raised when a mandatory asset could not be fetched."""

    def __init__(self, failures: Iterable[Tuple[AssetDef, Exception]]):
        self.failures = list(failures)
        lines = ["Unable to fetch required assets:"]
        for asset, error in self.failures:
            dest = "/".join(asset["dest"])  # type: ignore[index]
            lines.append(f"  - {dest}: {error}")
        super().__init__("\n".join(lines))


def _download(url: str) -> bytes:
    req = Request(url, headers={"User-Agent": USER_AGENT})
    with urlopen(req) as resp:
        return resp.read()


def _sha256(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as fh:
        for chunk in iter(lambda: fh.read(8192), b""):
            h.update(chunk)
    return h.hexdigest()


def ensure_assets(force: bool = False) -> List[Path]:
    """Ensure all known assets are present on disk.

    Returns a list of :class:`Path` objects that were freshly downloaded.
    Optional assets are skipped if the download fails; mandatory assets
    raise :class:`AssetDownloadError` so the caller can handle the failure
    gracefully.
    """

    base_dir = Path(resource_path())
    downloaded: List[Path] = []
    failures: List[Tuple[AssetDef, Exception]] = []
    cache: Dict[str, bytes] = {}

    for asset in ASSET_DEFINITIONS:
        dest_parts: Tuple[str, ...] = asset["dest"]  # type: ignore[assignment]
        dest_path = base_dir.joinpath(*dest_parts)
        expected_hash: Optional[str] = asset.get("sha256")  # type: ignore[assignment]
        optional = bool(asset.get("optional"))

        if dest_path.exists() and not force:
            if expected_hash and _sha256(dest_path) == expected_hash:
                continue
            # Corrupt file -> force refresh
            dest_path.unlink(missing_ok=True)

        dest_path.parent.mkdir(parents=True, exist_ok=True)

        try:
            url: str = asset["url"]  # type: ignore[assignment]
            data = cache.get(url)
            if data is None:
                data = _download(url)
                cache[url] = data
            dest_path.write_bytes(data)
        except Exception as exc:  # pragma: no cover - network failures
            if optional:
                LOGGER.warning("Skipping optional asset %s: %s", dest_path, exc)
                continue
            failures.append((asset, exc))
            continue

        if expected_hash and _sha256(dest_path) != expected_hash:
            if optional:
                LOGGER.warning("Checksum mismatch for optional asset %s", dest_path)
                dest_path.unlink(missing_ok=True)
                continue
            failures.append((asset, ValueError("checksum mismatch")))
            dest_path.unlink(missing_ok=True)
            continue

        downloaded.append(dest_path)

    if failures:
        raise AssetDownloadError(failures)

    return downloaded


def main(argv: Optional[Sequence[str]] = None) -> int:
    parser = argparse.ArgumentParser(description="Download the Kivy TD runtime assets")
    parser.add_argument("--force", action="store_true", help="re-download assets even if they already exist")
    args = parser.parse_args(argv)

    try:
        files = ensure_assets(force=args.force)
    except AssetDownloadError as exc:  # pragma: no cover - manual invocation
        print(exc, file=sys.stderr)
        return 1

    if files:
        print("Downloaded:")
        for path in files:
            print(f"  - {path}")
    else:
        print("All assets already present.")
    return 0


if __name__ == "__main__":  # pragma: no cover - manual usage
    raise SystemExit(main())

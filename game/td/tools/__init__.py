"""Utilities for managing optional runtime assets."""

from importlib import import_module
from typing import Any

__all__ = ("ensure_assets", "AssetDownloadError", "RECOMMENDED_UI_FILES")


def __getattr__(name: str) -> Any:  # pragma: no cover - thin compatibility shim
    if name in __all__:
        module = import_module("td.tools.assets")
        return getattr(module, name)
    raise AttributeError(f"module {__name__!r} has no attribute {name!r}")

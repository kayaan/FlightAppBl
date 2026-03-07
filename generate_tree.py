#!/usr/bin/env python3
"""
Gibt die Dateistruktur des aktuellen Verzeichnisses als Baum aus.
Ignoriert automatisch Build-Ordner wie bin, obj, node_modules etc.
"""

import os
import sys
from pathlib import Path

# Ordner und Dateien die ignoriert werden
IGNORED_DIRS = {
    "bin", "obj", "build", "dist", "out", "output",
    ".git", ".svn", ".hg",
    "node_modules", ".npm", ".yarn",
    "__pycache__", ".pytest_cache", ".mypy_cache",
    ".venv", "venv", "env", ".env",
    ".idea", ".vscode",
    "target",  # Java/Rust
    ".DS_Store",
}

IGNORED_EXTENSIONS = {
    ".pyc", ".pyo", ".pyd",
    ".o", ".obj", ".lib", ".a", ".so", ".dll", ".exe",
    ".class",
    ".log",
}


def should_ignore(name: str) -> bool:
    if name in IGNORED_DIRS:
        return True
    _, ext = os.path.splitext(name)
    if ext in IGNORED_EXTENSIONS:
        return True
    return False


def print_tree(root: Path, prefix: str = "", max_depth: int = None, current_depth: int = 0):
    if max_depth is not None and current_depth >= max_depth:
        return

    try:
        entries = sorted(root.iterdir(), key=lambda e: (e.is_file(), e.name.lower()))
    except PermissionError:
        print(prefix + "  [Kein Zugriff]")
        return

    entries = [e for e in entries if not should_ignore(e.name)]

    for i, entry in enumerate(entries):
        is_last = i == len(entries) - 1
        connector = "└── " if is_last else "├── "
        extension = "    " if is_last else "│   "

        if entry.is_dir():
            print(f"{prefix}{connector}📁 {entry.name}/")
            print_tree(entry, prefix + extension, max_depth, current_depth + 1)
        else:
            size = entry.stat().st_size
            size_str = format_size(size)
            print(f"{prefix}{connector}📄 {entry.name}  ({size_str})")


def format_size(size: int) -> str:
    for unit in ["B", "KB", "MB", "GB"]:
        if size < 1024:
            return f"{size:.0f} {unit}" if unit == "B" else f"{size:.1f} {unit}"
        size /= 1024
    return f"{size:.1f} TB"


def count_entries(root: Path) -> tuple[int, int]:
    dirs, files = 0, 0
    for entry in root.rglob("*"):
        if any(should_ignore(part) for part in entry.parts):
            continue
        if entry.is_dir():
            dirs += 1
        else:
            files += 1
    return dirs, files


if __name__ == "__main__":
    # Startverzeichnis: Argument oder aktuelles Verzeichnis
    start = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(".")
    start = start.resolve()

    print(f"\n📂 {start}\n")
    print_tree(start)

    dirs, files = count_entries(start)
    print(f"\n{dirs} Ordner, {files} Dateien")
    print(f"\nIgnorierte Ordner: {', '.join(sorted(IGNORED_DIRS))}")
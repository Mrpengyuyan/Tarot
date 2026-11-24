#!/usr/bin/env python3
"""Pre-release repository hygiene and secret-boundary scanner."""

from __future__ import annotations

import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Iterable, List, Optional, Sequence

SERVER_DIR = Path(__file__).resolve().parent
REPO_ROOT = SERVER_DIR.parent

FORBIDDEN_TRACKED_FILE_PATTERNS = (
    re.compile(r"(^|/)\.env($|\.(?!example$|production\.example$).+)"),
    re.compile(r"\.(db|sqlite|sqlite3)$", re.IGNORECASE),
)

SECRET_CONTENT_PATTERNS = (
    ("API key token (sk-...)", re.compile(r"sk-[A-Za-z0-9]{20,}")),
    ("Private key header", re.compile(r"-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----")),
    ("GitHub personal access token", re.compile(r"gh[pousr]_[A-Za-z0-9]{20,}")),
    ("AWS access key ID", re.compile(r"AKIA[0-9A-Z]{16}")),
)

FORBIDDEN_CLIENT_CONFIG_KEYS = {"api_key", "apikey", "secret", "secret_key", "token", "password", "deepseek_api_key"}

SCANNABLE_EXTENSIONS = {
    ".py", ".cs", ".json", ".yml", ".yaml", ".md", ".txt", ".ini", ".toml", ".sh", ".example", ".cfg",
}


def scan_text_for_secrets(text: str, source_label: str) -> List[str]:
    findings: List[str] = []
    for label, pattern in SECRET_CONTENT_PATTERNS:
        if pattern.search(text):
            findings.append(f"Potential secret ({label}) detected in {source_label}")
    return findings


def scan_desktop_config(config_path: Path) -> List[str]:
    findings: List[str] = []
    if not config_path.exists():
        return findings
    try:
        raw = config_path.read_text(encoding="utf-8")
        findings.extend(scan_text_for_secrets(raw, str(config_path)))
        payload = json.loads(raw)
        if isinstance(payload, dict):
            for key in payload:
                if str(key).strip().lower() in FORBIDDEN_CLIENT_CONFIG_KEYS:
                    findings.append(f"Forbidden secret-like key '{key}' in desktop config {config_path.name}")
    except Exception as exc:
        findings.append(f"Failed to parse desktop config {config_path}: {exc}")
    return findings


def list_tracked_files(repo_root: Path) -> Optional[List[str]]:
    if not (repo_root / ".git").exists():
        return None
    result = subprocess.run(
        ["git", "ls-files"],
        cwd=repo_root,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        return None
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def collect_hygiene_issues(repo_root: Path = REPO_ROOT, tracked_files: Optional[Iterable[str]] = None) -> List[str]:
    issues: List[str] = []
    files = list(tracked_files) if tracked_files is not None else list_tracked_files(repo_root)

    if files is not None:
        for rel_path in files:
            normalized = rel_path.replace("\\", "/")
            for pattern in FORBIDDEN_TRACKED_FILE_PATTERNS:
                if pattern.search(normalized):
                    issues.append(f"Forbidden runtime/secret file tracked by git: {normalized}")
                    break

            abs_path = repo_root / rel_path
            if not abs_path.is_file():
                continue
            if abs_path.suffix.lower() not in SCANNABLE_EXTENSIONS and not abs_path.name.startswith(".env"):
                continue
            try:
                content = abs_path.read_text(encoding="utf-8", errors="ignore")
            except OSError:
                continue
            issues.extend(scan_text_for_secrets(content, normalized))

    desktop_cfg = (
        repo_root
        / "UnityClient"
        / "TarotUnity"
        / "Assets"
        / "StreamingAssets"
        / "tarot_desktop_config.json"
    )
    issues.extend(scan_desktop_config(desktop_cfg))

    for env_template_name in (".env.example", ".env.production.example"):
        env_template = repo_root / "Server" / env_template_name
        if env_template.exists():
            content = env_template.read_text(encoding="utf-8", errors="ignore")
            issues.extend(scan_text_for_secrets(content, f"Server/{env_template_name}"))

    return issues


def main(argv: Optional[Sequence[str]] = None) -> int:
    _ = argv
    issues = collect_hygiene_issues(REPO_ROOT)
    if not issues:
        print("PASS: release hygiene and secret-boundary scan found 0 issues.")
        return 0
    for item in issues:
        print(f"FAIL: {item}")
    return 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))

from __future__ import annotations

import re
import subprocess
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]


def test_env_file_is_not_tracked_by_git():
    if not (PROJECT_ROOT / ".git").exists():
        return

    result = subprocess.run(
        ["git", "ls-files", "--error-unmatch", ".env"],
        cwd=PROJECT_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    assert result.returncode != 0, ".env is tracked by git; remove it and rotate leaked secrets."


def test_env_example_contains_placeholders_only():
    content = (PROJECT_ROOT / ".env.example").read_text(encoding="utf-8")

    secret_match = re.search(r"^SECRET_KEY=(.*)$", content, re.MULTILINE)
    api_key_match = re.search(r"^DEEPSEEK_API_KEY=(.*)$", content, re.MULTILINE)

    assert secret_match is not None
    assert api_key_match is not None

    secret_value = secret_match.group(1).strip()
    api_key_value = api_key_match.group(1).strip()

    assert secret_value and "replace" in secret_value.lower()
    assert api_key_value and "your_" in api_key_value.lower()
    assert not api_key_value.startswith("sk-")

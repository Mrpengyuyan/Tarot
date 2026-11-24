from __future__ import annotations

import re
import subprocess
from pathlib import Path

import pytest

import check_release_hygiene
from app.core.config import Settings


PROJECT_ROOT = Path(__file__).resolve().parents[1]


def test_env_file_is_not_tracked_by_git():
    if not any((parent / ".git").exists() for parent in (PROJECT_ROOT, *PROJECT_ROOT.parents)):
        pytest.skip("Git tracking check requires a repository checkout")

    result = subprocess.run(
        ["git", "ls-files", "--error-unmatch", ".env"],
        cwd=PROJECT_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    assert result.returncode == 1, ".env is tracked by git or its tracking status could not be checked."


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


def test_published_example_secret_is_rejected_in_production():
    content = (PROJECT_ROOT / ".env.example").read_text(encoding="utf-8")
    example_secret = re.search(r"^SECRET_KEY=(.*)$", content, re.MULTILINE).group(1).strip()
    config = Settings(
        SECRET_KEY=example_secret,
        REQUIRE_STRONG_SECRET=True,
        ENVIRONMENT="production",
        AUTH_COOKIE_SECURE=True,
        _env_file=None,
    )

    with pytest.raises(ValueError, match="Weak SECRET_KEY"):
        config.validate_runtime()


def test_release_hygiene_scanner_flags_secrets_and_passes_clean_repo(tmp_path):
    assert check_release_hygiene.scan_text_for_secrets("DEEPSEEK_API_KEY=your_deepseek_api_key_here", "clean") == []
    synthetic_key = "sk-" + ("a" * 24)
    flagged = check_release_hygiene.scan_text_for_secrets(f"DEEPSEEK_API_KEY={synthetic_key}", "leak")
    assert len(flagged) == 1

    bad_client = tmp_path / "tarot_desktop_config.json"
    bad_client.write_text('{"backendBaseUrl":"https://api.example.com/api/v1","api_key":"secret"}', encoding="utf-8")
    assert any("api_key" in item for item in check_release_hygiene.scan_desktop_config(bad_client))

    good_client = tmp_path / "good_desktop_config.json"
    good_client.write_text('{"backendBaseUrl":"https://api.example.com/api/v1","requestTimeoutSeconds":15}', encoding="utf-8")
    assert check_release_hygiene.scan_desktop_config(good_client) == []

    assert check_release_hygiene.collect_hygiene_issues(PROJECT_ROOT.parent) == []


def test_production_env_template_uses_placeholders_and_safe_defaults():
    content = (PROJECT_ROOT / ".env.production.example").read_text(encoding="utf-8")
    assert "ENVIRONMENT=production" in content
    assert "AUTH_COOKIE_SECURE=true" in content
    assert "ALLOW_MOCK_AI_FALLBACK=false" in content
    assert "AI_BUDGET_GUARD_ENABLED=true" in content
    assert "DEEPSEEK_ENABLE_REASONER_FALLBACK=false" in content
    assert "replace-with-a-long-random-secret" in content
    assert "your_deepseek_api_key_here" in content
    assert check_release_hygiene.scan_text_for_secrets(content, ".env.production.example") == []

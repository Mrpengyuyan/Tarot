from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
import time
from pathlib import Path
from typing import Any, Dict, List, Optional


def configure_stdout() -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run a reusable UI smoke test for the tarot reading flow.",
    )
    parser.add_argument("--frontend-url", default="http://localhost:3000")
    parser.add_argument("--api-base-url", default="http://localhost:8000/api/v1")
    parser.add_argument("--username", default="admin")
    parser.add_argument("--password", default="admin123")
    parser.add_argument("--question", default="我最近的工作方向应该如何调整？")
    parser.add_argument(
        "--spread-index",
        type=int,
        default=0,
        help="Zero-based index in the spread selection list.",
    )
    parser.add_argument(
        "--browser-path",
        default=os.environ.get("SMOKE_BROWSER_PATH", "").strip() or None,
        help="Optional browser executable path. Defaults to a detected Edge install.",
    )
    parser.add_argument("--headed", action="store_true", help="Run with a visible browser window.")
    parser.add_argument(
        "--timeout-seconds",
        type=int,
        default=120,
        help="Timeout for waiting on the AI interpretation request.",
    )
    return parser.parse_args()


def resolve_browser_path(explicit_path: Optional[str]) -> Optional[str]:
    discovered_browser = (
        shutil.which("msedge")
        or shutil.which("microsoft-edge")
        or shutil.which("google-chrome")
        or shutil.which("chrome")
        or shutil.which("chromium")
        or shutil.which("chromium-browser")
    )
    candidates = [
        explicit_path,
        os.environ.get("PLAYWRIGHT_EXECUTABLE_PATH"),
        discovered_browser,
        r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        r"C:\Users\%USERNAME%\AppData\Local\Microsoft\Edge\Application\msedge.exe",
    ]

    for candidate in candidates:
        if not candidate:
            continue
        resolved = os.path.expandvars(candidate)
        if Path(resolved).exists():
            return resolved

    return None


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def click_button_by_text(page: Any, label: str, timeout_ms: int = 20000) -> None:
    deadline = time.time() + (timeout_ms / 1000)
    while time.time() < deadline:
        clicked = page.evaluate(
            """
            (targetLabel) => {
              const buttons = Array.from(document.querySelectorAll('button'));
              const button = buttons.find((item) => item.innerText.trim() === targetLabel);
              if (!button) return false;
              button.click();
              return true;
            }
            """,
            label,
        )
        if clicked:
            return
        page.wait_for_timeout(250)

    body = page.locator("body").inner_text()[:2000]
    raise RuntimeError(f"Button not found: {label}\nCurrent page:\n{body}")


def fill_visible_textarea(page: Any, value: str) -> None:
    deadline = time.time() + 10
    textareas = page.locator("textarea")

    while time.time() < deadline:
        count = textareas.count()
        for index in range(count):
            candidate = textareas.nth(index)
            try:
                if (
                    candidate.get_attribute("aria-hidden") != "true"
                    and candidate.is_visible()
                    and candidate.is_editable()
                ):
                    candidate.fill(value)
                    return
            except Exception:
                continue
        page.wait_for_timeout(250)

    raise RuntimeError("Visible textarea not found.")


def click_tarot_back(page: Any) -> None:
    clicked = page.evaluate(
        """
        () => {
          const isVisible = (el) => {
            const rect = el.getBoundingClientRect();
            return rect.width > 0 && rect.height > 0 &&
              rect.bottom > 0 && rect.right > 0 &&
              rect.left < window.innerWidth && rect.top < window.innerHeight;
          };

          const clickableCards = Array.from(document.querySelectorAll('div')).filter((item) => {
            const style = window.getComputedStyle(item);
            const rect = item.getBoundingClientRect();
            return style.cursor === 'pointer'
              && rect.width >= 120
              && rect.height >= 200
              && isVisible(item);
          });
          if (clickableCards.length) {
            clickableCards[0].click();
            return true;
          }

          const tarotTextNodes = Array.from(document.querySelectorAll('svg text')).filter((node) => {
            const text = (node.textContent || '').trim().toUpperCase();
            return text === 'TAROT';
          });
          for (const node of tarotTextNodes) {
            let current = node.parentElement;
            while (current) {
              const style = window.getComputedStyle(current);
              const rect = current.getBoundingClientRect();
              if (style.cursor === 'pointer' && rect.width >= 120 && rect.height >= 200 && isVisible(current)) {
                current.click();
                return true;
              }
              current = current.parentElement;
            }
          }
          return false;
        }
        """,
    )
    require(clicked, "No clickable tarot back found on the draw page.")


def extract_reading_id(records_responses: List[Dict[str, Any]]) -> Optional[int]:
    for item in reversed(records_responses):
        match = re.search(r"/records/(\d+)/draw", item["url"])
        if match:
            return int(match.group(1))
    return None


def normalize_text(value: str) -> str:
    return re.sub(r"\s+", " ", value).strip()


def main() -> int:
    configure_stdout()
    args = parse_args()

    try:
        from playwright.sync_api import sync_playwright
    except ImportError as exc:
        print("Missing dependency: playwright", file=sys.stderr)
        print("Install it with: pip install playwright", file=sys.stderr)
        return 2

    browser_path = resolve_browser_path(args.browser_path)
    records_responses: List[Dict[str, Any]] = []

    def handle_response(response: Any) -> None:
        url = response.url
        if "/api/v1/records/" not in url:
            return
        try:
            body = response.text()
        except Exception as exc:  # pragma: no cover - best effort logging
            body = f"<unreadable:{exc}>"
        records_responses.append(
            {
                "url": url,
                "status": response.status,
                "body": body[:4000],
            }
        )

    with sync_playwright() as playwright:
        launch_kwargs: Dict[str, Any] = {"headless": not args.headed}
        if browser_path:
            launch_kwargs["executable_path"] = browser_path
        browser = playwright.chromium.launch(**launch_kwargs)
        page = browser.new_page(viewport={"width": 1440, "height": 1200})
        page.on("response", handle_response)

        frontend = args.frontend_url.rstrip("/")

        print("[1/7] Login")
        page.goto(f"{frontend}/auth/login", wait_until="domcontentloaded")
        page.wait_for_timeout(1500)
        require(page.locator("input").count() >= 2, "Login form inputs not found.")
        page.locator("input").nth(0).fill(args.username)
        page.locator("input").nth(1).fill(args.password)
        page.locator('button[type="submit"]').click()
        page.wait_for_timeout(3000)

        print("[2/7] Select spread")
        page.goto(f"{frontend}/reading/draw", wait_until="domcontentloaded")
        page.wait_for_timeout(4000)
        spread_cards = page.locator(".MuiCardActionArea-root")
        require(
            spread_cards.count() > args.spread_index,
            f"Spread index {args.spread_index} is out of range.",
        )
        spread_cards.nth(args.spread_index).click(force=True)
        page.wait_for_timeout(800)
        click_button_by_text(page, "下一步")
        page.wait_for_timeout(1000)

        print("[3/7] Enter question")
        fill_visible_textarea(page, args.question)
        page.wait_for_timeout(500)
        click_button_by_text(page, "进入抽牌")
        page.wait_for_timeout(1500)

        print("[4/7] Draw cards")
        click_button_by_text(page, "开始抽牌")
        page.wait_for_timeout(12000)
        require("点击牌面翻开" in page.locator("body").inner_text(), "Draw stage did not render correctly.")

        print("[5/7] Flip cards and wait for AI interpretation")
        click_tarot_back(page)
        deadline = time.time() + args.timeout_seconds
        interpret_response: Optional[Dict[str, Any]] = None
        while time.time() < deadline:
            for item in records_responses:
                if "/interpret" in item["url"]:
                    interpret_response = item
                    break
            if interpret_response is not None:
                break
            page.wait_for_timeout(1000)

        require(interpret_response is not None, "AI interpretation request was not observed.")
        require(
            int(interpret_response["status"]) == 200,
            f"AI interpretation failed: {interpret_response['status']} {interpret_response['body']}",
        )
        try:
            interpret_payload = json.loads(interpret_response["body"])
        except json.JSONDecodeError:
            raise RuntimeError(f"Interpretation response was not valid JSON: {interpret_response['body']}")

        overall_interpretation = normalize_text(str(interpret_payload.get("overall_interpretation") or ""))
        require(bool(overall_interpretation), "Interpretation payload missing overall_interpretation.")
        interpretation_snippet = overall_interpretation[:18]

        page.wait_for_timeout(5000)
        draw_body = normalize_text(page.locator("body").inner_text())
        require(
            interpretation_snippet in draw_body,
            "Same-page interpretation section was not rendered.",
        )

        print("[6/7] Validate detail page persistence")
        reading_id = extract_reading_id(records_responses)
        require(reading_id is not None, "Failed to extract reading id from draw response.")
        page.goto(f"{frontend}/reading/{reading_id}", wait_until="domcontentloaded")
        page.wait_for_timeout(10000)
        detail_body = normalize_text(page.locator("body").inner_text())
        require(
            interpretation_snippet in detail_body,
            "Detail page does not show persisted interpretation.",
        )

        print("[7/7] Summary")
        summary = {
            "reading_id": reading_id,
            "interpret_status": interpret_response["status"],
            "frontend_url": frontend,
            "api_base_url": args.api_base_url,
            "overall_interpretation_preview": draw_body[:160],
            "interpret_response": interpret_response,
        }
        print(json.dumps(summary, ensure_ascii=False, indent=2))
        browser.close()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

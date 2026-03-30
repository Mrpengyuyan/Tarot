from __future__ import annotations

import argparse
import json
import sys
from typing import Any, Dict

from smoke_ui_interpretation import configure_stdout, require, resolve_browser_path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Run a frontend smoke test for dashboard, cards and spreads pages.",
    )
    parser.add_argument("--frontend-url", default="http://localhost:3000")
    parser.add_argument("--username", default="demo_user")
    parser.add_argument("--password", default="demo12345")
    parser.add_argument("--browser-path", default=None)
    parser.add_argument("--headed", action="store_true")
    return parser.parse_args()


def login(page: Any, frontend_url: str, username: str, password: str) -> None:
    page.goto(f"{frontend_url.rstrip('/')}/auth/login", wait_until="domcontentloaded")
    page.wait_for_timeout(1500)
    require(page.locator("input").count() >= 2, "Login form inputs not found.")
    page.locator("input").nth(0).fill(username)
    page.locator("input").nth(1).fill(password)
    page.locator('button[type="submit"]').click()
    page.wait_for_timeout(3000)


def collect_page_text(page: Any, url: str) -> str:
    page.goto(url, wait_until="domcontentloaded")
    page.wait_for_timeout(3500)
    return page.locator("body").inner_text()


def main() -> int:
    configure_stdout()
    args = parse_args()

    try:
        from playwright.sync_api import sync_playwright
    except ImportError:
        print("Missing dependency: playwright", file=sys.stderr)
        print("Install it with: pip install playwright", file=sys.stderr)
        return 2

    browser_path = resolve_browser_path(args.browser_path)
    frontend = args.frontend_url.rstrip("/")

    with sync_playwright() as playwright:
        launch_kwargs: Dict[str, Any] = {"headless": not args.headed}
        if browser_path:
            launch_kwargs["executable_path"] = browser_path
        browser = playwright.chromium.launch(**launch_kwargs)
        page = browser.new_page(viewport={"width": 1440, "height": 1200})

        login(page, frontend, args.username, args.password)

        dashboard_text = collect_page_text(page, f"{frontend}/dashboard")
        require("最近记录" in dashboard_text, "Dashboard missing recent records section.")
        require("数据概览" in dashboard_text, "Dashboard missing stats overview section.")
        require("解读摘要" in dashboard_text, "Dashboard missing AI summary block.")
        require("404" not in dashboard_text, "Dashboard rendered a 404 page.")

        cards_text = collect_page_text(page, f"{frontend}/cards")
        require("塔罗牌库" in cards_text, "Cards page missing title.")
        require("大阿卡纳" in cards_text, "Cards page missing major arcana section.")
        require(("愚者" in cards_text) or ("总牌数" in cards_text), "Cards page did not render card library data.")
        require("404" not in cards_text, "Cards page rendered a 404 page.")

        spreads_text = collect_page_text(page, f"{frontend}/spreads")
        require("牌阵目录" in spreads_text, "Spreads page missing title.")
        require("位置 1" in spreads_text, "Spreads page missing position layout blocks.")
        require(("单牌" in spreads_text) or ("牌阵总数" in spreads_text), "Spreads page did not render spread data.")
        require("404" not in spreads_text, "Spreads page rendered a 404 page.")

        summary = {
            "dashboard_preview": dashboard_text[:220],
            "cards_preview": cards_text[:220],
            "spreads_preview": spreads_text[:220],
            "frontend_url": frontend,
        }
        print(json.dumps(summary, ensure_ascii=False, indent=2))
        browser.close()

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

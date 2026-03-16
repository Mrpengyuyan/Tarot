#!/usr/bin/env python3
"""
End-to-end API verification for records + interpretation flow.

Flow:
1) login
2) get spreads
3) create record
4) draw cards
5) get cards
6) request interpretation
7) poll interpretation endpoint
8) verify appended server.log lines include expected route hits
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import time
import uuid
from dataclasses import dataclass, asdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple

import requests


@dataclass
class StepResult:
    name: str
    ok: bool
    status_code: Optional[int] = None
    detail: str = ""
    elapsed_ms: Optional[int] = None
    data_preview: Optional[Dict[str, Any]] = None


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _preview(value: Any, max_len: int = 240) -> Dict[str, Any]:
    if value is None:
        return {}
    try:
        text = json.dumps(value, ensure_ascii=False)
    except Exception:
        text = str(value)
    if len(text) > max_len:
        text = text[:max_len] + "...<truncated>"
    return {"value": text}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Verify records -> interpret API flow and logs.")
    parser.add_argument("--base-url", default="http://localhost:8000/api/v1", help="API base URL")
    parser.add_argument("--username", default=os.getenv("TAROT_TEST_USERNAME", "testuser"))
    parser.add_argument("--password", default=os.getenv("TAROT_TEST_PASSWORD", "testpass123"))
    parser.add_argument("--question-type", default="general", choices=["love", "career", "finance", "health", "general"])
    parser.add_argument("--spread-name", default="", help="Optional spread name. Uses first spread if omitted.")
    parser.add_argument("--request-timeout", type=float, default=20.0, help="Timeout for normal API requests")
    parser.add_argument("--interpret-timeout", type=float, default=90.0, help="Timeout for interpretation POST")
    parser.add_argument("--poll-attempts", type=int, default=20, help="Interpretation polling attempts")
    parser.add_argument("--poll-interval", type=float, default=2.0, help="Seconds between polls")
    parser.add_argument("--log-file", default="server.log", help="Server log file path for hit verification")
    return parser.parse_args()


def read_log_offset(path: Path) -> int:
    if not path.exists():
        return 0
    return path.stat().st_size


def read_new_log_lines(path: Path, offset: int) -> List[str]:
    if not path.exists():
        return []
    with path.open("rb") as f:
        f.seek(offset)
        payload = f.read()
    text = payload.decode("utf-8", errors="ignore")
    return [line.strip() for line in text.splitlines() if line.strip()]


def request_json(
    session: requests.Session,
    method: str,
    url: str,
    *,
    timeout: float,
    headers: Optional[Dict[str, str]] = None,
    params: Optional[Dict[str, Any]] = None,
    json_body: Optional[Dict[str, Any]] = None,
    form_data: Optional[Dict[str, Any]] = None,
) -> Tuple[int, Any, str, int]:
    started = time.time()
    resp = session.request(
        method=method,
        url=url,
        timeout=timeout,
        headers=headers,
        params=params,
        json=json_body,
        data=form_data,
    )
    elapsed_ms = int((time.time() - started) * 1000)
    try:
        payload = resp.json()
    except Exception:
        payload = None
    return resp.status_code, payload, resp.text, elapsed_ms


def verify_log_hits(lines: List[str], reading_id: Optional[int]) -> Dict[str, Any]:
    # Parse standard uvicorn access log lines:
    # INFO: ... - "METHOD /path?query HTTP/1.1" 200 OK
    parsed: List[Tuple[str, str, int]] = []
    line_re = re.compile(r'"([A-Z]+) ([^" ]+) HTTP/1\.1" (\d{3})')
    for line in lines:
        m = line_re.search(line)
        if not m:
            continue
        method = m.group(1)
        raw_path = m.group(2)
        path_without_query = raw_path.split("?", 1)[0]
        try:
            status = int(m.group(3))
        except ValueError:
            continue
        parsed.append((method, path_without_query, status))

    checks: List[Tuple[str, str, str]] = [
        ("login", "POST", "/api/v1/login"),
        ("spreads", "GET", "/api/v1/spreads/"),
        ("records_create", "POST", "/api/v1/records/"),
    ]
    if reading_id is not None:
        rid = str(reading_id)
        checks.extend(
            [
                ("draw", "POST", f"/api/v1/records/{rid}/draw"),
                ("cards", "GET", f"/api/v1/records/{rid}/cards"),
                ("interpret", "POST", f"/api/v1/records/{rid}/interpret"),
                ("detail", "GET", f"/api/v1/records/{rid}"),
            ]
        )

    results: Dict[str, Any] = {}
    for name, method, expected_path in checks:
        statuses = [
            status
            for log_method, log_path, status in parsed
            if log_method == method and log_path == expected_path
        ]
        results[name] = {
            "hit": len(statuses) > 0,
            "statuses": statuses,
        }

    missing = [name for name, item in results.items() if not item["hit"]]
    return {
        "results": results,
        "missing_routes": missing,
        "all_hit": len(missing) == 0,
        "appended_line_count": len(lines),
    }


def choose_spread(spreads: List[Dict[str, Any]], spread_name: str) -> Optional[Dict[str, Any]]:
    if not spreads:
        return None
    if not spread_name:
        return spreads[0]
    for spread in spreads:
        if str(spread.get("name", "")).strip() == spread_name.strip():
            return spread
    return None


def main() -> int:
    args = parse_args()
    base_url = args.base_url.rstrip("/")
    trace_id = uuid.uuid4().hex[:12]
    started_at = utc_now()

    script_root = Path(__file__).resolve().parent
    reports_dir = script_root / "reports"
    reports_dir.mkdir(parents=True, exist_ok=True)
    report_path = reports_dir / f"flow_report_{trace_id}.json"

    log_file_path = Path(args.log_file)
    log_file_for_report = args.log_file
    if not log_file_path.is_absolute():
        # default expected runtime is code/ directory
        log_file_path = (Path.cwd() / log_file_path).resolve()
    log_offset = read_log_offset(log_file_path)

    session = requests.Session()
    # Avoid local API calls being routed to corporate/system HTTP proxy.
    session.trust_env = False
    results: List[StepResult] = []
    token: Optional[str] = None
    reading_id: Optional[int] = None
    spread_used: Optional[Dict[str, Any]] = None
    interpretation_payload: Optional[Dict[str, Any]] = None

    def add_step(step: StepResult) -> None:
        results.append(step)
        status_text = f" status={step.status_code}" if step.status_code is not None else ""
        print(f"[{'OK' if step.ok else 'FAIL'}] {step.name}{status_text} {step.detail}")

    # Step 1: login
    try:
        status, payload, raw, elapsed = request_json(
            session,
            "POST",
            f"{base_url}/login",
            timeout=args.request_timeout,
            form_data={"username": args.username, "password": args.password},
        )
        if status == 200 and isinstance(payload, dict) and payload.get("access_token"):
            token = str(payload["access_token"])
            add_step(
                StepResult(
                    name="login",
                    ok=True,
                    status_code=status,
                    detail="token received",
                    elapsed_ms=elapsed,
                )
            )
        else:
            add_step(
                StepResult(
                    name="login",
                    ok=False,
                    status_code=status,
                    detail=f"unexpected response: {raw[:200]}",
                    elapsed_ms=elapsed,
                    data_preview=_preview(payload),
                )
            )
    except Exception as exc:
        add_step(StepResult(name="login", ok=False, detail=f"exception: {exc}"))

    headers = {"Authorization": f"Bearer {token}"} if token else {}

    # Step 2: spreads
    spreads: List[Dict[str, Any]] = []
    if token:
        try:
            status, payload, raw, elapsed = request_json(
                session,
                "GET",
                f"{base_url}/spreads/",
                timeout=args.request_timeout,
                headers=headers,
            )
            if status == 200 and isinstance(payload, list) and payload:
                spreads = payload
                spread_used = choose_spread(spreads, args.spread_name)
                if spread_used is None:
                    add_step(
                        StepResult(
                            name="get_spreads",
                            ok=False,
                            status_code=status,
                            detail=f"spread '{args.spread_name}' not found",
                            elapsed_ms=elapsed,
                            data_preview=_preview(payload[:2]),
                        )
                    )
                else:
                    add_step(
                        StepResult(
                            name="get_spreads",
                            ok=True,
                            status_code=status,
                            detail=f"selected spread_id={spread_used.get('id')} name={spread_used.get('name')}",
                            elapsed_ms=elapsed,
                        )
                    )
            else:
                add_step(
                    StepResult(
                        name="get_spreads",
                        ok=False,
                        status_code=status,
                        detail=f"unexpected response: {raw[:200]}",
                        elapsed_ms=elapsed,
                        data_preview=_preview(payload),
                    )
                )
        except Exception as exc:
            add_step(StepResult(name="get_spreads", ok=False, detail=f"exception: {exc}"))

    # Step 3: create record
    if token and spread_used:
        question = f"[trace:{trace_id}] Integration check at {started_at}"
        body = {
            "spread_type_id": int(spread_used["id"]),
            "question": question,
            "question_type": args.question_type,
        }
        try:
            status, payload, raw, elapsed = request_json(
                session,
                "POST",
                f"{base_url}/records/",
                timeout=args.request_timeout,
                headers=headers,
                json_body=body,
            )
            if status == 200 and isinstance(payload, dict) and payload.get("id") is not None:
                reading_id = int(payload["id"])
                add_step(
                    StepResult(
                        name="create_record",
                        ok=True,
                        status_code=status,
                        detail=f"reading_id={reading_id}",
                        elapsed_ms=elapsed,
                    )
                )
            else:
                add_step(
                    StepResult(
                        name="create_record",
                        ok=False,
                        status_code=status,
                        detail=f"unexpected response: {raw[:200]}",
                        elapsed_ms=elapsed,
                        data_preview=_preview(payload),
                    )
                )
        except Exception as exc:
            add_step(StepResult(name="create_record", ok=False, detail=f"exception: {exc}"))

    # Step 4: draw cards
    if token and reading_id is not None:
        try:
            status, payload, raw, elapsed = request_json(
                session,
                "POST",
                f"{base_url}/records/{reading_id}/draw",
                timeout=args.request_timeout,
                headers=headers,
            )
            ok = status == 200
            detail = "draw success" if ok else f"unexpected response: {raw[:200]}"
            add_step(
                StepResult(
                    name="draw_cards",
                    ok=ok,
                    status_code=status,
                    detail=detail,
                    elapsed_ms=elapsed,
                    data_preview=None if ok else _preview(payload),
                )
            )
        except Exception as exc:
            add_step(StepResult(name="draw_cards", ok=False, detail=f"exception: {exc}"))

    # Step 5: get cards
    if token and reading_id is not None:
        try:
            status, payload, raw, elapsed = request_json(
                session,
                "GET",
                f"{base_url}/records/{reading_id}/cards",
                timeout=args.request_timeout,
                headers=headers,
            )
            ok = status == 200 and isinstance(payload, list) and len(payload) > 0
            detail = f"cards={len(payload) if isinstance(payload, list) else 0}"
            if not ok:
                detail = f"unexpected response: {raw[:200]}"
            add_step(
                StepResult(
                    name="get_cards",
                    ok=ok,
                    status_code=status,
                    detail=detail,
                    elapsed_ms=elapsed,
                    data_preview=None if ok else _preview(payload),
                )
            )
        except Exception as exc:
            add_step(StepResult(name="get_cards", ok=False, detail=f"exception: {exc}"))

    # Step 6: request interpretation
    interpret_status: Optional[int] = None
    interpret_error: Optional[str] = None
    if token and reading_id is not None:
        try:
            status, payload, raw, elapsed = request_json(
                session,
                "POST",
                f"{base_url}/records/{reading_id}/interpret",
                timeout=args.interpret_timeout,
                headers=headers,
                params={"force_ai": True},
            )
            interpret_status = status
            if status == 200 and isinstance(payload, dict):
                interpretation_payload = payload
                add_step(
                    StepResult(
                        name="create_interpretation",
                        ok=True,
                        status_code=status,
                        detail="interpretation returned",
                        elapsed_ms=elapsed,
                        data_preview=_preview({"overall_interpretation": payload.get("overall_interpretation", "")}),
                    )
                )
            else:
                interpret_error = raw[:300]
                add_step(
                    StepResult(
                        name="create_interpretation",
                        ok=False,
                        status_code=status,
                        detail=f"unexpected response: {raw[:200]}",
                        elapsed_ms=elapsed,
                        data_preview=_preview(payload),
                    )
                )
        except Exception as exc:
            interpret_error = str(exc)
            add_step(StepResult(name="create_interpretation", ok=False, detail=f"exception: {exc}"))

    # Step 7: poll interpretation endpoint if needed
    polled_interpretation: Optional[Dict[str, Any]] = None
    if token and reading_id is not None and interpretation_payload is None:
        for i in range(max(0, args.poll_attempts)):
            try:
                status, payload, raw, elapsed = request_json(
                    session,
                    "GET",
                    f"{base_url}/records/{reading_id}/interpretation",
                    timeout=args.request_timeout,
                    headers=headers,
                )
                if status == 200 and isinstance(payload, dict):
                    polled_interpretation = payload
                    add_step(
                        StepResult(
                            name="poll_interpretation",
                            ok=True,
                            status_code=status,
                            detail=f"ready after attempt={i + 1}",
                            elapsed_ms=elapsed,
                            data_preview=_preview({"overall_interpretation": payload.get("overall_interpretation", "")}),
                        )
                    )
                    break
                if status != 404:
                    add_step(
                        StepResult(
                            name="poll_interpretation",
                            ok=False,
                            status_code=status,
                            detail=f"unexpected response: {raw[:180]}",
                            elapsed_ms=elapsed,
                            data_preview=_preview(payload),
                        )
                    )
                    break
            except Exception as exc:
                add_step(StepResult(name="poll_interpretation", ok=False, detail=f"exception: {exc}"))
                break

            time.sleep(max(0.0, args.poll_interval))

    # Step 8: fetch detail
    if token and reading_id is not None:
        try:
            status, payload, raw, elapsed = request_json(
                session,
                "GET",
                f"{base_url}/records/{reading_id}",
                timeout=args.request_timeout,
                headers=headers,
            )
            ok = status == 200 and isinstance(payload, dict)
            add_step(
                StepResult(
                    name="get_record_detail",
                    ok=ok,
                    status_code=status,
                    detail="detail loaded" if ok else f"unexpected response: {raw[:200]}",
                    elapsed_ms=elapsed,
                    data_preview=None if ok else _preview(payload),
                )
            )
        except Exception as exc:
            add_step(StepResult(name="get_record_detail", ok=False, detail=f"exception: {exc}"))

    # Log verification
    new_lines = read_new_log_lines(log_file_path, log_offset)
    log_result = verify_log_hits(new_lines, reading_id)

    final_interpretation = interpretation_payload or polled_interpretation or {}
    overall_text = ""
    if isinstance(final_interpretation, dict):
        overall_text = str(final_interpretation.get("overall_interpretation", "")).strip()

    finished_at = utc_now()
    report: Dict[str, Any] = {
        "trace_id": trace_id,
        "started_at": started_at,
        "finished_at": finished_at,
        "config": {
            "base_url": base_url,
            "username": args.username,
            "question_type": args.question_type,
            "spread_name": args.spread_name,
            "log_file": log_file_for_report,
        },
        "runtime": {
            "reading_id": reading_id,
            "spread_used": spread_used,
            "interpret_status": interpret_status,
            "interpret_error": interpret_error,
        },
        "steps": [asdict(s) for s in results],
        "interpretation": {
            "has_interpretation": bool(overall_text),
            "overall_interpretation_preview": overall_text[:240],
        },
        "log_verification": log_result,
    }

    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    print("")
    print("==== SUMMARY ====")
    ok_steps = sum(1 for s in results if s.ok)
    print(f"Trace ID: {trace_id}")
    print(f"Reading ID: {reading_id}")
    print(f"Steps OK: {ok_steps}/{len(results)}")
    print(f"Interpretation available: {bool(overall_text)}")
    print(f"Log all-hit: {log_result['all_hit']}")
    if log_result["missing_routes"]:
        print(f"Missing routes: {', '.join(log_result['missing_routes'])}")
    print(f"Report: {report_path}")

    # Fail only when core API chain fails or interpret route is not observed in logs.
    core_failed = any(
        (s.name in {"login", "get_spreads", "create_record", "draw_cards", "get_cards"} and not s.ok)
        for s in results
    )
    interpret_log_missing = "interpret" in log_result.get("missing_routes", [])
    return 1 if (core_failed or interpret_log_missing) else 0


if __name__ == "__main__":
    sys.exit(main())

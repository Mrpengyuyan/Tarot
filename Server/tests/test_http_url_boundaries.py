import ast
import http.client
import json
from pathlib import Path
import socket
import threading
import time
from urllib.parse import urlencode

import httptools
import pytest
import uvicorn
from uvicorn.protocols.http.httptools_impl import HttpToolsProtocol


MAX_URL_BYTES = 65_535
QUERY_PREFIX = b"/probe?q="
OVERSIZED_TARGETS = [
    b"/" + b"p" * MAX_URL_BYTES,
    QUERY_PREFIX + b"q" * (MAX_URL_BYTES + 1 - len(QUERY_PREFIX)),
    ("/probe?" + urlencode({"q": "\u4e2d" * 16666 + "x"})).encode("ascii"),
    ("/probe?" + urlencode({"q": "\U0001f52e" * 12499 + "xxx"})).encode("ascii"),
]
VALID_TARGETS = [
    b"/" + b"p" * (MAX_URL_BYTES - 1),
    QUERY_PREFIX + b"q" * (MAX_URL_BYTES - len(QUERY_PREFIX)),
    ("/probe?" + urlencode({"q": "English \u4e2d\U0001f52e literal \\ud800"})).encode("ascii"),
    b"/api/v1/cards/?search=abc%25_def",
]
TARGET_IDS = ["path-boundary", "query-boundary", "chinese", "emoji"]


def test_both_dependency_manifests_pin_the_fixed_http_parser():
    root = Path(__file__).resolve().parents[1]
    requirements = (root / "requirements.txt").read_text(encoding="utf-8").splitlines()
    setup_tree = ast.parse((root / "setup.py").read_text(encoding="utf-8"))
    setup_call = next(node for node in ast.walk(setup_tree)
                      if isinstance(node, ast.Call) and isinstance(node.func, ast.Name) and node.func.id == "setup")
    dependencies = ast.literal_eval(next(keyword.value for keyword in setup_call.keywords
                                         if keyword.arg == "install_requires"))
    assert "httptools==0.9.0" in requirements
    assert "httptools==0.9.0" in dependencies


@pytest.mark.parametrize("target", OVERSIZED_TARGETS, ids=TARGET_IDS)
def test_parser_rejects_oversized_targets_instead_of_truncating(target):
    assert len(target) > MAX_URL_BYTES
    with pytest.raises(httptools.HttpParserInvalidURLError):
        httptools.parse_url(target)


@pytest.mark.parametrize("target", VALID_TARGETS, ids=["max-path", "max-query", "unicode", "wildcards"])
def test_parser_preserves_supported_paths_and_queries(target):
    path, _, query = target.partition(b"?")
    parsed = httptools.parse_url(target)
    assert parsed.path == path
    assert (parsed.query or b"") == query


@pytest.fixture(params=["auto", "httptools"])
def http_server(request):
    received = []

    async def echo(scope, receive, send):
        received.append((scope["raw_path"], scope["query_string"], scope["method"]))
        body = json.dumps({"path": scope["raw_path"].decode("ascii"),
                           "query": scope["query_string"].decode("ascii")}).encode("ascii")
        await send({"type": "http.response.start", "status": 200,
                    "headers": [(b"content-type", b"application/json"),
                                (b"content-length", str(len(body)).encode("ascii"))]})
        await send({"type": "http.response.body", "body": body})

    with socket.create_server(("127.0.0.1", 0)) as listener:
        config = uvicorn.Config(echo, http=request.param, loop="asyncio", lifespan="off", ws="none",
                                access_log=False, log_level="error", log_config=None)
        config.load()
        assert config.http_protocol_class is HttpToolsProtocol
        server = uvicorn.Server(config)
        thread = threading.Thread(target=server.run, kwargs={"sockets": [listener]}, daemon=True)
        thread.start()
        try:
            deadline = time.monotonic() + 10
            while not server.started and thread.is_alive() and time.monotonic() < deadline:
                time.sleep(0.01)
            assert server.started, "Isolated HTTP server did not start"
            yield listener.getsockname()[1], received
        finally:
            server.should_exit = True
            thread.join(timeout=10)
            assert not thread.is_alive(), "Isolated HTTP server did not stop"


def _request(port, method, target):
    connection = http.client.HTTPConnection("127.0.0.1", port, timeout=10)
    try:
        connection.request(method, target.decode("ascii"))
        response = connection.getresponse()
        return response.status, response.read()
    finally:
        connection.close()


@pytest.mark.parametrize("target", OVERSIZED_TARGETS, ids=TARGET_IDS)
@pytest.mark.parametrize("method", ["GET", "POST"])
def test_real_http_rejects_oversized_targets_before_running_application(http_server, target, method):
    port, received = http_server
    status, body = _request(port, method, target)
    assert status == 400
    assert received == []
    assert b"Invalid HTTP request received" in body
    status, body = _request(port, "GET", b"/healthy")
    assert status == 200
    assert json.loads(body) == {"path": "/healthy", "query": ""}
    assert received == [(b"/healthy", b"", "GET")]


@pytest.mark.parametrize("target", VALID_TARGETS, ids=["max-path", "max-query", "unicode", "wildcards"])
def test_real_http_keeps_supported_targets_complete(http_server, target):
    port, received = http_server
    path, _, query = target.partition(b"?")
    status, body = _request(port, "GET", target)
    assert status == 200
    assert json.loads(body) == {"path": path.decode("ascii"), "query": query.decode("ascii")}
    assert received == [(path, query, "GET")]

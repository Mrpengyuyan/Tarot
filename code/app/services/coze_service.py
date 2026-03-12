import asyncio
import json
import logging
from typing import Any, Dict, List, Optional

import httpx

from app.core.config import settings

logger = logging.getLogger(__name__)


class CozeError(Exception):
    """Error raised when Coze request/response handling fails."""


class CozeService:
    def __init__(self) -> None:
        self.api_key = settings.COZE_API_KEY
        self.bot_id = settings.COZE_BOT_ID
        self.base_url = settings.COZE_BASE_URL.rstrip("/")
        self.timeout = float(settings.COZE_TIMEOUT or 65.0)
        self.primary_chat_endpoint = (settings.COZE_CHAT_ENDPOINT or "open_api/v2/chat").strip("/")

    def is_configured(self) -> bool:
        return bool(self.api_key and self.bot_id)

    def _headers(self) -> Dict[str, str]:
        return {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json",
        }

    async def _request(
        self,
        method: str,
        endpoint: str,
        payload: Optional[Dict[str, Any]] = None,
        timeout: Optional[float] = None,
    ) -> Dict[str, Any]:
        if not self.is_configured():
            raise CozeError("Coze is not configured. Please set COZE_API_KEY and COZE_BOT_ID.")

        url = f"{self.base_url}/{endpoint.lstrip('/')}"
        request_timeout = float(timeout) if timeout else self.timeout
        try:
            async with httpx.AsyncClient(timeout=request_timeout) as client:
                if method.upper() == "POST":
                    response = await client.post(url, headers=self._headers(), json=payload)
                elif method.upper() == "GET":
                    response = await client.get(url, headers=self._headers(), params=payload)
                else:
                    raise CozeError(f"Unsupported HTTP method: {method}")
        except httpx.TimeoutException as exc:
            raise CozeError(f"Coze request timeout ({request_timeout}s)") from exc
        except httpx.RequestError as exc:
            raise CozeError(f"Coze request error: {exc}") from exc

        if response.status_code >= 400:
            text = response.text[:500]
            raise CozeError(f"Coze HTTP {response.status_code}: {text}")

        try:
            return response.json()
        except json.JSONDecodeError as exc:
            raise CozeError(f"Coze returned non-JSON response: {response.text[:200]}") from exc

    @staticmethod
    def _normalize_content(content: Any) -> Optional[str]:
        if isinstance(content, str):
            stripped = content.strip()
            if not stripped:
                return None
            # Some providers wrap text as JSON string payload.
            if stripped.startswith("{") and stripped.endswith("}"):
                try:
                    obj = json.loads(stripped)
                    if isinstance(obj, dict):
                        for key in ("text", "content", "output", "answer"):
                            value = obj.get(key)
                            if isinstance(value, str) and value.strip():
                                return value.strip()
                except Exception:
                    pass
            return stripped

        if isinstance(content, dict):
            for key in ("text", "content", "output", "answer"):
                value = content.get(key)
                if isinstance(value, str) and value.strip():
                    return value.strip()
            try:
                return json.dumps(content, ensure_ascii=False)
            except Exception:
                return str(content)

        return None

    @staticmethod
    def _extract_text_from_message(message: Dict[str, Any]) -> Optional[str]:
        if not isinstance(message, dict):
            return None

        role = str(message.get("role", "")).lower()
        msg_type = str(message.get("type", "")).lower()
        content = message.get("content")
        if content is None:
            content = message.get("answer")
        if content is None:
            content = message.get("output")

        if role not in {"assistant", "bot", ""}:
            return None
        if msg_type and msg_type not in {"answer", "text", "output"}:
            return None

        return CozeService._normalize_content(content)

    def _extract_answer_from_messages(self, messages: Any) -> Optional[str]:
        if isinstance(messages, list):
            for message in reversed(messages):
                text = self._extract_text_from_message(message)
                if text:
                    return text
        return None

    def _extract_answer_text(self, result: Dict[str, Any]) -> Optional[str]:
        # Top-level direct candidates
        for key in ("answer", "output", "content", "message"):
            value = result.get(key)
            normalized = self._normalize_content(value)
            if normalized:
                return normalized

        # Standard "messages" collection
        top_messages = result.get("messages")
        top_text = self._extract_answer_from_messages(top_messages)
        if top_text:
            return top_text

        # Nested "data" payload variants
        data = result.get("data")
        if isinstance(data, list):
            list_text = self._extract_answer_from_messages(data)
            if list_text:
                return list_text

        if isinstance(data, dict):
            for key in ("answer", "output", "content", "message"):
                value = data.get(key)
                normalized = self._normalize_content(value)
                if normalized:
                    return normalized

            nested_messages = data.get("messages")
            nested_text = self._extract_answer_from_messages(nested_messages)
            if nested_text:
                return nested_text

        return None

    async def _send_message_v3(self, message: str, user_id: str, max_wait_time: int) -> str:
        create_payload = {
            "bot_id": self.bot_id,
            "user_id": user_id,
            "stream": False,
            "auto_save_history": True,
            "additional_messages": [
                {
                    "role": "user",
                    "content": message,
                    "content_type": "text",
                }
            ],
        }
        create_result = await self._request(
            "POST",
            "v3/chat",
            create_payload,
            timeout=max_wait_time + 5,
        )

        create_code = create_result.get("code")
        if create_code not in (None, 0, "0"):
            msg = create_result.get("msg") or create_result.get("message") or "unknown error"
            raise CozeError(f"Coze v3 create failed with code={create_code}: {msg}")

        direct_text = self._extract_answer_text(create_result)
        if direct_text:
            return direct_text

        data = create_result.get("data") or {}
        if not isinstance(data, dict):
            raise CozeError("Coze v3 create returned unexpected data payload")

        conversation_id = data.get("conversation_id")
        chat_id = data.get("id") or data.get("chat_id")
        if not conversation_id or not chat_id:
            raise CozeError("Coze v3 response missing conversation_id/chat_id")

        elapsed = 0
        poll_interval = 1
        last_status = str(data.get("status") or "").lower()

        while elapsed < max_wait_time:
            retrieve_result = await self._request(
                "GET",
                "v3/chat/retrieve",
                {"conversation_id": conversation_id, "chat_id": chat_id},
                timeout=15,
            )
            retrieve_code = retrieve_result.get("code")
            if retrieve_code not in (None, 0, "0"):
                msg = retrieve_result.get("msg") or retrieve_result.get("message") or "unknown error"
                raise CozeError(f"Coze v3 retrieve failed with code={retrieve_code}: {msg}")

            retrieved_text = self._extract_answer_text(retrieve_result)
            if retrieved_text:
                return retrieved_text

            retrieve_data = retrieve_result.get("data") or {}
            if isinstance(retrieve_data, dict):
                last_status = str(retrieve_data.get("status") or "").lower()
                last_error = retrieve_data.get("last_error")
            else:
                last_error = None

            if last_status in {"completed", "failed", "cancelled"}:
                message_result = await self._request(
                    "GET",
                    "v3/chat/message/list",
                    {"conversation_id": conversation_id, "chat_id": chat_id},
                    timeout=15,
                )
                message_code = message_result.get("code")
                if message_code not in (None, 0, "0"):
                    msg = message_result.get("msg") or message_result.get("message") or "unknown error"
                    raise CozeError(f"Coze v3 message list failed with code={message_code}: {msg}")

                listed_text = self._extract_answer_text(message_result)
                if listed_text:
                    return listed_text

                if last_status in {"failed", "cancelled"}:
                    raise CozeError(f"Coze v3 chat {last_status}: {last_error}")
                break

            await asyncio.sleep(poll_interval)
            elapsed += poll_interval

        raise CozeError(f"Coze v3 chat did not return answer within {max_wait_time}s (status={last_status})")

    async def send_message_and_wait(self, message: str, user_id: str = "user", max_wait_time: int = 60) -> str:
        if not self.is_configured():
            raise CozeError("Coze is not configured.")

        payload = {
            "conversation_id": "",
            "bot_id": self.bot_id,
            "user": user_id,
            "query": message,
            "stream": False,
        }

        # Try configured endpoint first; fallback to common alternates.
        endpoints: List[str] = []
        for endpoint in [self.primary_chat_endpoint, "open_api/v2/chat", "v3/chat"]:
            normalized = endpoint.strip("/")
            if normalized and normalized not in endpoints:
                endpoints.append(normalized)

        last_error: Optional[Exception] = None
        for endpoint in endpoints:
            try:
                if endpoint.startswith("v3/chat"):
                    return await self._send_message_v3(message=message, user_id=user_id, max_wait_time=max_wait_time)

                result = await self._request("POST", endpoint, payload, timeout=max_wait_time + 5)
                code = result.get("code")
                if code not in (None, 0, "0"):
                    msg = result.get("msg") or result.get("message") or "unknown error"
                    raise CozeError(f"Coze returned code={code}: {msg}")

                text = self._extract_answer_text(result)
                if text:
                    return text

                raise CozeError(f"No assistant text found in Coze response for endpoint '{endpoint}'")
            except Exception as exc:
                last_error = exc
                logger.warning("Coze endpoint '%s' failed: %s", endpoint, exc)
                continue

        raise CozeError(f"All Coze endpoints failed. Last error: {last_error}")

    async def health_check(self) -> Dict[str, Any]:
        if not self.is_configured():
            return {
                "status": "not_configured",
                "message": "COZE_API_KEY or COZE_BOT_ID is missing.",
                "is_healthy": False,
            }

        try:
            response = await self.send_message_and_wait(
                message="Ping. Reply with one short sentence.",
                user_id="health_check",
                max_wait_time=15,
            )
            return {
                "status": "healthy",
                "message": "Coze is reachable.",
                "is_healthy": bool(response),
                "details": {"sample": response[:120]},
            }
        except Exception as exc:
            return {
                "status": "unhealthy",
                "message": str(exc),
                "is_healthy": False,
            }


coze_service = CozeService()

from __future__ import annotations


class CozeError(Exception):
    """Base error for upstream AI provider failures (legacy Coze* name retained for compatibility)."""


class CozeTimeoutError(CozeError):
    """Timeout error from upstream model provider."""


class CozeRequestError(CozeError):
    """Network/request-level upstream error."""


class CozeHttpStatusError(CozeError):
    """HTTP status error from upstream provider."""

    def __init__(self, status_code: int, detail: str):
        super().__init__(detail)
        self.status_code = int(status_code)


class CozeBudgetExceededError(CozeError):
    """Raised when budget guard blocks a request."""

    def __init__(self, message: str, *, request_started: bool = False) -> None:
        super().__init__(message)
        self.request_started = request_started


# Modern provider-accurate aliases
AIProviderError = CozeError
AIProviderTimeoutError = CozeTimeoutError
AIProviderRequestError = CozeRequestError
AIProviderHttpStatusError = CozeHttpStatusError
AIBudgetExceededError = CozeBudgetExceededError

__all__ = [
    "AIProviderError",
    "AIProviderTimeoutError",
    "AIProviderRequestError",
    "AIProviderHttpStatusError",
    "AIBudgetExceededError",
    "CozeError",
    "CozeTimeoutError",
    "CozeRequestError",
    "CozeHttpStatusError",
    "CozeBudgetExceededError",
]

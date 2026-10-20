from typing import Any

from pydantic import BaseModel, field_validator


def _check_utf8(value: Any) -> None:
    if isinstance(value, str):
        value.encode("utf-8")
    elif isinstance(value, BaseModel):
        for name in type(value).model_fields:
            _check_utf8(getattr(value, name))
    elif isinstance(value, list):
        for item in value:
            _check_utf8(item)


class UTF8InputModel(BaseModel):
    """Validate write inputs without tightening response or legacy models."""

    @field_validator("*")
    @classmethod
    def validate_text(cls, value: Any) -> Any:
        try:
            _check_utf8(value)
        except UnicodeEncodeError as exc:
            raise ValueError("Text must contain valid Unicode") from exc
        return value

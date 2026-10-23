from unicodedata import normalize

from pydantic import EmailStr, TypeAdapter


_EMAIL = TypeAdapter(EmailStr)


def email_identity_key(value: str) -> bytes:
    """Canonical identity v1; changes require migrating the stored keys."""
    email = _EMAIL.validate_python(value)
    return normalize("NFC", email.lower()).encode("utf-8")

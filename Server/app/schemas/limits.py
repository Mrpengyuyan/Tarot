from typing import Annotated

from pydantic import AfterValidator, Field


# SQLite binds integer parameters as signed 64-bit values.
MAX_DATABASE_INTEGER = 2**63 - 1
DatabaseInteger = Annotated[int, Field(ge=-(2**63), le=MAX_DATABASE_INTEGER)]

# SQLite limits LIKE patterns to 50,000 UTF-8 bytes; searches add two '%' bytes.
MAX_SEARCH_TERM_BYTES = 50_000 - 2


def _validate_search_term(value: str) -> str:
    if len(value.encode("utf-8")) > MAX_SEARCH_TERM_BYTES:
        raise ValueError(f"Search term must not exceed {MAX_SEARCH_TERM_BYTES} UTF-8 bytes")
    return value


SearchTerm = Annotated[str, AfterValidator(_validate_search_term)]

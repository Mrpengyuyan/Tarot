from typing import Annotated

from pydantic import Field


# SQLite binds integer parameters as signed 64-bit values.
MAX_DATABASE_INTEGER = 2**63 - 1
DatabaseInteger = Annotated[int, Field(ge=-(2**63), le=MAX_DATABASE_INTEGER)]

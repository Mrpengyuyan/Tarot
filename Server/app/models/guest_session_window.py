from sqlalchemy import Integer, String
from sqlalchemy.orm import Mapped, mapped_column

from app.db.base_class import Base


class GuestSessionWindow(Base):
    __tablename__ = "guest_session_windows"

    window_key: Mapped[str] = mapped_column(String(13), primary_key=True)
    source_hash: Mapped[str] = mapped_column(String(64), primary_key=True)
    issued: Mapped[int] = mapped_column(Integer, nullable=False, server_default="0")

"""Persist account type and daily reading quota usage.

Revision ID: 0003_reading_quota
Revises: 0002_interpretation_tracking
"""

from datetime import datetime, timezone
import re

from alembic import op
import sqlalchemy as sa


revision = "0003_reading_quota"
down_revision = "0002_interpretation_tracking"
branch_labels = None
depends_on = None


def upgrade() -> None:
    bind = op.get_bind()
    existing = {column["name"] for column in sa.inspect(bind).get_columns("users")}
    if "is_guest" not in existing:
        op.add_column("users", sa.Column("is_guest", sa.Boolean(), nullable=False, server_default=sa.false()))
    if "reading_quota_day" not in existing:
        op.add_column("users", sa.Column("reading_quota_day", sa.Date(), nullable=True))
    if "reading_quota_count" not in existing:
        op.add_column("users", sa.Column("reading_quota_count", sa.Integer(), nullable=False, server_default="0"))

    metadata = sa.MetaData()
    users = sa.Table("users", metadata, autoload_with=bind)
    predictions = sa.Table("predictions", metadata, autoload_with=bind)
    guest_name = re.compile(r"guest_[0-9a-f]{32}\Z")
    guest_email = re.compile(r"guest_[0-9a-f]{32}@guest\.tarot\.game\Z")
    for user_id, username, email in bind.execute(sa.select(users.c.id, users.c.username, users.c.email)):
        if guest_name.fullmatch(username) or guest_email.fullmatch(email):
            bind.execute(sa.update(users).where(users.c.id == user_id).values(is_guest=True))

    today = datetime.now(timezone.utc).date()
    day_start = datetime.combine(today, datetime.min.time(), tzinfo=timezone.utc)
    counts = bind.execute(
        sa.select(predictions.c.user_id, sa.func.count(predictions.c.id))
        .where(predictions.c.created_at >= day_start)
        .group_by(predictions.c.user_id)
    )
    for user_id, count in counts:
        bind.execute(
            sa.update(users)
            .where(users.c.id == user_id)
            .values(reading_quota_day=today, reading_quota_count=count)
        )


def downgrade() -> None:
    existing = {column["name"] for column in sa.inspect(op.get_bind()).get_columns("users")}
    with op.batch_alter_table("users") as batch_op:
        for column in ("reading_quota_count", "reading_quota_day", "is_guest"):
            if column in existing:
                batch_op.drop_column(column)

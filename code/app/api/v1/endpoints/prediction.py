"""Deprecated prediction endpoints.

This module is intentionally kept as a compatibility guard. The active flow is
implemented under `/api/v1/records/*`.
"""

from fastapi import APIRouter, HTTPException

router = APIRouter()


@router.api_route('/{path:path}', methods=['GET', 'POST', 'PUT', 'PATCH', 'DELETE'])
def deprecated_prediction_endpoint(path: str):
    raise HTTPException(
        status_code=410,
        detail='Deprecated endpoint. Use /api/v1/records/* endpoints instead.',
    )

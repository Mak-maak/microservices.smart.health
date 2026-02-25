from fastapi import APIRouter, Depends, Header
from typing import Optional
from .query import SuggestPrescriptionQuery
from .handler import SuggestPrescriptionHandler
from .schemas import SuggestPrescriptionRequest, SuggestPrescriptionResponse

router = APIRouter()


@router.post("", response_model=SuggestPrescriptionResponse)
async def suggest_prescription(
    request: SuggestPrescriptionRequest,
    x_correlation_id: Optional[str] = Header(default=None),
):
    handler = SuggestPrescriptionHandler()
    query = SuggestPrescriptionQuery(
        symptoms=request.symptoms,
        patient_history=request.patient_history,
        correlation_id=x_correlation_id,
    )
    return await handler(query)

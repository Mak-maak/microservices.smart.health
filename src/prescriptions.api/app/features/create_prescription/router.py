from fastapi import APIRouter, Depends, Header
from sqlalchemy.ext.asyncio import AsyncSession
from typing import Optional
from app.infrastructure.database import get_db
from app.mediator.mediator import mediator
from .command import CreatePrescriptionCommand
from .schemas import CreatePrescriptionRequest, PrescriptionResponse

router = APIRouter()


@router.post("", response_model=PrescriptionResponse, status_code=201)
async def create_prescription(
    request: CreatePrescriptionRequest,
    db: AsyncSession = Depends(get_db),
    x_correlation_id: Optional[str] = Header(default=None),
):
    from .handler import CreatePrescriptionHandler
    handler = CreatePrescriptionHandler(db)

    command = CreatePrescriptionCommand(
        appointment_id=request.appointment_id,
        patient_id=request.patient_id,
        doctor_id=request.doctor_id,
        symptoms=request.symptoms,
        diagnosis=request.diagnosis,
        medications=request.medications,
        dosage=request.dosage,
        notes=request.notes,
        correlation_id=x_correlation_id,
    )
    return await handler(command)

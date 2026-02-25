import uuid
from fastapi import APIRouter, Depends
from sqlalchemy.ext.asyncio import AsyncSession
from app.infrastructure.database import get_db
from .query import GetPrescriptionByIdQuery, GetPrescriptionsByPatientQuery
from .handler import GetPrescriptionByIdHandler, GetPrescriptionsByPatientHandler
from .schemas import PrescriptionListResponse
from app.features.create_prescription.schemas import PrescriptionResponse

router = APIRouter()


@router.get("/{prescription_id}", response_model=PrescriptionResponse)
async def get_prescription(
    prescription_id: uuid.UUID,
    db: AsyncSession = Depends(get_db),
):
    handler = GetPrescriptionByIdHandler(db)
    return await handler(GetPrescriptionByIdQuery(prescription_id=prescription_id))


@router.get("/patient/{patient_id}", response_model=PrescriptionListResponse)
async def get_prescriptions_by_patient(
    patient_id: uuid.UUID,
    db: AsyncSession = Depends(get_db),
):
    handler = GetPrescriptionsByPatientHandler(db)
    return await handler(GetPrescriptionsByPatientQuery(patient_id=patient_id))

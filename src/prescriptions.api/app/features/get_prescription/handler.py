import uuid
import structlog
from typing import Optional, List
from sqlalchemy.ext.asyncio import AsyncSession
from fastapi import HTTPException
from app.infrastructure.repositories import PrescriptionRepository
from app.features.create_prescription.schemas import PrescriptionResponse
from app.features.get_prescription.schemas import PrescriptionListResponse
from .query import GetPrescriptionByIdQuery, GetPrescriptionsByPatientQuery

logger = structlog.get_logger()


def _to_response(p) -> PrescriptionResponse:
    return PrescriptionResponse(
        id=p.id,
        appointment_id=p.appointment_id,
        patient_id=p.patient_id,
        doctor_id=p.doctor_id,
        symptoms=p.symptoms,
        diagnosis=p.diagnosis,
        medications=p.medications,
        dosage=p.dosage,
        notes=p.notes,
        created_at=p.created_at.isoformat(),
        updated_at=p.updated_at.isoformat(),
        version=p.version,
    )


class GetPrescriptionByIdHandler:
    def __init__(self, session: AsyncSession):
        self.repo = PrescriptionRepository(session)

    async def __call__(self, query: GetPrescriptionByIdQuery) -> PrescriptionResponse:
        logger.info("get_prescription.handler.by_id", prescription_id=str(query.prescription_id))
        prescription = await self.repo.get_by_id(query.prescription_id)
        if not prescription:
            raise HTTPException(status_code=404, detail="Prescription not found")
        return _to_response(prescription)


class GetPrescriptionsByPatientHandler:
    def __init__(self, session: AsyncSession):
        self.repo = PrescriptionRepository(session)

    async def __call__(self, query: GetPrescriptionsByPatientQuery) -> PrescriptionListResponse:
        logger.info("get_prescription.handler.by_patient", patient_id=str(query.patient_id))
        prescriptions = await self.repo.get_by_patient_id(query.patient_id)
        items = [_to_response(p) for p in prescriptions]
        return PrescriptionListResponse(items=items, total=len(items))

import uuid
from typing import Optional, List
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy import select
from app.domain.entities import Prescription, MLTrainingMetadata


class PrescriptionRepository:
    def __init__(self, session: AsyncSession):
        self.session = session

    async def create(self, prescription: Prescription) -> Prescription:
        self.session.add(prescription)
        await self.session.flush()
        await self.session.refresh(prescription)
        return prescription

    async def get_by_id(self, prescription_id: uuid.UUID) -> Optional[Prescription]:
        result = await self.session.execute(
            select(Prescription).where(Prescription.id == prescription_id)
        )
        return result.scalar_one_or_none()

    async def get_by_patient_id(self, patient_id: uuid.UUID) -> List[Prescription]:
        result = await self.session.execute(
            select(Prescription)
            .where(Prescription.patient_id == patient_id)
            .order_by(Prescription.created_at.desc())
        )
        return list(result.scalars().all())

    async def get_all(self) -> List[Prescription]:
        result = await self.session.execute(
            select(Prescription).order_by(Prescription.created_at.desc())
        )
        return list(result.scalars().all())


class MLTrainingMetadataRepository:
    def __init__(self, session: AsyncSession):
        self.session = session

    async def create(self, metadata: MLTrainingMetadata) -> MLTrainingMetadata:
        self.session.add(metadata)
        await self.session.flush()
        await self.session.refresh(metadata)
        return metadata

    async def get_latest(self) -> Optional[MLTrainingMetadata]:
        result = await self.session.execute(
            select(MLTrainingMetadata).order_by(MLTrainingMetadata.trained_at.desc()).limit(1)
        )
        return result.scalar_one_or_none()

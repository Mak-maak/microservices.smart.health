import uuid
import structlog
from sqlalchemy.ext.asyncio import AsyncSession
from app.domain.entities import Prescription
from app.domain.events import PrescriptionSavedEvent
from app.infrastructure.repositories import PrescriptionRepository
from app.messaging.publisher import publisher
from app.config.settings import settings
from .command import CreatePrescriptionCommand
from .schemas import PrescriptionResponse

logger = structlog.get_logger()


class CreatePrescriptionHandler:
    def __init__(self, session: AsyncSession):
        self.session = session
        self.repo = PrescriptionRepository(session)

    async def __call__(self, command: CreatePrescriptionCommand) -> PrescriptionResponse:
        logger.info(
            "create_prescription.handler.start",
            patient_id=str(command.patient_id),
            doctor_id=str(command.doctor_id),
        )

        prescription = Prescription(
            appointment_id=command.appointment_id,
            patient_id=command.patient_id,
            doctor_id=command.doctor_id,
            symptoms=command.symptoms,
            diagnosis=command.diagnosis,
            medications=command.medications,
            dosage=command.dosage,
            notes=command.notes,
        )

        saved = await self.repo.create(prescription)

        event = PrescriptionSavedEvent(
            aggregate_id=str(saved.id),
            correlation_id=command.correlation_id or str(uuid.uuid4()),
            payload={
                "prescription_id": str(saved.id),
                "patient_id": str(saved.patient_id),
                "doctor_id": str(saved.doctor_id),
                "appointment_id": str(saved.appointment_id),
                "diagnosis": saved.diagnosis,
            },
        )
        await publisher.publish(settings.prescription_saved_topic, event)

        logger.info("create_prescription.handler.complete", prescription_id=str(saved.id))

        return PrescriptionResponse(
            id=saved.id,
            appointment_id=saved.appointment_id,
            patient_id=saved.patient_id,
            doctor_id=saved.doctor_id,
            symptoms=saved.symptoms,
            diagnosis=saved.diagnosis,
            medications=saved.medications,
            dosage=saved.dosage,
            notes=saved.notes,
            created_at=saved.created_at.isoformat(),
            updated_at=saved.updated_at.isoformat(),
            version=saved.version,
        )

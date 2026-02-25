from dataclasses import dataclass
from typing import Optional, List, Any
import uuid


@dataclass
class CreatePrescriptionCommand:
    appointment_id: uuid.UUID
    patient_id: uuid.UUID
    doctor_id: uuid.UUID
    symptoms: List[str]
    diagnosis: str
    medications: List[str]
    dosage: dict
    notes: Optional[str]
    correlation_id: Optional[str] = None

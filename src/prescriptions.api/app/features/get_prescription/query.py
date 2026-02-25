from dataclasses import dataclass
from typing import Optional
import uuid


@dataclass
class GetPrescriptionByIdQuery:
    prescription_id: uuid.UUID


@dataclass
class GetPrescriptionsByPatientQuery:
    patient_id: uuid.UUID

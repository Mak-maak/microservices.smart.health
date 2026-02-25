from pydantic import BaseModel, Field
from typing import Optional, List, Any
import uuid


class CreatePrescriptionRequest(BaseModel):
    appointment_id: uuid.UUID
    patient_id: uuid.UUID
    doctor_id: uuid.UUID
    symptoms: List[str] = Field(default_factory=list)
    diagnosis: str = Field(min_length=1, max_length=500)
    medications: List[str] = Field(default_factory=list)
    dosage: dict[str, Any] = Field(default_factory=dict)
    notes: Optional[str] = None


class PrescriptionResponse(BaseModel):
    id: uuid.UUID
    appointment_id: uuid.UUID
    patient_id: uuid.UUID
    doctor_id: uuid.UUID
    symptoms: List[str]
    diagnosis: str
    medications: List[str]
    dosage: dict[str, Any]
    notes: Optional[str]
    created_at: str
    updated_at: str
    version: int

    model_config = {"from_attributes": True}

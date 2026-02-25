from pydantic import BaseModel, Field
from typing import Optional, List, Any


class SuggestPrescriptionRequest(BaseModel):
    symptoms: List[str] = Field(min_length=1)
    patient_history: Optional[str] = None


class SuggestedMedication(BaseModel):
    name: str
    dosage: str
    frequency: str
    duration: str


class SuggestPrescriptionResponse(BaseModel):
    diagnosis: str
    medications: List[SuggestedMedication]
    notes: str
    confidence: float = Field(default=0.0, ge=0.0, le=1.0)
    source: str = Field(default="llm")

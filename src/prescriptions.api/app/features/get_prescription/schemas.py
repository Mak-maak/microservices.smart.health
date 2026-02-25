from app.features.create_prescription.schemas import PrescriptionResponse
from typing import List

__all__ = ["PrescriptionResponse", "PrescriptionListResponse"]

from pydantic import BaseModel


class PrescriptionListResponse(BaseModel):
    items: List[PrescriptionResponse]
    total: int

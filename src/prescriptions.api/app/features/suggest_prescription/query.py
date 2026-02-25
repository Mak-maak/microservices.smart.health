from dataclasses import dataclass
from typing import Optional, List


@dataclass
class SuggestPrescriptionQuery:
    symptoms: List[str]
    patient_history: Optional[str] = None
    correlation_id: Optional[str] = None

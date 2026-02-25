import uuid
from datetime import datetime, timezone
from pydantic import BaseModel, Field
from typing import Any


class BaseEvent(BaseModel):
    event_id: str = Field(default_factory=lambda: str(uuid.uuid4()))
    correlation_id: str = Field(default_factory=lambda: str(uuid.uuid4()))
    aggregate_id: str
    occurred_at: str = Field(
        default_factory=lambda: datetime.now(timezone.utc).isoformat()
    )
    source_service: str = "prescriptions.api"
    payload: dict[str, Any] = Field(default_factory=dict)


class PrescriptionSavedEvent(BaseEvent):
    event_type: str = "PrescriptionSavedEvent"


class PrescriptionSuggestedEvent(BaseEvent):
    event_type: str = "PrescriptionSuggestedEvent"


class AppointmentConfirmedEvent(BaseEvent):
    event_type: str = "AppointmentConfirmedEvent"


class PaymentCompletedEvent(BaseEvent):
    event_type: str = "PaymentCompletedEvent"

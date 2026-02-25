from pydantic import BaseModel
from typing import Optional
import uuid


class TrainModelRequest(BaseModel):
    force_retrain: bool = False


class TrainModelResponse(BaseModel):
    model_version: str
    accuracy_score: float
    dataset_size: int
    trained_at: str
    message: str

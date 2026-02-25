from fastapi import APIRouter, Depends
from sqlalchemy.ext.asyncio import AsyncSession
from app.infrastructure.database import get_db
from .command import TrainModelCommand
from .handler import TrainModelHandler
from .schemas import TrainModelRequest, TrainModelResponse

router = APIRouter()


@router.post("", response_model=TrainModelResponse)
async def train_model(
    request: TrainModelRequest,
    db: AsyncSession = Depends(get_db),
):
    handler = TrainModelHandler(db)
    command = TrainModelCommand(force_retrain=request.force_retrain)
    return await handler(command)

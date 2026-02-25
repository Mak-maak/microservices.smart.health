import uuid
import os
import asyncio
import structlog
from datetime import datetime, timezone
from sqlalchemy.ext.asyncio import AsyncSession
from app.domain.entities import MLTrainingMetadata
from app.infrastructure.repositories import PrescriptionRepository, MLTrainingMetadataRepository
from app.config.settings import settings
from .command import TrainModelCommand
from .schemas import TrainModelResponse

logger = structlog.get_logger()


class TrainModelHandler:
    def __init__(self, session: AsyncSession):
        self.session = session
        self.repo = PrescriptionRepository(session)
        self.metadata_repo = MLTrainingMetadataRepository(session)

    async def __call__(self, command: TrainModelCommand) -> TrainModelResponse:
        logger.info("train_model.handler.start", force_retrain=command.force_retrain)

        prescriptions = await self.repo.get_all()
        if not prescriptions:
            logger.warning("train_model.handler.no_data")
            return TrainModelResponse(
                model_version="0.0.0",
                accuracy_score=0.0,
                dataset_size=0,
                trained_at=datetime.now(timezone.utc).isoformat(),
                message="No training data available",
            )

        loop = asyncio.get_event_loop()
        result = await loop.run_in_executor(
            None,
            self._train_sync,
            prescriptions,
        )

        model_version = f"1.{len(prescriptions)}.0"
        metadata = MLTrainingMetadata(
            model_version=model_version,
            accuracy_score=result["accuracy"],
            dataset_size=result["dataset_size"],
        )
        await self.metadata_repo.create(metadata)

        logger.info(
            "train_model.handler.complete",
            model_version=model_version,
            accuracy=result["accuracy"],
            dataset_size=result["dataset_size"],
        )

        return TrainModelResponse(
            model_version=model_version,
            accuracy_score=result["accuracy"],
            dataset_size=result["dataset_size"],
            trained_at=datetime.now(timezone.utc).isoformat(),
            message=f"Model trained successfully on {result['dataset_size']} records",
        )

    def _train_sync(self, prescriptions) -> dict:
        import numpy as np
        from sklearn.feature_extraction.text import TfidfVectorizer
        from sklearn.linear_model import LogisticRegression
        from sklearn.model_selection import train_test_split
        from sklearn.metrics import accuracy_score
        import joblib

        # Build training data
        X_raw = []
        y_raw = []
        for p in prescriptions:
            symptom_text = " ".join(p.symptoms) if isinstance(p.symptoms, list) else str(p.symptoms)
            X_raw.append(symptom_text)
            y_raw.append(p.diagnosis)

        if len(set(y_raw)) < 2:
            # Not enough classes for proper split, train on all data
            vectorizer = TfidfVectorizer(max_features=500)
            X = vectorizer.fit_transform(X_raw)
            clf = LogisticRegression(max_iter=1000)
            clf.fit(X, y_raw)
            accuracy = 1.0
        else:
            vectorizer = TfidfVectorizer(max_features=500)
            X = vectorizer.fit_transform(X_raw)
            test_size = min(0.2, max(1 / len(y_raw), 0.1))
            X_train, X_test, y_train, y_test = train_test_split(
                X, y_raw, test_size=test_size, random_state=42
            )
            clf = LogisticRegression(max_iter=1000)
            clf.fit(X_train, y_train)
            y_pred = clf.predict(X_test)
            accuracy = accuracy_score(y_test, y_pred)

        # Save model
        os.makedirs(settings.model_storage_path, exist_ok=True)
        model_path = os.path.join(settings.model_storage_path, "prescription_model.pkl")
        joblib.dump({"vectorizer": vectorizer, "classifier": clf}, model_path)
        logger.info("train_model.handler.model_saved", path=model_path)

        return {"accuracy": float(accuracy), "dataset_size": len(prescriptions)}

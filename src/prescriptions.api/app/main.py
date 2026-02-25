import structlog
from contextlib import asynccontextmanager
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware

from app.config.settings import settings
from app.infrastructure.database import create_tables
from app.messaging.publisher import publisher
from app.messaging.subscriber import subscriber
from app.features.create_prescription.router import router as create_router
from app.features.get_prescription.router import router as get_router
from app.features.suggest_prescription.router import router as suggest_router
from app.features.train_model.router import router as train_router

structlog.configure(
    processors=[
        structlog.processors.TimeStamper(fmt="iso"),
        structlog.stdlib.add_log_level,
        structlog.processors.JSONRenderer(),
    ]
)

logger = structlog.get_logger()


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("prescriptions_api.startup", environment=settings.environment)
    await create_tables()
    await publisher.start()
    await subscriber.start(handlers={})
    logger.info("prescriptions_api.ready")
    yield
    logger.info("prescriptions_api.shutdown")
    await publisher.stop()
    await subscriber.stop()


app = FastAPI(
    title=settings.app_name,
    version=settings.app_version,
    description="Production-grade Prescription Microservice with ML and LLM integration",
    lifespan=lifespan,
    docs_url="/docs",
    redoc_url="/redoc",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    logger.error("unhandled_exception", path=str(request.url), error=str(exc))
    return JSONResponse(
        status_code=500,
        content={"detail": "Internal server error"},
    )


app.include_router(create_router, prefix="/api/prescriptions", tags=["Prescriptions"])
app.include_router(get_router, prefix="/api/prescriptions", tags=["Prescriptions"])
app.include_router(suggest_router, prefix="/api/prescriptions/suggest", tags=["Suggestions"])
app.include_router(train_router, prefix="/api/prescriptions/train", tags=["ML Training"])


@app.get("/health", tags=["Health"])
async def health():
    return {
        "status": "healthy",
        "service": settings.app_name,
        "version": settings.app_version,
        "environment": settings.environment,
    }

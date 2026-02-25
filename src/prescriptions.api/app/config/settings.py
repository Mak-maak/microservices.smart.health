from pydantic_settings import BaseSettings, SettingsConfigDict
from pydantic import Field
from typing import Optional


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
    )

    # App
    app_name: str = "Prescriptions API"
    app_version: str = "1.0.0"
    debug: bool = False
    environment: str = "production"

    # Database
    database_url: str = Field(
        default="postgresql+asyncpg://postgres:postgres@localhost:5432/smarthealth_prescriptions"
    )

    # Azure Service Bus
    azure_service_bus_connection_string: str = Field(default="")
    prescription_saved_topic: str = "prescription-saved"
    prescription_suggested_topic: str = "prescription-suggested"
    appointment_confirmed_subscription: str = "prescriptions-appointment-confirmed"
    payment_completed_subscription: str = "prescriptions-payment-completed"
    appointment_confirmed_topic: str = "appointment-confirmed"
    payment_completed_topic: str = "payment-completed"

    # Redis
    redis_url: str = Field(default="redis://localhost:6379")
    cache_ttl_seconds: int = 120

    # OpenAI / Azure OpenAI
    openai_api_key: str = Field(default="")
    openai_model: str = "gpt-4o-mini"
    openai_timeout: float = 30.0
    use_azure_openai: bool = False
    azure_openai_endpoint: str = Field(default="")
    azure_openai_api_version: str = "2024-02-01"
    azure_openai_deployment: str = Field(default="gpt-4o-mini")

    # ML
    model_storage_path: str = "./models"

    # Security
    secret_key: str = Field(default="change-me-in-production")
    algorithm: str = "HS256"
    access_token_expire_minutes: int = 30

    # Feature flags
    use_in_memory_bus: bool = True
    enable_redis_cache: bool = False
    enable_llm_suggestions: bool = True


settings = Settings()

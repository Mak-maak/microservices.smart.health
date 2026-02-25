import json
import structlog
from typing import Optional
from app.config.settings import settings
from app.domain.events import PrescriptionSuggestedEvent
from app.messaging.publisher import publisher
from .query import SuggestPrescriptionQuery
from .schemas import SuggestPrescriptionResponse, SuggestedMedication

logger = structlog.get_logger()

_FALLBACK_RESPONSE = SuggestPrescriptionResponse(
    diagnosis="Unable to determine diagnosis - please consult a physician",
    medications=[],
    notes="LLM service unavailable. Please consult a qualified medical professional.",
    confidence=0.0,
    source="fallback",
)

SYSTEM_PROMPT = """You are a medical AI assistant. Given symptoms, provide a structured prescription suggestion.
Always respond with valid JSON in this exact format:
{
  "diagnosis": "string",
  "medications": [{"name": "string", "dosage": "string", "frequency": "string", "duration": "string"}],
  "notes": "string",
  "confidence": 0.0
}
Important: This is for informational purposes only and must be reviewed by a licensed physician."""


class SuggestPrescriptionHandler:
    async def __call__(self, query: SuggestPrescriptionQuery) -> SuggestPrescriptionResponse:
        logger.info(
            "suggest_prescription.handler.start",
            symptoms=query.symptoms,
        )

        if not settings.enable_llm_suggestions or not settings.openai_api_key:
            logger.warning("suggest_prescription.handler.llm_disabled")
            return _FALLBACK_RESPONSE

        try:
            result = await self._call_llm(query)
            await self._publish_event(query, result)
            return result
        except Exception as e:
            logger.error("suggest_prescription.handler.llm_failed", error=str(e))
            return _FALLBACK_RESPONSE

    async def _call_llm(self, query: SuggestPrescriptionQuery) -> SuggestPrescriptionResponse:
        import openai
        from openai import AsyncOpenAI

        if settings.use_azure_openai:
            client = openai.AsyncAzureOpenAI(
                azure_endpoint=settings.azure_openai_endpoint,
                api_version=settings.azure_openai_api_version,
                api_key=settings.openai_api_key,
            )
            model = settings.azure_openai_deployment
        else:
            client = AsyncOpenAI(api_key=settings.openai_api_key)
            model = settings.openai_model

        user_content = f"Symptoms: {', '.join(query.symptoms)}"
        if query.patient_history:
            user_content += f"\nPatient history: {query.patient_history}"

        response = await client.chat.completions.create(
            model=model,
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": user_content},
            ],
            temperature=0.2,
            timeout=settings.openai_timeout,
            response_format={"type": "json_object"},
        )

        content = response.choices[0].message.content
        data = json.loads(content)

        medications = [SuggestedMedication(**m) for m in data.get("medications", [])]
        return SuggestPrescriptionResponse(
            diagnosis=data.get("diagnosis", ""),
            medications=medications,
            notes=data.get("notes", ""),
            confidence=float(data.get("confidence", 0.5)),
            source="llm",
        )

    async def _publish_event(self, query: SuggestPrescriptionQuery, result: SuggestPrescriptionResponse):
        import uuid
        event = PrescriptionSuggestedEvent(
            aggregate_id=str(uuid.uuid4()),
            correlation_id=query.correlation_id or str(uuid.uuid4()),
            payload={
                "symptoms": query.symptoms,
                "diagnosis": result.diagnosis,
                "confidence": result.confidence,
            },
        )
        await publisher.publish(settings.prescription_suggested_topic, event)

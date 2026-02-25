import json
import structlog
from typing import Optional
from app.config.settings import settings
from app.domain.events import BaseEvent

logger = structlog.get_logger()


class ServiceBusPublisher:
    def __init__(self):
        self._client = None
        self._use_in_memory = settings.use_in_memory_bus

    async def start(self):
        if self._use_in_memory:
            logger.info("messaging.publisher.using_in_memory_bus")
            return
        try:
            from azure.servicebus.aio import ServiceBusClient
            self._client = ServiceBusClient.from_connection_string(
                settings.azure_service_bus_connection_string
            )
            logger.info("messaging.publisher.connected_to_azure_service_bus")
        except Exception as e:
            logger.error("messaging.publisher.connection_failed", error=str(e))
            self._use_in_memory = True

    async def stop(self):
        if self._client:
            await self._client.close()

    async def publish(self, topic: str, event: BaseEvent):
        payload = event.model_dump_json()
        if self._use_in_memory:
            logger.info(
                "messaging.publisher.in_memory_publish",
                topic=topic,
                event_type=event.event_type if hasattr(event, 'event_type') else type(event).__name__,
                event_id=event.event_id,
                aggregate_id=event.aggregate_id,
            )
            return
        try:
            from azure.servicebus import ServiceBusMessage
            async with self._client.get_topic_sender(topic_name=topic) as sender:
                message = ServiceBusMessage(
                    body=payload,
                    content_type="application/json",
                    subject=event.event_type if hasattr(event, 'event_type') else type(event).__name__,
                    message_id=event.event_id,
                    correlation_id=event.correlation_id,
                )
                await sender.send_messages(message)
                logger.info(
                    "messaging.publisher.published",
                    topic=topic,
                    event_id=event.event_id,
                )
        except Exception as e:
            logger.error("messaging.publisher.publish_failed", topic=topic, error=str(e))
            raise


publisher = ServiceBusPublisher()

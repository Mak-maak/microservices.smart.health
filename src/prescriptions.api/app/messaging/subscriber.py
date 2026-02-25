import json
import asyncio
import structlog
from app.config.settings import settings

logger = structlog.get_logger()


class ServiceBusSubscriber:
    def __init__(self):
        self._client = None
        self._use_in_memory = settings.use_in_memory_bus
        self._running = False

    async def start(self, handlers: dict):
        if self._use_in_memory:
            logger.info("messaging.subscriber.using_in_memory_bus_skip")
            return
        try:
            from azure.servicebus.aio import ServiceBusClient
            self._client = ServiceBusClient.from_connection_string(
                settings.azure_service_bus_connection_string
            )
            self._running = True
            for topic, (subscription, handler) in handlers.items():
                asyncio.create_task(
                    self._consume(topic, subscription, handler)
                )
            logger.info("messaging.subscriber.started", topics=list(handlers.keys()))
        except Exception as e:
            logger.error("messaging.subscriber.start_failed", error=str(e))

    async def stop(self):
        self._running = False
        if self._client:
            await self._client.close()

    async def _consume(self, topic: str, subscription: str, handler):
        async with self._client.get_subscription_receiver(
            topic_name=topic,
            subscription_name=subscription,
        ) as receiver:
            while self._running:
                try:
                    messages = await receiver.receive_messages(max_message_count=10, max_wait_time=5)
                    for message in messages:
                        try:
                            body = json.loads(b"".join(message.body))
                            await handler(body)
                            await receiver.complete_message(message)
                        except Exception as e:
                            logger.error(
                                "messaging.subscriber.message_processing_failed",
                                topic=topic,
                                error=str(e),
                            )
                            await receiver.dead_letter_message(
                                message,
                                reason="ProcessingFailed",
                                error_description=str(e),
                            )
                except Exception as e:
                    logger.error("messaging.subscriber.receive_failed", topic=topic, error=str(e))
                    await asyncio.sleep(5)


subscriber = ServiceBusSubscriber()

from typing import Any, Callable, Dict, Type
import structlog

logger = structlog.get_logger()


class Mediator:
    def __init__(self):
        self._handlers: Dict[Type, Callable] = {}

    def register(self, request_type: Type, handler: Callable):
        self._handlers[request_type] = handler
        logger.debug("mediator.handler_registered", request_type=request_type.__name__)

    async def send(self, request: Any) -> Any:
        request_type = type(request)
        handler = self._handlers.get(request_type)
        if handler is None:
            raise ValueError(f"No handler registered for {request_type.__name__}")
        logger.info("mediator.dispatching", request_type=request_type.__name__)
        return await handler(request)


mediator = Mediator()

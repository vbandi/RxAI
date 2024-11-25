import asyncio
from rtclient import RTInputAudioItem, RTResponse
from rx.subject import Subject
from rx import operators as ops


class ConversationClient:
    """Manages the conversation using RTClient and RxPY."""

    def __init__(self, client):
        self.client = client
        self.input_transcription_updates = Subject()
        self.output_transcription_updates = Subject()
        self.output_transcription_delta_updates = Subject()
        self.function_call_started = Subject()
        self.function_call_finished = Subject()
        self.error_messages = Subject()
        self.audio_delta_updates = Subject()
        self.speech_started = Subject()
        self.stop_event = asyncio.Event()

    async def initialize_session(self, options, function_definitions):
        await self.client.configure(
            turn_detection=options['turn_detection'],
            input_audio_transcription=options['input_audio_transcription'],
            tools=function_definitions,
        )

    async def start(self):
        try:
            async for event in self.client.events():
                if isinstance(event, RTInputAudioItem):
                    self.speech_started.on_next(None)
                    await event
                    if event.transcript is not None:
                        self.input_transcription_updates.on_next(event)
                elif isinstance(event, RTResponse):
                    await self._handle_response(event)
        except Exception as e:
            self.error_messages.on_next(str(e))
            self.stop_event.set()
        finally:
            self.stop_event.set()

    async def _handle_response(self, response):
        async for item in response:
            if item.type == "message":
                await self._handle_message_item(item)
            elif item.type == "function_call":
                self.function_call_started.on_next(item)
                await item
                self.function_call_finished.on_next(item)

                if item.function_name == "user_wants_to_finish_conversation":
                    self.stop_event.set()
            else:
                pass

    async def _handle_message_item(self, item):
        async for content_part in item:
            if content_part.type == "audio":
                async for audio_chunk in content_part.audio_chunks():
                    self.audio_delta_updates.on_next(audio_chunk)

                transcript = ""
                async for transcript_chunk in content_part.transcript_chunks():
                    transcript += transcript_chunk
                self.output_transcription_updates.on_next(transcript)
            elif content_part.type == "text":
                text_data = ""
                async for text_chunk in content_part.text_chunks():
                    text_data += text_chunk
                    self.output_transcription_delta_updates.on_next(text_chunk)
                self.output_transcription_updates.on_next(text_data)

    def subscribe_events(self, scheduler):
        self.input_transcription_updates.pipe(
            ops.observe_on(scheduler)
        ).subscribe(lambda t: print(f"User: {t.transcript.strip('\n')}"))

        self.output_transcription_delta_updates.pipe(
            ops.observe_on(scheduler)
        ).subscribe(lambda t: print(f"{t}", end='', flush=True))

        self.output_transcription_updates.pipe(
            ops.observe_on(scheduler)
        ).subscribe(lambda t: print(f"Assistant: {t}\n"))

        self.function_call_started.pipe(
            ops.observe_on(scheduler)
        ).subscribe(lambda f: print(f"Function call started: {f.function_name}({f.arguments})"))

        self.function_call_finished.pipe(
            ops.observe_on(scheduler)
        ).subscribe(
            lambda f: print(
                f"Function call finished: {getattr(f, 'result', None) if getattr(f, 'result', None) is not None else f.function_name}"
            )
)

        self.error_messages.pipe(
            ops.observe_on(scheduler)
        ).subscribe(lambda msg: print(f"Error: {msg}"))

    def subscribe_audio(self, speaker_output):
        self.audio_delta_updates.subscribe(
            lambda audio_chunk: speaker_output.enqueue_for_playback(audio_chunk)
        )

        self.speech_started.subscribe(
            lambda _: speaker_output.clear_playback()
        )
import asyncio
import os
from dotenv import load_dotenv
from rtclient import RTClient, ServerVAD, InputAudioTranscription
from conversation_client import ConversationClient
from microphone_stream import MicrophoneAudioStream
from speaker_output import SpeakerOutput
from azure.core.credentials import AzureKeyCredential
from rx.scheduler.eventloop import AsyncIOScheduler

load_dotenv()

async def main():
    key = os.environ.get("OPENAI_API_KEY")
    if not key:
        raise ValueError("Please set the OPENAI_API_KEY environment variable in your .env file.")

    model = os.environ.get("OPENAI_MODEL")
    async with RTClient(key_credential=AzureKeyCredential(key), model=model) as client:
        print("Configuring Session...")
        await client.configure(instructions="You are a helpful and friendly AI assistant.")        
        conversation_client = ConversationClient(client)

        print(" >>> Listening to microphone input")
        print(" >>> (Say 'stop' or 'goodbye' to end the conversation)\n")

        finish_conversation_tool = {
            "type": "function",
            "name": "user_wants_to_finish_conversation",
            "description": "Invoked when the user says 'goodbye' explicitly.",
            "parameters": {
                "type": "object",
                "properties": {},
                "required": []
            }
        }

        options = {
            'turn_detection': ServerVAD(threshold=0.5, prefix_padding_ms=300, silence_duration_ms=200),
            'input_audio_transcription': InputAudioTranscription(model="whisper-1"),
        }
        await conversation_client.initialize_session(options, [finish_conversation_tool])

        loop = asyncio.get_event_loop()
        asyncio_scheduler = AsyncIOScheduler(loop)

        conversation_client.subscribe_events(asyncio_scheduler)

        speaker_output = SpeakerOutput()
        conversation_client.subscribe_audio(speaker_output)

        microphone_stream = MicrophoneAudioStream()
        microphone_stream.subscribe_audio(client)

        microphone_stream.start()
        asyncio.create_task(conversation_client.start())

        await conversation_client.stop_event.wait()

        microphone_stream.stop()
        speaker_output.dispose()
        await client.close()
        print("Conversation ended.")

if __name__ == "__main__":
    asyncio.run(main())
import sounddevice as sd
from rx.subject import Subject
import asyncio


class MicrophoneAudioStream:
    def __init__(self, sample_rate=24000, channels=1, dtype='int16', blocksize=0, loop=None):
        self.sample_rate = sample_rate
        self.channels = channels
        self.dtype = dtype
        self.blocksize = blocksize
        self.audio_subject = Subject()
        self.loop = loop or asyncio.get_event_loop()  # Use the provided loop or the current one
        self.stream = sd.InputStream(
            samplerate=self.sample_rate,
            channels=self.channels,
            dtype=self.dtype,
            blocksize=self.blocksize,
            callback=self._audio_callback
        )

    def _audio_callback(self, indata, frames, time, status):
        if status:
            print(f"Status: {status}")
        # Pass the audio data to the RxPY Subject
        self.audio_subject.on_next(indata.copy())

    def start(self):
        self.stream.start()

    def stop(self):
        self.stream.stop()
        self.audio_subject.on_completed()

    def subscribe_audio(self, client):
        self.audio_subject.subscribe(
            lambda audio_data: asyncio.run_coroutine_threadsafe(
                client.send_audio(audio_data.tobytes()), self.loop
            )
        )
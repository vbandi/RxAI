import sounddevice as sd
import numpy as np


class SpeakerOutput:
    def __init__(self, sample_rate=24000, channels=1, dtype='int16'):
        self.sample_rate = sample_rate
        self.channels = channels
        self.dtype = dtype
        self.buffer = np.array([], dtype=self.dtype)
        self.is_playing = False
        self.output_stream = sd.OutputStream(
            samplerate=self.sample_rate,
            channels=self.channels,
            dtype=self.dtype,
            callback=self._audio_callback
        )

    def _audio_callback(self, outdata, frames, time, status):
        if status:
            print(status)
        needed_frames = frames
        available_frames = len(self.buffer) // self.channels
        if available_frames >= needed_frames:
            outdata[:] = self.buffer[:needed_frames * self.channels].reshape(needed_frames, self.channels)
            self.buffer = self.buffer[needed_frames * self.channels:]
        else:
            if available_frames > 0:
                outdata[:available_frames] = self.buffer.reshape(available_frames, self.channels)
                self.buffer = np.array([], dtype=self.dtype)
            outdata[available_frames:] = 0
            if not self.buffer.size:
                self.output_stream.stop()
                self.is_playing = False

    def enqueue_for_playback(self, audio_data: bytes):
        audio_array = np.frombuffer(audio_data, dtype=self.dtype)
        self.buffer = np.concatenate((self.buffer, audio_array))
        if not self.is_playing:
            self.output_stream.start()
            self.is_playing = True

    def clear_playback(self):
        self.buffer = np.array([], dtype=self.dtype)
        if self.is_playing:
            self.output_stream.stop()
            self.is_playing = False

    def dispose(self):
        if self.is_playing:
            self.output_stream.stop()
            self.is_playing = False
        self.output_stream.close()

// From https://github.com/Azure-Samples/aoai-realtime-audio-sdk/blob/6779885d3aaa2ddbed2bbc5dbba74da8cddffca1/dotnet/samples/console-from-mic/MicrophoneAudioStream.cs

using NAudio.Wave;

/// <summary>
/// Uses the NAudio library (https://github.com/naudio/NAudio) to provide a rudimentary abstraction to output
/// BinaryData audio segments to the default output (speaker/headphone) device.
/// </summary>
public class SpeakerOutput : IDisposable
{
    private readonly BufferedWaveProvider _waveProvider;
    private readonly WaveOutEvent _waveOutEvent;

    public SpeakerOutput()
    {
        WaveFormat outputAudioFormat = new(
            rate: 24000,
            bits: 16,
            channels: 1);
        _waveProvider = new(outputAudioFormat)
        {
            BufferDuration = TimeSpan.FromMinutes(2),
        };
        _waveOutEvent = new();
        _waveOutEvent.Init(_waveProvider);
        _waveOutEvent.Play();
    }

    public void EnqueueForPlayback(BinaryData audioData)
    {
        byte[] buffer = audioData.ToArray();
        _waveProvider.AddSamples(buffer, 0, buffer.Length);
    }

    public void ClearPlaybackIf(IObservable<bool> observable)
    {
        observable.Subscribe(clear =>
        {
            if (clear)
            {
                _waveOutEvent.Stop();
                _waveProvider.ClearBuffer();
            }
            else
            {
                _waveOutEvent.Play();
            }
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _waveProvider.ClearBuffer();
            _waveOutEvent.Dispose();
        }
    }
}
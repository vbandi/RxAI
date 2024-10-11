#pragma warning disable OPENAI002

using System.Text;
using OpenAI.RealtimeConversation;
using RxAI.Realtime.FunctionCalling;
using RxAI.Realtime;

Console.OutputEncoding = Encoding.UTF8;

string? openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(openAIKey))
{
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
    return;
}

RealtimeConversationClientRX conversation = RealtimeConversationClientRX.FromOpenAIKey(openAIKey);

ConversationSessionOptions options = new()
{
    //ContentModalities = ConversationContentModalities.Text,
    Instructions = "You are an annoyingly rude assistant. Use lots of sarcasm and emojis.",
    InputTranscriptionOptions = new() { Model = ConversationTranscriptionModel.Whisper1 },
};

// Initialize the conversation
var calculator = new Calculator();
await conversation.InitializeSessionAsync(options, FunctionCallingHelper.GetFunctionDefinitions(calculator));

// Transcription updates
conversation.InputTranscriptionFinishedUpdates.Subscribe(t => Console.WriteLine(t.Transcript));
conversation.OutputTranscriptionFinishedUpdates.Subscribe(u => Console.WriteLine());
conversation.OutputTranscriptionDeltaUpdates.Subscribe(u => Console.Write(u.Delta));

// Function updates
conversation.FunctionCallStarted.Subscribe(f => Console.WriteLine($"Function call: {f.Name}({f.Arguments})"));
conversation.FunctionCallEnded.Subscribe(f => Console.WriteLine($"Function call finished: {f.result}"));

// Cost updates
conversation.SetupCost(5f / 1_000_000, 20f / 1_000_000, 100f / 1_000_000, 200f / 1_000_000);
conversation.TotalCost.Subscribe(c => Console.WriteLine($"Total cost: {c}"));

// Setup speaker output
SpeakerOutput speakerOutput = new();
conversation.AudioDeltaUpdates.Subscribe(d => speakerOutput.EnqueueForPlayback(d.Delta));

// Setup microphone input
MicrophoneAudioStream microphone = MicrophoneAudioStream.Start();
await conversation.SendAudioAsync(microphone);

await conversation.StartResponseTurnAsync();

Console.WriteLine("Starting conversation...");

while (true)
{
    await Task.Delay(10);
}

public class Calculator
{
    [FunctionDescription("Get a random number within a specified range")]
    public static int GetRandomNumber(
        [ParameterDescription("min")] int min,
        [ParameterDescription("max")] int max)
    {
        return new Random().Next(min, max);
    }

    [FunctionDescription("Add two numbers together")]
    public static int Add(
        [ParameterDescription("The first number to add")] int a,
        [ParameterDescription("The second number to add")] int b)
    {
        return a + b;
    }
}

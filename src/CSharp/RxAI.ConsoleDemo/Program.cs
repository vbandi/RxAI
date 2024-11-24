#pragma warning disable OPENAI002

using System.Text;
using OpenAI.RealtimeConversation;
using RxAI.Realtime.FunctionCalling;
using RxAI.Realtime;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

// OpenAI
//var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
//
//if (openAIKey is null)
//    throw new InvalidOperationException("OPENAI_API_KEY environment variable not set.");
//
//var conversation = RealtimeConversationClientRX.FromOpenAIKey(openAIKey);

// Azure OpenAI 
string? aoaiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_ENDPOINT");
string? aoaiDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_DEPLOYMENT");
string? aoaiApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

if (aoaiEndpoint is null || aoaiDeployment is null || aoaiApiKey is null)
    throw new InvalidOperationException("AZURE_OPENAI_API_ENDPOINT, AZURE_OPENAI_API_DEPLOYMENT, and AZURE_OPENAI_API_KEY environment variables must be set.");

var conversation = RealtimeConversationClientRX.FromAzureCredential(aoaiEndpoint, aoaiDeployment, aoaiApiKey);

ConversationSessionOptions options = new()
{
    //ContentModalities = ConversationContentModalities.Text,
    Instructions = "You are an annoyingly rude assistant. Use lots of sarcasm and emojis.",
    InputTranscriptionOptions = new() { Model = ConversationTranscriptionModel.Whisper1 },
};

// Use type for static functions, or instance for both static and instance functions
var functionDefinitions = FunctionCallingHelper.GetFunctionDefinitions(typeof(Calculator));

// Initialize the conversation
await conversation.InitializeSessionAsync(options, functionDefinitions);

// Transcription updates
conversation.InputTranscriptionFinishedUpdates.Subscribe(t => AnsiConsole.MarkupLine($"[yellow]{t.Transcript}[/]"));
conversation.OutputTranscriptionFinishedUpdates.Subscribe(u => AnsiConsole.WriteLine());
conversation.OutputTranscriptionDeltaUpdates.Subscribe(u => AnsiConsole.Markup($"[white]{u.Delta}[/]"));

// Function updates
conversation.FunctionCallStarted.Subscribe(f => AnsiConsole.MarkupLine($"[green]Function call: {f.Name}({f.Arguments})[/]"));
conversation.FunctionCallFinished.Subscribe(f => AnsiConsole.MarkupLine($"[green]Function call finished: {f.result}[/]"));

// Cost updates
conversation.SetupCost(5f / 1_000_000, 20f / 1_000_000, 100f / 1_000_000, 200f / 1_000_000);
conversation.TotalCost.Subscribe(c => AnsiConsole.MarkupLine($"[gray]Total cost: {c}[/]"));

// Setup speaker output
SpeakerOutput speakerOutput = new();
conversation.AudioDeltaUpdates.Subscribe(d => speakerOutput.EnqueueForPlayback(d.Delta));

// Setup microphone input
MicrophoneAudioStream microphone = MicrophoneAudioStream.Start();
await conversation.SendAudioAsync(microphone);

// Show potential errors
conversation.ErrorMessages.Subscribe(txt => AnsiConsole.MarkupLine($"[red]Error: {txt}[/]"));

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

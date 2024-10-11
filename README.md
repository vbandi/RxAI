# RxAI: the RX.Net way to AI

RxAI is a C# library that provides a reactive programming interface for interacting with OpenAI and Azure Realtime API. It simplifies the process of creating real-time, interactive AI-powered applications using Reactive Extensions (Rx).

## Features

- Use the Realtime API from OpenAI and Azure OpenAI with a reactive (RX.Net) programming model
- Real-time conversation capabilities with audio input and output
- Function calling support for extending AI capabilities
- Built-in cost tracking and management
- Customizable conversation options

![image](https://github.com/user-attachments/assets/d256131f-f3c6-4fef-b596-0754458ef7de)


## Project Structure

The RxAI solution consists of two main projects:

1. **RxAI.Realtime**: This is the core library project that contains the main functionality for interacting with OpenAI and Azure Realtime API. It includes the `RealtimeConversationClientRX` class, which provides the reactive interface for AI conversations, as well as supporting classes for function calling and other utilities.

2. **RxAI.ConsoleDemo**: This is a console application project that demonstrates how to use the RxAI.Realtime library. It provides a working example of setting up and running an interactive AI conversation with audio input and output, custom function calling, and cost tracking.

## Installation

To use RxAI in your project, you need to:

1. Clone this repository
2. Add a reference to the RxAI.Realtime project in your solution
3. Install the required NuGet packages (OpenAI, System.Reactive, etc.)

## Running the Demo

To run the RxAI.ConsoleDemo and interact with the AI:

### Using Visual Studio

1. Set your OpenAI API key as an environment variable named `OPENAI_API_KEY`. This must be done before launching Visual Studio.
   - On Windows: Open Command Prompt and run `setx OPENAI_API_KEY your_api_key_here`
   - On macOS/Linux: Add `export OPENAI_API_KEY=your_api_key_here` to your shell profile file (e.g., ~/.bash_profile, ~/.zshrc)
2. Open the RxAI.sln solution file in Visual Studio.
3. Set the RxAI.ConsoleDemo project as the startup project.
4. Press F5 or click "Start Debugging" to run the demo.

### Using Command Line

1. Navigate to the RxAI.ConsoleDemo project directory in your terminal.
2. Set your OpenAI API key as an environment variable:
   - On Windows: `set OPENAI_API_KEY=your_api_key_here`
   - On macOS/Linux: `export OPENAI_API_KEY=your_api_key_here`
3. Run the following commands:
   ```
   dotnet build
   dotnet run
   ```

Once the demo is running, you can interact with the AI using your microphone for input and speakers for output. The console will display transcriptions, function calls, and cost information.

### Tips
- To exercise the demo's function calling capabilities, try saying: "Add two random numbers". This will demonstrate how the AI can use the custom Calculator functions to perform operations.
- Avoid feedback loops by muting your speakers or using headphones to prevent the AI's output from being picked up by the microphone.

## Usage

Here's a simple example of how to use RxAI to create an interactive AI conversation:

```csharp
using System.Text;
using OpenAI.RealtimeConversation;
using RxAI.Realtime.FunctionCalling;
using RxAI.Realtime;

// Set up the OpenAI API key
string? openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(openAIKey))
{
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
    return;
}

// Create a conversation client
RealtimeConversationClientRX conversation = RealtimeConversationClientRX.FromOpenAIKey(openAIKey);

// Set up conversation options
ConversationSessionOptions options = new()
{
    Instructions = "You are an annoyingly rude assistant. Use lots of sarcasm and emojis.",
    InputTranscriptionOptions = new() { Model = ConversationTranscriptionModel.Whisper1 },
};

// Initialize the conversation with custom functions
var calculator = new Calculator();
await conversation.InitializeSessionAsync(options, FunctionCallingHelper.GetFunctionDefinitions(calculator));

// Subscribe to various events
conversation.InputTranscriptionFinishedUpdates.Subscribe(t => Console.WriteLine(t.Transcript));
conversation.OutputTranscriptionDeltaUpdates.Subscribe(u => Console.Write(u.Delta));
conversation.FunctionCallStarted.Subscribe(f => Console.WriteLine($"Function call: {f.Name}({f.Arguments})"));
conversation.FunctionCallFinished.Subscribe(f => Console.WriteLine($"Function call finished: {f.result}"));

// Set up cost tracking
conversation.SetupCost(5f / 1_000_000, 20f / 1_000_000, 100f / 1_000_000, 200f / 1_000_000);
conversation.TotalCost.Subscribe(c => Console.WriteLine($"Total cost: {c}"));

// Set up audio input and output
SpeakerOutput speakerOutput = new();
conversation.AudioDeltaUpdates.Subscribe(d => speakerOutput.EnqueueForPlayback(d.Delta));

MicrophoneAudioStream microphone = MicrophoneAudioStream.Start();
await conversation.SendAudioAsync(microphone);

// Start the conversation
await conversation.StartResponseTurnAsync();

Console.WriteLine("Starting conversation...");

while (true)
{
    await Task.Delay(10);
}

// Custom function definitions
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
```

## How It Works

1. The code sets up a `RealtimeConversationClientRX` using an OpenAI API key.
2. Conversation options are configured, including custom instructions for the AI.
3. The conversation is initialized with custom functions (a Calculator in this example).
4. Various event subscriptions are set up to handle transcription updates, function calls, and cost tracking.
5. Audio input (microphone) and output (speaker) are configured for real-time interaction.
6. The conversation is started, allowing for continuous interaction with the AI.

This example demonstrates the simplicity and power of using RxAI to create interactive AI applications with real-time audio capabilities and custom function support.

## License

[MIT License](LICENSE)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

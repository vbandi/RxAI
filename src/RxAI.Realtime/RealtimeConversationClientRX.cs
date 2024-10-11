#pragma warning disable OPENAI002

using System.ClientModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using OpenAI;
using OpenAI.RealtimeConversation;
using RxAI.Realtime.FunctionCalling;

namespace RxAI.Realtime;

/// <summary>
/// A reactive wrapper around <see cref="RealtimeConversationClient"/>.
/// </summary>
public class RealtimeConversationClientRX
{
    private readonly RealtimeConversationClient _client;
    private RealtimeConversationSession? _session;
    private readonly Subject<ConversationUpdate> _updates = new();
    private readonly Dictionary<string, FunctionDefinition> _functionDefinitions = [];
    private readonly Subject<FunctionCall> _functionCallStarted = new();
      private readonly Subject<(FunctionCall functionCall, string? result)> _functionCallEnded = new();

    public IObservable<ConversationUpdate> Updates => _updates.AsObservable();
    public IObservable<ConversationSessionStartedUpdate> SessionStartedUpdates => _updates.OfType<ConversationSessionStartedUpdate>().AsObservable();
    public IObservable<ConversationAudioDeltaUpdate> AudioDeltaUpdates => _updates.OfType<ConversationAudioDeltaUpdate>().AsObservable();
    public IObservable<ConversationAudioDoneUpdate> AudioDoneUpdates => _updates.OfType<ConversationAudioDoneUpdate>().AsObservable();
    public IObservable<ConversationContentPartFinishedUpdate> PartFinishedUpdates => _updates.OfType<ConversationContentPartFinishedUpdate>().AsObservable();
    public IObservable<ConversationContentPartStartedUpdate> ContentPartStartedUpdates => _updates.OfType<ConversationContentPartStartedUpdate>().AsObservable();
    public IObservable<ConversationErrorUpdate> ErrorUpdates => _updates.OfType<ConversationErrorUpdate>().AsObservable();
    public IObservable<ConversationFunctionCallArgumentsDoneUpdate> FunctionCallArgumentsDoneUpdates => _updates.OfType<ConversationFunctionCallArgumentsDoneUpdate>().AsObservable();
    public IObservable<ConversationFunctionCallArgumentsDeltaUpdate> FunctionCallArgumentsDeltaUpdates => _updates.OfType<ConversationFunctionCallArgumentsDeltaUpdate>().AsObservable();
    public IObservable<ConversationInputAudioBufferClearedUpdate> InputAudioBufferClearedUpdates => _updates.OfType<ConversationInputAudioBufferClearedUpdate>().AsObservable();
    public IObservable<ConversationInputAudioBufferCommittedUpdate> InputAudioBufferCommittedUpdates => _updates.OfType<ConversationInputAudioBufferCommittedUpdate>().AsObservable();
    public IObservable<ConversationInputSpeechFinishedUpdate> InputSpeechFinishedUpdates => _updates.OfType<ConversationInputSpeechFinishedUpdate>().AsObservable();
    public IObservable<ConversationInputSpeechStartedUpdate> InputSpeechStartedUpdates => _updates.OfType<ConversationInputSpeechStartedUpdate>().AsObservable();
    public IObservable<ConversationInputTranscriptionFailedUpdate> InputTranscriptionFailedUpdates => _updates.OfType<ConversationInputTranscriptionFailedUpdate>().AsObservable();
    public IObservable<ConversationInputTranscriptionFinishedUpdate> InputTranscriptionFinishedUpdates => _updates.OfType<ConversationInputTranscriptionFinishedUpdate>().AsObservable();
    public IObservable<ConversationItemDeletedUpdate> ItemDeletedUpdates => _updates.OfType<ConversationItemDeletedUpdate>().AsObservable();
    public IObservable<ConversationItemFinishedUpdate> ItemFinishedUpdates => _updates.OfType<ConversationItemFinishedUpdate>().AsObservable();
    public IObservable<ConversationItemStartedUpdate> ItemStartedUpdates => _updates.OfType<ConversationItemStartedUpdate>().AsObservable();
    public IObservable<ConversationOutputTranscriptionDeltaUpdate> OutputTranscriptionDeltaUpdates => _updates.OfType<ConversationOutputTranscriptionDeltaUpdate>().AsObservable();
    public IObservable<ConversationOutputTranscriptionFinishedUpdate> OutputTranscriptionFinishedUpdates => _updates.OfType<ConversationOutputTranscriptionFinishedUpdate>().AsObservable();
    public IObservable<ConversationResponseFinishedUpdate> ResponseFinishedUpdates => _updates.OfType<ConversationResponseFinishedUpdate>().AsObservable();
    public IObservable<ConversationResponseStartedUpdate> ResponseStartedUpdates => _updates.OfType<ConversationResponseStartedUpdate>().AsObservable();
    public IObservable<ConversationSessionConfiguredUpdate> SessionConfiguredUpdates => _updates.OfType<ConversationSessionConfiguredUpdate>().AsObservable();
    public IObservable<ConversationTextDeltaUpdate> TextDeltaUpdates => _updates.OfType<ConversationTextDeltaUpdate>().AsObservable();
    public IObservable<ConversationTextDoneUpdate> TextDoneUpdates => _updates.OfType<ConversationTextDoneUpdate>().AsObservable();
    public IObservable<FunctionCall> FunctionCallStarted => _functionCallStarted.AsObservable();
    public IObservable<(FunctionCall functionCall, string? result)> FunctionCallEnded => _functionCallEnded.AsObservable();

    public BehaviorSubject<Usage?> Usages { get; } = new(new Usage());

    public float TextInputPrice { get; set; }
    public float TextOutputPrice { get; set; }
    public float AudioInputPrice { get; set; }
    public float AudioOutputPrice { get; set; }
    
    public BehaviorSubject<float> TotalCost { get; } = new(0);
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RealtimeConversationClientRX"/> class.
    /// </summary>
    /// <param name="client">The underlying <see cref="RealtimeConversationClient"/>.</param>
    public RealtimeConversationClientRX(RealtimeConversationClient client)
    {
        _client = client;

        ItemFinishedUpdates
            .Where(update => !string.IsNullOrEmpty(update.FunctionName))
            .Subscribe(update => HandleFunctionCall(update).FireAndForget());
    }

    /// <summary>
    /// Initializes a new conversation session.
    /// </summary>
    /// <param name="options">The options to use for the session.</param>
    /// <param name="functionDefinitions">The function definitions to use for the session.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is already initialized.</exception>
    public async Task InitializeSessionAsync(ConversationSessionOptions? options = null, IEnumerable<FunctionDefinition>? functionDefinitions = null, CancellationToken cancellationToken = default)
    {
        if (_session != null)
            throw new InvalidOperationException("Session already initialized");

        _session = await _client.StartConversationSessionAsync(cancellationToken);

        options ??= new ConversationSessionOptions
        {
            InputTranscriptionOptions = new() { Model = ConversationTranscriptionModel.Whisper1 },
            Instructions = "You are an annoyingly rude assistant. Use lots of sarcasm and emojis. Only speak in Hungarian."
        };

        if (functionDefinitions != null)
            SetFunctionDefinitions(options, functionDefinitions);

        await UpdateSessionOptionsAsync(options, cancellationToken);

        ResponseFinishedUpdates.Subscribe(UpdateUsage);

        _ = Task.Run(async () =>  // note: potential threading issues here
        {
            await foreach (ConversationUpdate update in _session.ReceiveUpdatesAsync(cancellationToken))
                _updates.OnNext(update);
        }, cancellationToken);
    }

    /// <summary>
    /// Updates the usage statistics.
    /// </summary>
    /// <param name="update">The update to process.</param>
    private void UpdateUsage(ConversationResponseFinishedUpdate update)
    {
        Usage? usage = Usage.ParseFromBinaryData(update.GetRawContent());

        if (usage == null)
            return;

        float totalCost = CalculateTotalCost(usage);

        if (Usages.Value != usage)
        {
            Usages.OnNext(usage);
            TotalCost.OnNext(totalCost);
        }
    }

    /// <summary>
    /// Calculates the total cost based on the usage.
    /// </summary>
    /// <param name="usage">The usage statistics.</param>
    /// <returns>The total cost.</returns>
    private float CalculateTotalCost(Usage usage)
    {
        float totalCost = 0;

        if (usage.InputTokenDetails != null)
        {
            totalCost += usage.InputTokenDetails.TextTokens * TextInputPrice;
            totalCost += usage.InputTokenDetails.AudioTokens * AudioInputPrice;
        }

        if (usage.OutputTokenDetails != null)
        {
            totalCost += usage.OutputTokenDetails.TextTokens * TextOutputPrice;
            totalCost += usage.OutputTokenDetails.AudioTokens * AudioOutputPrice;
        }

        return totalCost;
    }

    /// <summary>
    /// Sets up the cost of the tokens.
    /// </summary>
    /// <param name="textInputTokenPrice">The price of a text input token.</param>
    /// <param name="textOutputTokenPrice">The price of a text output token.</param>
    /// <param name="audioInputTokenPrice">The price of an audio input token.</param>
    /// <param name="audioOutputTokenPrice">The price of an audio output token.</param>
    public void SetupCost(float textInputTokenPrice, float textOutputTokenPrice, float audioInputTokenPrice, float audioOutputTokenPrice)
    {
        TextInputPrice = textInputTokenPrice;
        TextOutputPrice = textOutputTokenPrice;
        AudioInputPrice = audioInputTokenPrice;
        AudioOutputPrice = audioOutputTokenPrice;
    }

    /// <summary>
    /// Updates the session options.
    /// </summary>
    /// <param name="options">The new session options.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task UpdateSessionOptionsAsync(ConversationSessionOptions options, CancellationToken cancellationToken = default)
    {
        VerifySession();

        await _session!.ConfigureSessionAsync(options, cancellationToken);
    }

    /// <summary>
    /// Transmits audio data from a stream, ending the client turn once the stream is complete.
    /// </summary>
    /// <param name="audio">The audio stream to transmit.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    public async Task SendAudioAsync(Stream audio, CancellationToken cancellationToken = default)
    {
        VerifySession();
        await _session!.SendAudioAsync(audio, cancellationToken);
    }

    /// <summary>
    /// Starts a response turn in the conversation session.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    public async Task StartResponseTurnAsync(CancellationToken cancellationToken = default)
    {
        VerifySession();
        await _session!.StartResponseTurnAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a user message to the conversation session.
    /// </summary>
    /// <param name="text">The text of the user message.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    public async Task SendUserMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        VerifySession();
        ConversationItem item = ConversationItem.CreateUserMessage([ConversationContentPart.FromInputText(text)]);
        await _session!.AddItemAsync(item, cancellationToken);
    }

    /// <summary>
    /// Sends a system message to the conversation session.
    /// </summary>
    /// <param name="text">The text of the system message.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    public async Task SendSystemMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        VerifySession();
        ConversationItem item = ConversationItem.CreateSystemMessage(null, [ConversationContentPart.FromInputText(text)]);
        await _session!.AddItemAsync(item, cancellationToken);
    }

    /// <summary>
    /// Sends an assistant message to the conversation session.
    /// </summary>
    /// <param name="text">The text of the assistant message.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    public async Task SendAssistantMessageAsync(string text, CancellationToken cancellationToken = default)
    {
        VerifySession();
        ConversationItem item = ConversationItem.CreateAssistantMessage([ConversationContentPart.FromInputText(text)]);
        await _session!.AddItemAsync(item, cancellationToken);
    }

    /// <summary>
    /// Sends a function message to the conversation session.
    /// </summary>
    /// <param name="callId">The ID of the function call.</param>
    /// <param name="output">The output of the function call.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    public async Task SendFunctionMessageAsync(string callId, string output, CancellationToken cancellationToken = default)
    {
        VerifySession();
        ConversationItem item = ConversationItem.CreateFunctionCallOutput(callId, output);
        await _session!.AddItemAsync(item, cancellationToken);
    }

    /// <summary>
    /// Creates a <see cref="RealtimeConversationClientRX"/> instance from an OpenAI API key.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <param name="model">The model to use for the conversation.</param>
    /// <param name="options">Optional OpenAI client options.</param>
    /// <returns>A new instance of <see cref="RealtimeConversationClientRX"/>.</returns>
    public static RealtimeConversationClientRX FromOpenAIKey(
        string apiKey,
        string model = "gpt-4o-realtime-preview-2024-10-01",
        OpenAIClientOptions? options = null)
    {
        OpenAIClient client = options == null ? new(apiKey) : new(new ApiKeyCredential(apiKey), options);
        return FromOpenAIClient(client, model);
    }

    /// <summary>
    /// Creates a <see cref="RealtimeConversationClientRX"/> instance from an Azure credential.
    /// </summary>
    /// <param name="credential">The Azure API key credential.</param>
    /// <param name="model">The model to use for the conversation.</param>
    /// <param name="options">Optional OpenAI client options.</param>
    /// <returns>A new instance of <see cref="RealtimeConversationClientRX"/>.</returns>
    public static RealtimeConversationClientRX FromAzureCredential(
        ApiKeyCredential credential,
        string model = "gpt-4o-realtime-preview-2024-10-01",
        OpenAIClientOptions? options = null)
    {
        OpenAIClient client = options == null ? new(credential) : new(credential, options);
        return FromOpenAIClient(client, model);
    }

    /// <summary>
    /// Creates a <see cref="RealtimeConversationClientRX"/> instance from an OpenAI client.
    /// </summary>
    /// <param name="client">The OpenAI client.</param>
    /// <param name="model">The model to use for the conversation.</param>
    /// <returns>A new instance of <see cref="RealtimeConversationClientRX"/>.</returns>
    public static RealtimeConversationClientRX FromOpenAIClient(OpenAIClient client, string model)
    {
        var conversationClient = client.GetRealtimeConversationClient(model)!;
        return new RealtimeConversationClientRX(conversationClient);
    }

    /// <summary>
    /// Handles a function call.
    /// </summary>
    /// <param name="update">The conversation item finished update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task HandleFunctionCall(ConversationItemFinishedUpdate update)
    {
        if (!_functionDefinitions.TryGetValue(update.FunctionName, out var functionDefinition))
        {
            Console.WriteLine($"Function '{update.FunctionName}' not found in function definitions.");  // TODO: use iobservable instead of Console
            return;
        }

        var functionCall = new FunctionCall { Name = update.FunctionName, Arguments = update.FunctionCallArguments };

        _functionCallStarted.OnNext(functionCall);

        var result = (await FunctionCallingHelper.CallFunctionAsync<object>(functionCall, functionDefinition))?.ToString() ?? "null";

        _functionCallEnded.OnNext((functionCall, result));

        await SendFunctionMessageAsync(update.FunctionCallId, result);

        await StartResponseTurnAsync();
    }

    /// <summary>
    /// Verifies that the session is initialized.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the session is not initialized.</exception>
    private void VerifySession()
    {
        if (_session == null)
            throw new InvalidOperationException("Session not initialized");
    }

    /// <summary>
    /// Sets the function definitions for the conversation session.
    /// </summary>
    /// <param name="options">The conversation session options.</param>
    /// <param name="functionDefinitions">The function definitions to set.</param>
    private void SetFunctionDefinitions(ConversationSessionOptions options, IEnumerable<FunctionDefinition> functionDefinitions)
    {
        options.Tools.Clear();
        _functionDefinitions.Clear();

        foreach (var functionDefinition in functionDefinitions)
        {
            functionDefinition.ThrowIfNameIsNull();

            options.Tools.Add(functionDefinition.ToConversationTool());

            _functionDefinitions[functionDefinition.Name] = functionDefinition;
        }
    }
}

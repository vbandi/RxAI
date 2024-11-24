#pragma warning disable OPENAI002

using System.Reactive.Linq;
using System.Reactive.Subjects;
using OpenAI.RealtimeConversation;
using RxAI.Realtime.FunctionCalling;

namespace RxAI.Realtime;

public partial class RealtimeConversationClientRX
{
    private Subject<string> _errorMessages = new();

    /// <summary>
    /// An observable sequence of all conversation updates.
    /// </summary>
    public IObservable<ConversationUpdate> Updates => _updates.AsObservable();

    /// <summary>
    /// Emitted when a new conversation session is started.
    /// </summary>
    public IObservable<ConversationSessionStartedUpdate> SessionStartedUpdates => _updates.OfType<ConversationSessionStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a chunk of audio is received from the server.
    /// </summary>
    public IObservable<ConversationAudioDeltaUpdate> AudioDeltaUpdates => _updates.OfType<ConversationAudioDeltaUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server has finished sending audio for the current response.
    /// </summary>
    public IObservable<ConversationAudioDoneUpdate> AudioDoneUpdates => _updates.OfType<ConversationAudioDoneUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a content part has finished processing.
    /// </summary>
    public IObservable<ConversationContentPartFinishedUpdate> PartFinishedUpdates => _updates.OfType<ConversationContentPartFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a new content part has started processing.
    /// </summary>
    public IObservable<ConversationContentPartStartedUpdate> ContentPartStartedUpdates => _updates.OfType<ConversationContentPartStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when an error occurs during the conversation.
    /// </summary>
    public IObservable<ConversationErrorUpdate> ErrorUpdates => _updates.OfType<ConversationErrorUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server has finished sending the arguments for a function call.
    /// </summary>
    public IObservable<ConversationFunctionCallArgumentsDoneUpdate> FunctionCallArgumentsDoneUpdates => _updates.OfType<ConversationFunctionCallArgumentsDoneUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a chunk of function call arguments is received from the server.
    /// </summary>
    public IObservable<ConversationFunctionCallArgumentsDeltaUpdate> FunctionCallArgumentsDeltaUpdates => _updates.OfType<ConversationFunctionCallArgumentsDeltaUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the input audio buffer has been cleared.
    /// </summary>
    public IObservable<ConversationInputAudioBufferClearedUpdate> InputAudioBufferClearedUpdates => _updates.OfType<ConversationInputAudioBufferClearedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the input audio buffer has been committed for processing.
    /// </summary>
    public IObservable<ConversationInputAudioBufferCommittedUpdate> InputAudioBufferCommittedUpdates => _updates.OfType<ConversationInputAudioBufferCommittedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the user has finished speaking.
    /// </summary>
    public IObservable<ConversationInputSpeechFinishedUpdate> InputSpeechFinishedUpdates => _updates.OfType<ConversationInputSpeechFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the user starts speaking.
    /// </summary>
    public IObservable<ConversationInputSpeechStartedUpdate> InputSpeechStartedUpdates => _updates.OfType<ConversationInputSpeechStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the transcription of user input fails.
    /// </summary>
    public IObservable<ConversationInputTranscriptionFailedUpdate> InputTranscriptionFailedUpdates => _updates.OfType<ConversationInputTranscriptionFailedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the transcription of user input is complete.
    /// </summary>
    public IObservable<ConversationInputTranscriptionFinishedUpdate> InputTranscriptionFinishedUpdates => _updates.OfType<ConversationInputTranscriptionFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a conversation item has been deleted.
    /// </summary>
    public IObservable<ConversationItemDeletedUpdate> ItemDeletedUpdates => _updates.OfType<ConversationItemDeletedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a conversation item has finished processing.
    /// </summary>
    public IObservable<ConversationItemFinishedUpdate> ItemFinishedUpdates => _updates.OfType<ConversationItemFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a new conversation item has started processing.
    /// </summary>
    public IObservable<ConversationItemStartedUpdate> ItemStartedUpdates => _updates.OfType<ConversationItemStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a chunk of output transcription is received from the server.
    /// </summary>
    public IObservable<ConversationOutputTranscriptionDeltaUpdate> OutputTranscriptionDeltaUpdates => _updates.OfType<ConversationOutputTranscriptionDeltaUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the output transcription is complete.
    /// </summary>
    public IObservable<ConversationOutputTranscriptionFinishedUpdate> OutputTranscriptionFinishedUpdates => _updates.OfType<ConversationOutputTranscriptionFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server has finished sending the response.
    /// </summary>
    public IObservable<ConversationResponseFinishedUpdate> ResponseFinishedUpdates => _updates.OfType<ConversationResponseFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server starts sending a response.
    /// </summary>
    public IObservable<ConversationResponseStartedUpdate> ResponseStartedUpdates => _updates.OfType<ConversationResponseStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the conversation session has been configured.
    /// </summary>
    public IObservable<ConversationSessionConfiguredUpdate> SessionConfiguredUpdates => _updates.OfType<ConversationSessionConfiguredUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a chunk of text is received from the server.
    /// </summary>
    public IObservable<ConversationTextDeltaUpdate> TextDeltaUpdates => _updates.OfType<ConversationTextDeltaUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server has finished sending text for the current response.
    /// </summary>
    public IObservable<ConversationTextDoneUpdate> TextDoneUpdates => _updates.OfType<ConversationTextDoneUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a function call is started.
    /// </summary>
    public IObservable<FunctionCall> FunctionCallStarted => _functionCallStarted.AsObservable();

    /// <summary>
    /// Emitted when a function call is finished, including the result.
    /// </summary>
    public IObservable<(FunctionCall functionCall, string? result)> FunctionCallFinished => _functionCallFinished.AsObservable();

    /// <summary>
    /// Provides the current usage statistics for the conversation.
    /// </summary>
    public BehaviorSubject<Usage?> Usages { get; } = new(new Usage());

    /// <summary>
    /// Provides the current total cost of the conversation.
    /// </summary>
    public BehaviorSubject<float> TotalCost { get; } = new(0);

    /// <summary>
    /// Provides the non-fatal error messages that occur in the <see cref="RealtimeConversationClientRX" /> (such as invalid function calls).
    /// </summary>
    public IObservable<string> ErrorMessages => _errorMessages.AsObservable();
}

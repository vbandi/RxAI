#pragma warning disable OPENAI002

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using OpenAI.Chat;
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

    // Session Lifecycle
    /// <summary>
    /// Emitted when a new conversation session is started.
    /// Corresponds to <see cref="ConversationUpdateKind.SessionStarted"/>.
    /// </summary>
    public IObservable<ConversationSessionStartedUpdate> SessionStartedUpdates => _updates.OfType<ConversationSessionStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the conversation session has been configured.
    /// Corresponds to <see cref="ConversationUpdateKind.SessionConfigured"/>.
    /// </summary>
    public IObservable<ConversationSessionConfiguredUpdate> SessionConfiguredUpdates => _updates.OfType<ConversationSessionConfiguredUpdate>().AsObservable();

    // Response Lifecycle
    /// <summary>
    /// Emitted when the server starts sending a response.
    /// Corresponds to <see cref="ConversationUpdateKind.ResponseStarted"/>.
    /// </summary>
    public IObservable<ConversationResponseStartedUpdate> ResponseStartedUpdates => _updates.OfType<ConversationResponseStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server has finished sending the response.
    /// Corresponds to <see cref="ConversationUpdateKind.ResponseFinished"/>.
    /// </summary>
    public IObservable<ConversationResponseFinishedUpdate> ResponseFinishedUpdates => _updates.OfType<ConversationResponseFinishedUpdate>().AsObservable();

    // Item Lifecycle & Streaming
    /// <summary>
    /// Emitted when a new conversation item is created.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemCreated"/>.
    /// </summary>
    public IObservable<ConversationItemCreatedUpdate> ItemCreatedUpdates => _updates.OfType<ConversationItemCreatedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a conversation item has been deleted.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemDeleted"/>.
    /// </summary>
    public IObservable<ConversationItemDeletedUpdate> ItemDeletedUpdates => _updates.OfType<ConversationItemDeletedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a conversation item has been truncated.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemTruncated"/>.
    /// </summary>
    public IObservable<ConversationItemTruncatedUpdate> ItemTruncatedUpdates => _updates.OfType<ConversationItemTruncatedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a new conversation item has started streaming.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemStreamingStarted"/> ('response.output_item.added').
    /// </summary>
    public IObservable<ConversationItemStreamingStartedUpdate> ItemStreamingStartedUpdates => _updates.OfType<ConversationItemStreamingStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a conversation item has finished streaming.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemStreamingFinished"/> ('response.output_item.done').
    /// </summary>
    public IObservable<ConversationItemStreamingFinishedUpdate> ItemStreamingFinishedUpdates => _updates.OfType<ConversationItemStreamingFinishedUpdate>().AsObservable();

    // Item Content Part Streaming
    /// <summary>
    /// Emitted when a delta for a content part of an item is received.
    /// This can be for text, audio, function call arguments, etc., depending on the specific delta type.
    /// E.g. <see cref="ConversationUpdateKind.ItemStreamingPartTextDelta"/>, <see cref="ConversationUpdateKind.ItemStreamingPartAudioDelta"/>.
    /// </summary>
    public IObservable<ConversationItemStreamingPartDeltaUpdate> ItemStreamingPartDeltaUpdates => _updates.OfType<ConversationItemStreamingPartDeltaUpdate>().AsObservable();

    /// <summary>
    /// Emitted when a content part of an item has finished streaming.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemContentPartFinished"/> ('response.content_part.done').
    /// </summary>
    public IObservable<ConversationItemStreamingPartFinishedUpdate> ItemStreamingPartFinishedUpdates => _updates.OfType<ConversationItemStreamingPartFinishedUpdate>().AsObservable();

    // Specific Content Streaming Finished Events
    /// <summary>
    /// Emitted when the server has finished sending audio for the current response item.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemStreamingPartAudioFinished"/> ('response.audio.done').
    /// </summary>
    public IObservable<ConversationItemStreamingAudioFinishedUpdate> ItemStreamingAudioFinishedUpdates => _updates.OfType<ConversationItemStreamingAudioFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server has finished sending an audio transcription for the current response item.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemStreamingPartAudioTranscriptionFinished"/> ('response.audio_transcript.done').
    /// </summary>
    public IObservable<ConversationItemStreamingAudioTranscriptionFinishedUpdate> ItemStreamingAudioTranscriptionFinishedUpdates => _updates.OfType<ConversationItemStreamingAudioTranscriptionFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the server has finished sending text for the current response item.
    /// Corresponds to <see cref="ConversationUpdateKind.ItemStreamingPartTextFinished"/> ('response.text.done').
    /// </summary>
    public IObservable<ConversationItemStreamingTextFinishedUpdate> ItemStreamingTextFinishedUpdates => _updates.OfType<ConversationItemStreamingTextFinishedUpdate>().AsObservable();

    // Audio Delta (Special Case)
    /// <summary>
    /// Emitted when a chunk of audio is received from the server.
    /// Note: This observable selects <c>AudioBytesUpdate</c> from <c>StreamingChatOutputAudioUpdate</c>.
    /// Consider using more specific audio delta events like those derived from <see cref="ConversationItemStreamingPartDeltaUpdate"/> if appropriate.
    /// </summary>
    public IObservable<BinaryData> AudioDeltaUpdates => _updates.OfType<ConversationItemStreamingPartDeltaUpdate>().
        Where(x => x.AudioBytes is not null).Select(x => x.AudioBytes);

    // Input Events
    /// <summary>
    /// Emitted when the input audio buffer has been cleared.
    /// Corresponds to <see cref="ConversationUpdateKind.InputAudioCleared"/>.
    /// </summary>
    public IObservable<ConversationInputAudioClearedUpdate> InputAudioClearedUpdates => _updates.OfType<ConversationInputAudioClearedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the input audio buffer has been committed for processing.
    /// Corresponds to <see cref="ConversationUpdateKind.InputAudioCommitted"/>.
    /// </summary>
    public IObservable<ConversationInputAudioCommittedUpdate> InputAudioCommittedUpdates => _updates.OfType<ConversationInputAudioCommittedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the user starts speaking.
    /// Corresponds to <see cref="ConversationUpdateKind.InputSpeechStarted"/>.
    /// </summary>
    public IObservable<ConversationInputSpeechStartedUpdate> InputSpeechStartedUpdates => _updates.OfType<ConversationInputSpeechStartedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the user has finished speaking.
    /// Corresponds to <see cref="ConversationUpdateKind.InputSpeechStopped"/>.
    /// </summary>
    public IObservable<ConversationInputSpeechFinishedUpdate> InputSpeechFinishedUpdates => _updates.OfType<ConversationInputSpeechFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the transcription of user input is complete.
    /// Corresponds to <see cref="ConversationUpdateKind.InputTranscriptionFinished"/>.
    /// </summary>
    public IObservable<ConversationInputTranscriptionFinishedUpdate> InputTranscriptionFinishedUpdates => _updates.OfType<ConversationInputTranscriptionFinishedUpdate>().AsObservable();

    /// <summary>
    /// Emitted when the transcription of user input fails.
    /// Corresponds to <see cref="ConversationUpdateKind.InputTranscriptionFailed"/>.
    /// </summary>
    public IObservable<ConversationInputTranscriptionFailedUpdate> InputTranscriptionFailedUpdates => _updates.OfType<ConversationInputTranscriptionFailedUpdate>().AsObservable();

    // Other Informational Updates
    /// <summary>
    /// Emitted when rate limits have been updated.
    /// Corresponds to <see cref="ConversationUpdateKind.RateLimitsUpdated"/>.
    /// </summary>
    public IObservable<ConversationRateLimitsUpdate> RateLimitsUpdates => _updates.OfType<ConversationRateLimitsUpdate>().AsObservable();

    /// <summary>
    /// Emitted when an error occurs during the conversation.
    /// Corresponds to <see cref="ConversationUpdateKind.Error"/>.
    /// </summary>
    public IObservable<ConversationErrorUpdate> ErrorUpdates => _updates.OfType<ConversationErrorUpdate>().AsObservable();

    // Function Calling Related (Existing - Unchanged by this task)
    /// <summary>
    /// Emitted when a function call is started.
    /// </summary>
    public IObservable<FunctionCall> FunctionCallStarted => _functionCallStarted.AsObservable();

    /// <summary>
    /// Emitted when a function call is finished, including the result.
    /// </summary>
    public IObservable<(FunctionCall functionCall, string? result)> FunctionCallFinished => _functionCallFinished.AsObservable();

    // Usage and Cost (Existing - Unchanged by this task)
    /// <summary>
    /// Provides the current usage statistics for the conversation.
    /// </summary>
    public BehaviorSubject<Usage?> Usages { get; } = new(new Usage());

    /// <summary>
    /// Provides the current total cost of the conversation.
    /// </summary>
    public BehaviorSubject<float> TotalCost { get; } = new(0);

    // General Error Messages (Existing - Unchanged by this task)
    /// <summary>
    /// Provides the non-fatal error messages that occur in the <see cref="RealtimeConversationClientRX" /> (such as invalid function calls).
    /// </summary>
    public IObservable<string> ErrorMessages => _errorMessages.AsObservable();
}

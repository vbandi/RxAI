using System.Text.Json;
using System.Text.Json.Serialization;

namespace RxAI.Realtime;

/// <summary>
/// Represents usage statistics for API calls.
/// </summary>
public class Usage
{
    /// <summary>
    /// Gets or sets the total number of tokens used in the API call.
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of input tokens used in the API call.
    /// </summary>
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens used in the API call.
    /// </summary>
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the detailed token usage for input.
    /// </summary>
    [JsonPropertyName("input_token_details")]
    public TokenDetails? InputTokenDetails { get; set; }

    /// <summary>
    /// Gets or sets the detailed token usage for output.
    /// </summary>
    [JsonPropertyName("output_token_details")]
    public TokenDetails? OutputTokenDetails { get; set; }

    /// <summary>
    /// Parses usage information from binary data.
    /// </summary>
    /// <param name="binaryData">The binary data to parse.</param>
    /// <returns>A <see cref="Usage"/> object if parsing is successful; otherwise, null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when parsing fails due to JSON errors, missing keys, or unexpected issues.</exception>
    public static Usage? ParseFromBinaryData(BinaryData binaryData)
    {
        try
        {
            string jsonString = binaryData.ToString();
            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement usageElement = doc.RootElement.GetProperty("response").GetProperty("usage");

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true,
            };

            Usage? result = JsonSerializer.Deserialize<Usage>(usageElement.GetRawText(), options);
            return result;
        }
        catch (JsonException jsonEx)
        {
            throw new InvalidOperationException("JSON Parsing Error.", jsonEx);
        }
        catch (KeyNotFoundException keyEx)
        {
            throw new InvalidOperationException("Required key not found in JSON.", keyEx);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An unexpected error occurred.", ex);
        }
    }
}

/// <summary>
/// Represents detailed token usage statistics.
/// </summary>
public class TokenDetails
{
    /// <summary>
    /// Gets or sets the number of cached tokens.
    /// </summary>
    [JsonPropertyName("cached_tokens")]
    public int CachedTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of text tokens.
    /// </summary>
    [JsonPropertyName("text_tokens")]
    public int TextTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of audio tokens.
    /// </summary>
    [JsonPropertyName("audio_tokens")]
    public int AudioTokens { get; set; }
}

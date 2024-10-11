using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenAI.RealtimeConversation;

#pragma warning disable OPENAI002 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace RxAI.Realtime.FunctionCalling;

/// <summary>
/// Helper methods for Function Calling.
/// </summary>
public static class FunctionCallingHelper
{
    /// <summary>
    /// Returns a <see cref="FunctionDefinition"/> from the provided method, using any
    /// <see cref="FunctionDescriptionAttribute"/> and <see cref="ParameterDescriptionAttribute"/> attributes.
    /// </summary>
    /// <param name="methodInfo">The method to create the <see cref="FunctionDefinition"/> from.</param>
    /// <param name="obj">Optional object instance for non-static methods.</param>
    /// <returns>The <see cref="FunctionDefinition"/> created.</returns>
    public static FunctionDefinition GetFunctionDefinition(MethodInfo methodInfo, object? obj = null)
    {
        var methodDescriptionAttribute = methodInfo.GetCustomAttribute<FunctionDescriptionAttribute>();

        var result = new FunctionDefinition
        {
            Name = methodDescriptionAttribute?.Name ?? methodInfo.Name,
            Description = methodDescriptionAttribute?.Description,
            Parameters = new FunctionParameters
            {
                Properties = [],
                Required = []
            },
            Owner = obj != null ? new WeakReference<object>(obj) : null
        };

        var parameters = methodInfo.GetParameters().ToList();

        foreach (var parameter in parameters)
        {
            var parameterDescriptionAttribute = parameter.GetCustomAttribute<ParameterDescriptionAttribute>();
            var description = parameterDescriptionAttribute?.Description;

            FunctionProperty property = CreateFunctionProperty(parameter, parameterDescriptionAttribute, description);

            result.Parameters.Properties.Add(
                parameterDescriptionAttribute?.Name ?? parameter.Name!,
                property);

            if (parameterDescriptionAttribute?.Required ?? true)
                result.Parameters.Required.Add(parameterDescriptionAttribute?.Name ?? parameter.Name!);
        }

        return result;
    }

    /// <summary>
    /// Creates a <see cref="FunctionProperty"/> based on the parameter type and attributes.
    /// </summary>
    /// <param name="parameter">The parameter info.</param>
    /// <param name="parameterDescriptionAttribute">The parameter description attribute.</param>
    /// <param name="description">The parameter description.</param>
    /// <returns>A <see cref="FunctionProperty"/> object.</returns>
    private static FunctionProperty CreateFunctionProperty(ParameterInfo parameter, ParameterDescriptionAttribute? parameterDescriptionAttribute, string? description)
    {
        return parameter.ParameterType switch
        {
            var t when t == typeof(int) => new FunctionProperty { Type = "integer", Description = description },
            var t when t == typeof(float) || t == typeof(double) => new FunctionProperty { Type = "number", Description = description },
            var t when t == typeof(bool) => new FunctionProperty { Type = "boolean", Description = description },
            var t when t == typeof(string) => new FunctionProperty { Type = "string", Description = description },
            var t when t.IsEnum => new FunctionProperty
            {
                Type = "string",
                Enum = string.IsNullOrEmpty(parameterDescriptionAttribute?.Enum) ?
                    [.. Enum.GetNames(parameter.ParameterType)] :
                    parameterDescriptionAttribute.Enum.Split(",").Select(x => x.Trim()).ToList(),
                Description = description
            },
            _ => throw new Exception($"Parameter type '{parameter.ParameterType}' not supported")
        };
    }

    /// <summary>
    /// Enumerates the methods in the provided object, and returns a <see cref="List{T}"/> of
    /// <see cref="FunctionDefinition"/> for all methods marked with a <see cref="FunctionDescriptionAttribute"/>.
    /// </summary>
    /// <param name="obj">The object to analyze.</param>
    /// <returns>A list of <see cref="FunctionDefinition"/> objects.</returns>
    public static List<FunctionDefinition> GetFunctionDefinitions(object obj)
    {
        var type = obj.GetType();
        return GetFunctionDefinitions(type, obj);
    }

    /// <summary>
    /// Enumerates the methods in the provided type, and returns a <see cref="List{T}"/> of
    /// <see cref="FunctionDefinition"/> for all methods.
    /// </summary>
    /// <typeparam name="T">The type to analyze.</typeparam>
    /// <returns>A list of <see cref="FunctionDefinition"/> objects.</returns>
    public static List<FunctionDefinition> GetFunctionDefinitions<T>() => GetFunctionDefinitions(typeof(T));

    /// <summary>
    /// Enumerates the methods in the provided type, and returns a <see cref="List{T}"/> of
    /// <see cref="FunctionDefinition"/> for all methods.
    /// </summary>
    /// <param name="type">The type to analyze.</param>
    /// <param name="obj">Optional object instance for non-static methods.</param>
    /// <returns>A list of <see cref="FunctionDefinition"/> objects.</returns>
    public static List<FunctionDefinition> GetFunctionDefinitions(Type type, object? obj = null)
    {
        var methods = type.GetMethods();

        var result = methods
            .Where(method => method.GetCustomAttribute<FunctionDescriptionAttribute>() != null)
            .Select(method => GetFunctionDefinition(method, obj)).ToList();

        return result;
    }

    /// <summary>
    /// Calls the function on the provided object, using the provided <see cref="FunctionCall"/> and returns the result of
    /// the call.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="functionCall">The <see cref="FunctionCall"/> provided by the LLM.</param>
    /// <param name="functionDefinition">The <see cref="FunctionDefinition"/> containing the weak reference to the object.</param>
    /// <param name="allowPartialArguments">Whether to allow partial arguments.</param>
    /// <returns>The result of the function call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidFunctionCallException">If the method is not found, the return type is incompatible, or the arguments are invalid.</exception>
    public static T? CallFunction<T>(FunctionCall functionCall, FunctionDefinition functionDefinition, bool allowPartialArguments = false)
    {
        var (obj, methodInfo, args) = PrepareMethodInvocation<T>(functionCall, functionDefinition, allowPartialArguments);

        var result = methodInfo.Invoke(obj, [.. args]);
        return (T?)result;
    }

    /// <summary>
    /// Calls the async function on the provided object, using the provided <see cref="FunctionCall"/> and returns the result of
    /// the call asynchronously.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="functionCall">The <see cref="FunctionCall"/> provided by the LLM.</param>
    /// <param name="functionDefinition">The <see cref="FunctionDefinition"/> containing the weak reference to the object.</param>
    /// <param name="allowPartialArguments">Whether to allow partial arguments.</param>
    /// <returns>The result of the async function call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="functionCall"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidFunctionCallException">If the method is not found, the return type is incompatible, or the arguments are invalid.</exception>
    public static async Task<T?> CallFunctionAsync<T>(FunctionCall functionCall, FunctionDefinition functionDefinition, bool allowPartialArguments = false)
    {
        var (obj, methodInfo, args) = PrepareMethodInvocation<T>(functionCall, functionDefinition, allowPartialArguments);

        var returnType = methodInfo.ReturnType;

        if (returnType == typeof(Task)) // Task type
        {
            await (Task)methodInfo.Invoke(obj, [.. args])!;
            return default;
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)) // Generic Task type
        {
            var resultTask = (Task)methodInfo.Invoke(obj, [.. args])!;
            await resultTask;
            var resultProperty = resultTask.GetType().GetProperty("Result");
            return (T?)resultProperty?.GetValue(resultTask);
        }
        else // Non-Task type
        {
            return (T?)methodInfo.Invoke(obj, [.. args]);
        }
    }

    /// <summary>
    /// Prepares the method invocation by parsing the arguments and checking the method signature.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="functionCall">The <see cref="FunctionCall"/> provided by the LLM.</param>
    /// <param name="functionDefinition">The <see cref="FunctionDefinition"/> containing the weak reference to the object.</param>
    /// <param name="allowPartialArguments">Whether to allow partial arguments.</param>
    /// <returns>A tuple containing the object, <see cref="MethodInfo"/>, and arguments.</returns>
    /// <exception cref="InvalidFunctionCallException">If the method is not found, the return type is incompatible, or the arguments are invalid.</exception>
    private static (object obj, MethodInfo methodInfo, List<object?> args) PrepareMethodInvocation<T>(
        FunctionCall functionCall,
        FunctionDefinition functionDefinition,
        bool allowPartialArguments)
    {
        ArgumentNullException.ThrowIfNull(functionCall);

        functionCall.ThrowIfNameIsNull();

        var obj = functionDefinition.GetOwnerOrThrow();

        var methodInfo = FindMethod(obj, functionCall.Name);

        if (!IsCompatibleReturnType<T>(methodInfo.ReturnType))
        {
            throw new InvalidFunctionCallException(
                $"Method '{functionCall.Name}' on type '{obj.GetType()}' has return type '{methodInfo.ReturnType}' but expected '{typeof(T)}' or Task<{typeof(T)}>");
        }

        var parameters = methodInfo.GetParameters().ToList();

        if (TryParseArguments(functionCall.Arguments, out var arguments, allowPartialArguments ? "\"}" : null))
        {
            var args = PrepareArguments(parameters, arguments);
            return (obj, methodInfo, args);
        }

        throw new InvalidFunctionCallException("Failed to parse function arguments.");
    }

    /// <summary>
    /// Finds the method in the object type based on the function name.
    /// </summary>
    /// <param name="obj">The object containing the method.</param>
    /// <param name="functionName">The name of the function to find.</param>
    /// <returns>The <see cref="MethodInfo"/> of the found method.</returns>
    /// <exception cref="InvalidFunctionCallException">If the method is not found.</exception>
    private static MethodInfo FindMethod(object obj, string functionName)
    {
        return obj.GetType().GetMethod(functionName)
            ?? Array.Find(
                obj.GetType().GetMethods(),
                methodInfo => methodInfo.GetCustomAttribute<FunctionDescriptionAttribute>()?.Name == functionName)
            ?? throw new InvalidFunctionCallException($"Method '{functionName}' on type '{obj.GetType()}' not found.");
    }

    /// <summary>
    /// Prepares the arguments for method invocation.
    /// </summary>
    /// <param name="parameters">The parameters of the method.</param>
    /// <param name="arguments">The parsed arguments from the function call.</param>
    /// <returns>A list of prepared arguments.</returns>
    private static List<object?> PrepareArguments(List<ParameterInfo> parameters, Dictionary<string, object> arguments)
    {
        var args = new List<object?>();

        foreach (var parameter in parameters)
        {
            var parameterDescriptionAttribute =
                parameter.GetCustomAttribute<ParameterDescriptionAttribute>();

            var name = parameterDescriptionAttribute?.Name ?? parameter.Name!;
            var argument = arguments.FirstOrDefault(x => x.Key == name);

            if (argument.Key == null) // argument not found - add it as null
            {
                args.Add(parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null);
            }
            else
            {
                var value = parameter.ParameterType.IsEnum
                    ? Enum.Parse(parameter.ParameterType, argument.Value.ToString()!)
                    : ((JsonElement)argument.Value).Deserialize(parameter.ParameterType);

                args.Add(value);
            }
        }

        return args;
    }

    /// <summary>
    /// Checks if the return type is compatible with the provided type.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="returnType">The return type of the method.</param>
    /// <returns><c>true</c> if the return type is compatible with the provided type, <c>false</c> otherwise.</returns>
    private static bool IsCompatibleReturnType<T>(Type returnType)
    {
        return typeof(T).IsAssignableFrom(returnType) ||
               returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                typeof(T).IsAssignableFrom(returnType.GetGenericArguments()[0]);
    }

    /// <summary>
    /// Tries to parse the arguments from the provided JSON string.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="result">The result of the parsing.</param>
    /// <param name="endToken">The end token to use for partial arguments.</param>
    /// <returns><c>true</c> if the arguments were parsed successfully, <c>false</c> otherwise.</returns>
    private static bool TryParseArguments(string? json, out Dictionary<string, object> result, string? endToken = null)
    {
        result = [];
        try
        {
            result = (!string.IsNullOrWhiteSpace(json) ? JsonSerializer.Deserialize<Dictionary<string, object>>(json) : []) ?? [];
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a <see cref="FunctionDefinition"/> to a <see cref="ConversationFunctionTool"/>.
    /// </summary>
    /// <param name="functionDefinition">The <see cref="FunctionDefinition"/> to convert.</param>
    /// <returns>A <see cref="ConversationFunctionTool"/>.</returns>
    public static ConversationFunctionTool ToConversationTool(this FunctionDefinition functionDefinition)
    {
        var arguments = functionDefinition.Parameters?.Properties?.ToDictionary(kvp => kvp.Key, GetArgumentForJson) ?? [];

        var parametersSchema = new
        {
            type = "object",
            properties = arguments,
            required = functionDefinition.Parameters?.Required ?? [],
            additionalProperties = false
        };

        ConversationFunctionTool result = new(functionDefinition.Name)
        {
            Description = functionDefinition.Description,
            Parameters = BinaryData.FromString(JsonSerializer.Serialize(parametersSchema))
        };

        return result;
    }

    /// <summary>
    /// Returns the value of the <see cref="FunctionProperty"/> as a serializable object, ignoring enum and description if they are null.
    /// </summary>
    /// <param name="keyValuePair">The key-value pair representing a function property.</param>
    /// <returns>A dictionary representing the serializable object.</returns>
    private static object GetArgumentForJson(KeyValuePair<string, FunctionProperty> keyValuePair)
    {
        var v = keyValuePair.Value;

        var result = new Dictionary<string, object>
        {
            { "Type", v.Type }
        };

        if (v.Description != null)
            result["Description"] = v.Description;

        if (v.Enum != null)
            result["Enum"] = v.Enum;

        return result;
    }
}

/// <summary>
/// Defines the properties of a function parameter.
/// </summary>
public class FunctionProperty
{
    /// <summary>
    /// Gets or sets the type of the function parameter.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    /// <summary>
    /// Gets or sets the description of the function parameter.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the enum values of the function parameter.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("enum")]
    public List<string>? Enum { get; set; }
}

/// <summary>
/// Defines a function that can be called by the AI.
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the arguments of the function.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Throws an <see cref="InvalidFunctionCallException"/> if the Name is null.
    /// </summary>
    /// <exception cref="InvalidFunctionCallException">If the Name is null.</exception>
    [MemberNotNull(nameof(Name))]
    public void ThrowIfNameIsNull()
    {
        if (Name is null)
            throw new InvalidFunctionCallException("Function Name is null.");
    }
}

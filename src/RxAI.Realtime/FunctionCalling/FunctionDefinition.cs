using System.Diagnostics.CodeAnalysis;

namespace RxAI.Realtime.FunctionCalling;

/// <summary>
/// Defines a function that can be called by the AI.
/// </summary>
public class FunctionDefinition
{
    /// <summary>
    /// Gets or sets the name of the function.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the function.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parameters of the function.
    /// </summary>
    public FunctionParameters? Parameters { get; set; }

    /// <summary>
    /// Gets or sets the type of the function Owner.
    /// </summary>
    public Type? OwnerType { get; set; }

    /// <summary>
    /// Gets or sets a weak reference to the owner object of the function.
    /// </summary>
    public WeakReference<object>? Owner { get; set; }

    [MemberNotNull(nameof(Name))]
    public void ThrowIfNameIsNull()
    {
        if (Name is null)
            throw new InvalidFunctionCallException("Function name is null");
    }
}

/// <summary>
/// Defines the properties of a function parameter.
/// </summary>
public class FunctionParameters
{
    /// <summary>
    /// Gets or sets the properties of the function parameters.
    /// </summary>
    public Dictionary<string, FunctionProperty>? Properties { get; set; }

    /// <summary>
    /// Gets or sets the required parameters of the function.
    /// </summary>
    public List<string>? Required { get; set; }
}

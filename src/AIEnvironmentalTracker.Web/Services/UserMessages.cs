namespace AIEnvironmentalTracker.Web.Services;

/// <summary>Turns validation exceptions into text that is fit to show to the user.</summary>
public static class UserMessages
{
    /// <summary>
    /// Returns the validation message without the " (Parameter 'x')" suffix .NET adds,
    /// which means nothing to the user.
    /// </summary>
    public static string From(ArgumentException ex)
    {
        return ex.ParamName is null
            ? ex.Message
            : ex.Message.Replace($" (Parameter '{ex.ParamName}')", string.Empty, StringComparison.Ordinal);
    }
}

namespace JobApplicationAssistant.Api.Contracts;

public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static ApiError Validation(IReadOnlyDictionary<string, string[]> details) =>
        new("validation_error", "The request payload is invalid.", details);

    public static ApiError NotFound(string message) =>
        new("not_found", message);
}

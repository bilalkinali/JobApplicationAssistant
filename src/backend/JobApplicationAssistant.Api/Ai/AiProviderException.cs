namespace JobApplicationAssistant.Api.Ai;

public abstract class AiProviderException : Exception
{
    protected AiProviderException(string errorCode, string message, int attemptCount, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        AttemptCount = attemptCount;
    }

    public string ErrorCode { get; }

    public int AttemptCount { get; }
}

public sealed class AiProviderUnavailableException : AiProviderException
{
    public AiProviderUnavailableException(string message, int attemptCount = 1, Exception? innerException = null)
        : base("ProviderUnavailable", message, attemptCount, innerException)
    {
    }
}

public sealed class AiInvalidOutputException : AiProviderException
{
    public AiInvalidOutputException(string message, int attemptCount, Exception? innerException = null)
        : base("InvalidOutput", message, attemptCount, innerException)
    {
    }
}

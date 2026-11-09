namespace KemisOnboarding.Api.Services;

public class WorkflowException : Exception
{
    public int StatusCode { get; }

    public WorkflowException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

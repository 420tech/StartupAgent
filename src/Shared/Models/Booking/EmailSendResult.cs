namespace StartupAgent.Shared.Models.Booking;

/// <summary>
/// Email send result with status and retry information
/// </summary>
public class EmailSendResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; set; }
    
    /// <summary>
    /// Whether retry should be attempted
    /// </summary>
    public bool CanRetry { get; set; }
    
    public static EmailSendResult CreateSuccess(string correlationId)
    {
        return new EmailSendResult
        {
            Success = true,
            CorrelationId = correlationId
        };
    }
    
    public static EmailSendResult CreateFailure(string error, string correlationId, bool canRetry = true)
    {
        return new EmailSendResult
        {
            Success = false,
            ErrorMessage = error,
            CorrelationId = correlationId,
            CanRetry = canRetry
        };
    }
}

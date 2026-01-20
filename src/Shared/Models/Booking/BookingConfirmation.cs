namespace StartupAgent.Shared.Models.Booking;

/// <summary>
/// Represents a booking confirmation event
/// </summary>
public class BookingConfirmation
{
    public required string BookingId { get; set; }
    public required string FounderId { get; set; }
    public required string FounderEmail { get; set; }
    public required string FounderName { get; set; }
    public required string SessionId { get; set; }
    
    /// <summary>
    /// ISO 8601 datetime of the scheduled call
    /// </summary>
    public required DateTime ScheduledAt { get; set; }
    
    /// <summary>
    /// Duration in minutes (typically 30)
    /// </summary>
    public int DurationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Price in USD (typically $97)
    /// </summary>
    public decimal PriceUsd { get; set; } = 97m;
    
    /// <summary>
    /// External payment link (e.g., Stripe Checkout or Gumroad)
    /// </summary>
    public required string PaymentLink { get; set; }
    
    /// <summary>
    /// When the confirmation was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Correlation ID for tracking/logging
    /// </summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

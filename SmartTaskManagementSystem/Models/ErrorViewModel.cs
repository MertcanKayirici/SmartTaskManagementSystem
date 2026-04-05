namespace SmartTaskManagementSystem.Models
{
    // ViewModel used for error handling in the application
    // Provides basic diagnostic information for debugging purposes
    public class ErrorViewModel
    {
        // Unique identifier for the current request (used for tracing errors)
        public string? RequestId { get; set; }

        // Indicates whether the RequestId should be displayed in the UI
        // Returns true if RequestId is not null or empty
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
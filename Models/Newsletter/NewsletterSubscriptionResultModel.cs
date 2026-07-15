public class NewsletterSubscriptionResult
{
    public bool Success { get; set; }

    public bool AlreadySubscribed { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? UnsubscribeToken { get; set; }

    public static NewsletterSubscriptionResult Created(
        string unsubscribeToken)
    {
        return new NewsletterSubscriptionResult
        {
            Success = true,
            AlreadySubscribed = false,
            Message = "Subscription completed successfully.",
            UnsubscribeToken = unsubscribeToken
        };
    }

    public static NewsletterSubscriptionResult Existing()
    {
        return new NewsletterSubscriptionResult
        {
            Success = true,
            AlreadySubscribed = true,
            Message = "This email address is already subscribed."
        };
    }

    public static NewsletterSubscriptionResult Failed(
        string message)
    {
        return new NewsletterSubscriptionResult
        {
            Success = false,
            Message = message
        };
    }
}
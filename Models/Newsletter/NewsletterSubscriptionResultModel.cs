public class NewsletterSubscriptionResultModel
{
    public bool Success { get; set; }

    public bool AlreadySubscribed { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? UnsubscribeToken { get; set; }

    public static NewsletterSubscriptionResultModel Created(
        string unsubscribeToken)
    {
        return new NewsletterSubscriptionResultModel
        {
            Success = true,
            AlreadySubscribed = false,
            Message = "Subscription completed successfully.",
            UnsubscribeToken = unsubscribeToken
        };
    }

    public static NewsletterSubscriptionResultModel Existing()
    {
        return new NewsletterSubscriptionResultModel
        {
            Success = true,
            AlreadySubscribed = true,
            Message = "This email address is already subscribed."
        };
    }

    public static NewsletterSubscriptionResultModel Failed(
        string message)
    {
        return new NewsletterSubscriptionResultModel
        {
            Success = false,
            Message = message
        };
    }
}
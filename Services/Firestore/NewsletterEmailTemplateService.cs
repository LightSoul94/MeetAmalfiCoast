using MeetAmalfiCoast.Models;
using System.Net;
using System.Text;

public class NewsletterEmailTemplateService
{
    private readonly IWebHostEnvironment _environment;

    public NewsletterEmailTemplateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string BuildAppointmentReminder(PlanningAppointmentsModel appointment)
    {
        string customerName = WebUtility.HtmlEncode(appointment.Customer);
        string appointmentDate = WebUtility.HtmlEncode(appointment.IsoDate);
        string appointmentStart = WebUtility.HtmlEncode(appointment.Start);
        string appointmentEnd = WebUtility.HtmlEncode(appointment.End);
        string appointmentTitle = WebUtility.HtmlEncode(appointment.Title);

        string content = $"""
            <p style="margin:0 0 22px;color:#d6d1c8;font-size:16px;line-height:1.8;">
                Hello <strong>{customerName}</strong>,
            </p>

            <p style="margin:0 0 22px;color:#d6d1c8;font-size:16px;line-height:1.8;">
                This is a friendly reminder that your appointment is scheduled for tomorrow.
            </p>

            <table role="presentation" cellspacing="0" cellpadding="0" border="0"
                   style="width:100%;margin:28px 0;border-collapse:collapse;">
                <tr>
                    <td style="{GetTableLabelStyle()}">Date</td>
                    <td style="{GetTableValueStyle()}">{appointmentDate}</td>
                </tr>
                <tr>
                    <td style="{GetTableLabelStyle()}">Time</td>
                    <td style="{GetTableValueStyle()}">{appointmentStart} - {appointmentEnd}</td>
                </tr>
                <tr>
                    <td style="{GetTableLabelStyle()}">Service</td>
                    <td style="{GetTableValueStyle()}">{appointmentTitle}</td>
                </tr>
            </table>

            <p style="margin:0 0 22px;color:#d6d1c8;font-size:16px;line-height:1.8;">
                If you need to change your appointment, please contact us as soon as possible.
            </p>

            <p style="margin:0 0 22px;color:#d6d1c8;font-size:16px;line-height:1.8;">
                We look forward to welcoming you!
            </p>
            """;

        return BuildTemplate(
            title: "Appointment Reminder",
            content: content,
            buttonText: null,
            buttonUrl: null,
            footerMessage: "You are receiving this email because you have an appointment with Meet Amalfi Coast."
        );
    }

    public async Task<string> BuildWelcomeEmailAsync(string unsubscribeUrl)
    {
        string content = await ReadTextFileAsync("benvenuto.txt");

        return BuildTemplate(
            title: "Welcome to Meet Amalfi Coast",
            content: FormatTextContent(content),
            buttonText: "Discover our services",
            buttonUrl: "http://localhost:5087/Home/Services",
            footerMessage: "You are receiving this email because you subscribed to the Meet Amalfi Coast newsletter.",
            unsubscribeUrl: unsubscribeUrl
        );
    }

    public async Task<string> BuildReminderEmailAsync(string unsubscribeUrl)
    {
        string content = await ReadTextFileAsync("promemoria.txt");

        return BuildTemplate(
            title: "The Amalfi Coast is waiting for you",
            content: FormatTextContent(content),
            buttonText: "Plan your journey",
            buttonUrl: "http://localhost:5087/#contact",
            footerMessage: "You are receiving this email because you subscribed to the Meet Amalfi Coast newsletter.",
            unsubscribeUrl: unsubscribeUrl
        );
    }

    private async Task<string> ReadTextFileAsync(string fileName)
    {
        string filePath = Path.Combine(
            _environment.ContentRootPath,
            "EmailTemplates",
            fileName
        );

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Il file email '{fileName}' non è stato trovato.",
                filePath
            );
        }

        return await File.ReadAllTextAsync(filePath);
    }

    private static string FormatTextContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        string normalizedContent = content
            .Replace("\r\n", "\n")
            .Trim();

        string[] paragraphs = normalizedContent.Split(
            "\n\n",
            StringSplitOptions.RemoveEmptyEntries
        );

        StringBuilder html = new();

        foreach (string paragraph in paragraphs)
        {
            string encodedParagraph = WebUtility.HtmlEncode(paragraph.Trim())
                .Replace("\n", "<br>");

            html.Append($"""
                <p style="margin:0 0 22px;color:#d6d1c8;font-size:16px;line-height:1.8;">
                    {encodedParagraph}
                </p>
                """);
        }

        return html.ToString();
    }

    private static string BuildTemplate(
        string title,
        string content,
        string? buttonText,
        string? buttonUrl,
        string footerMessage,
        string? unsubscribeUrl = null)
    {
        string encodedTitle = WebUtility.HtmlEncode(title);
        string encodedFooterMessage = WebUtility.HtmlEncode(footerMessage);
        string buttonHtml = BuildButton(buttonText, buttonUrl);

        string unsubscribeHtml = string.IsNullOrWhiteSpace(unsubscribeUrl)
            ? string.Empty
            : $"""
                <br><br>
                <a href="{WebUtility.HtmlEncode(unsubscribeUrl)}"
                   style="color:#d6ad61;text-decoration:underline;text-underline-offset:3px;">
                    Unsubscribe from this newsletter
                </a>
                """;

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{encodedTitle}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#070707;font-family:Arial,Helvetica,sans-serif;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"
                       style="background-color:#070707;">
                    <tr>
                        <td align="center" style="padding:40px 16px;">
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"
                                   style="max-width:640px;background-color:#111111;border:1px solid #4b3d23;">
                                <tr>
                                    <td align="center"
                                        style="padding:38px 30px 24px;border-bottom:1px solid #4b3d23;">
                                        <div style="color:#d6ad61;font-family:Georgia,'Times New Roman',serif;font-size:27px;letter-spacing:2px;text-transform:uppercase;">
                                            Meet Amalfi Coast
                                        </div>
                                        <div style="width:45px;height:1px;margin:22px auto 0;background-color:#d6ad61;"></div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:42px 42px 28px;">
                                        <h1 style="margin:0 0 28px;color:#f0d28a;font-family:Georgia,'Times New Roman',serif;font-size:31px;font-weight:400;line-height:1.25;text-align:center;">
                                            {encodedTitle}
                                        </h1>

                                        <div style="color:#d6d1c8;font-size:16px;line-height:1.8;">
                                            {content}
                                        </div>

                                        {buttonHtml}
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:25px 30px;background-color:#090909;border-top:1px solid #4b3d23;color:#948f86;font-size:12px;line-height:1.7;text-align:center;">
                                        {encodedFooterMessage}
                                        {unsubscribeHtml}
                                        <br><br>
                                        © {DateTime.UtcNow.Year} Meet Amalfi Coast. All rights reserved.
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    private static string BuildButton(string? buttonText, string? buttonUrl)
    {
        if (string.IsNullOrWhiteSpace(buttonText) ||
            string.IsNullOrWhiteSpace(buttonUrl))
        {
            return string.Empty;
        }

        return $"""
            <div style="padding:18px 0 12px;text-align:center;">
                <a href="{WebUtility.HtmlEncode(buttonUrl)}"
                   style="display:inline-block;padding:15px 27px;background-color:#d6ad61;color:#17110a;font-size:13px;font-weight:bold;letter-spacing:1px;text-decoration:none;text-transform:uppercase;">
                    {WebUtility.HtmlEncode(buttonText)}
                </a>
            </div>
            """;
    }

    private static string GetTableLabelStyle()
    {
        return """
            padding:12px;
            color:#d6ad61;
            font-size:14px;
            border-bottom:1px solid #332b1c;
            vertical-align:top;
            """;
    }

    private static string GetTableValueStyle()
    {
        return """
            padding:12px;
            color:#f7f2e8;
            font-size:14px;
            border-bottom:1px solid #332b1c;
            vertical-align:top;
            """;
    }
}
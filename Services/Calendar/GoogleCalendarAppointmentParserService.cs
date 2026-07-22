using System.Text.RegularExpressions;
using MeetAmalfiCoast.Models;

public class GoogleCalendarAppointmentParserService
{
    private static readonly Regex EmailRegex = new(
        @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"\+?[0-9][0-9\s().-]{6,20}",
        RegexOptions.Compiled);

    public GoogleCalendarAppointmentInfo Parse(
        string? summary,
        string? description)
    {
        string customerName = summary?.Trim() ?? string.Empty;

        string text = description?.Trim() ?? string.Empty;

        string customerEmail = string.Empty;
        string customerPhone = string.Empty;

        Match emailMatch = EmailRegex.Match(text);

        if (emailMatch.Success)
        {
            customerEmail = emailMatch.Value.Trim();
            text = text.Replace(emailMatch.Value, "");
        }

        Match phoneMatch = PhoneRegex.Match(text);

        if (phoneMatch.Success)
        {
            customerPhone = phoneMatch.Value.Trim();
            text = text.Replace(phoneMatch.Value, "");
        }

        string notes = NormalizeNotes(text);

        return new GoogleCalendarAppointmentInfo
        {
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CustomerPhone = customerPhone,
            Notes = notes
        };
    }

    private static string NormalizeNotes(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string[] lines = text
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        return string.Join(Environment.NewLine, lines);
    }
}
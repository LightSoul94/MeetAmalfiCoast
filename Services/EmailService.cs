using MeetAmalfiCoast.Models;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

public class EmailService
{
    private readonly SmtpSettings _settings;

    public EmailService(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    // Invia un'email
    public async Task SendAsync(string toEmail, string subject, string body)
    {
        using var message = new MailMessage();

        message.From = new MailAddress(_settings.From);
        message.To.Add(toEmail);
        message.Subject = subject;
        message.Body = body;
        message.IsBodyHtml = true;

        using var client = new SmtpClient(_settings.Host, _settings.Port);

        client.EnableSsl = _settings.EnableSsl;
        client.Credentials = new NetworkCredential(
            _settings.Username,
            _settings.Password
        );

        await client.SendMailAsync(message);
    }

    // Invia un promemoria di appuntamento via email
    public async Task SendAppointmentReminderAsync(PlanningAppointmentsModel appointment)
    {
        string subject = "Appointment Reminder - Meet Amalfi Coast";

        string body = $@"
            <!DOCTYPE html>
            <html>
                <body style='font-family:Arial,sans-serif;background:#f8f8f8;padding:30px;'>

                    <div style='max-width:600px;margin:auto;background:white;border-radius:10px;padding:30px;border:1px solid #ddd;'>

                        <h2 style='color:#d6ad61;'>Meet Amalfi Coast</h2>

                        <p>Hello <strong>{appointment.Customer}</strong>,</p>

                        <p>This is a friendly reminder that your appointment is scheduled for tomorrow.</p>

                        <table style='border-collapse:collapse;margin-top:20px;'>
                            <tr>
                                <td><strong>Date:</strong></td>
                                <td>{appointment.IsoDate}</td>
                            </tr>

                            <tr>
                                <td><strong>Time:</strong></td>
                                <td>{appointment.Start} - {appointment.End}</td>
                            </tr>

                            <tr>
                                <td><strong>Service:</strong></td>
                                <td>{appointment.Title}</td>
                            </tr>
                        </table>

                        <p style='margin-top:30px;'>
                        If you need to change your appointment, please contact us as soon as possible.
                        </p>

                        <p>
                        We look forward to welcoming you!
                        </p>

                        <hr>

                        <p style='font-size:12px;color:#777'>
                        Meet Amalfi Coast
                        </p>

                    </div>

                </body>
            </html>";

        await SendAsync(
            appointment.CustomerEmail,
            subject,
            body);
    }
}
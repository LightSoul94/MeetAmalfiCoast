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
    public async Task SendAppointmentReminderAsync(MeetAmalfiCoast.Models.PlanningAppointmentModel appointment)
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

    // Invia una notifica di nuova prenotazione via email
    public async Task SendNewBookingNotificationAsync(MeetAmalfiCoast.Models.PlanningAppointmentModel appointment)
    {
        string subject = "Nuova prenotazione ricevuta";

        DateTime date = DateTime.Parse(appointment.IsoDate);
        string formattedDate = date.ToString("dd/MM/yyyy");

        string body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
        </head>
        <body style='margin:0;padding:30px;background:#f5f5f5;font-family:Arial,Helvetica,sans-serif;'>

            <div style='max-width:650px;margin:auto;background:#ffffff;border-radius:12px;border:1px solid #e5e5e5;overflow:hidden;'>

                <div style='background:#111111;padding:25px;text-align:center;'>
                    <h2 style='margin:0;color:#d6ad61;font-size:28px;'>
                        📅 Nuova prenotazione ricevuta
                    </h2>
                </div>

                <div style='padding:30px;'>

                    <p style='margin-top:0;font-size:16px;color:#333;'>
                        È stata effettuata una nuova prenotazione tramite il sito web.
                    </p>

                    <div style='background:#fafafa;border:1px solid #e3e3e3;border-radius:8px;padding:20px;margin:25px 0;'>

                        <table style='width:100%;border-collapse:collapse;font-size:15px;'>

                            <tr>
                                <td style='padding:8px 0;width:140px;'><strong>👤 Cliente</strong></td>
                                <td>{appointment.Customer}</td>
                            </tr>

                            <tr>
                                <td style='padding:8px 0;'><strong>📧 Email</strong></td>
                                <td>{appointment.CustomerEmail}</td>
                            </tr>

                            <tr>
                                <td style='padding:8px 0;'><strong>📅 Data</strong></td>
                                <td>{formattedDate}</td>
                            </tr>

                            <tr>
                                <td style='padding:8px 0;'><strong>🕒 Orario</strong></td>
                                <td>{appointment.Start} - {appointment.End}</td>
                            </tr>

                            <tr>
                                <td style='padding:8px 0;'><strong>🚗 Servizio</strong></td>
                                <td>{appointment.Title}</td>
                            </tr>

                        </table>

                    </div>

                    <p style='margin-bottom:0;color:#555;font-size:15px;'>
                        L'appuntamento è disponibile nel planning di <strong>Meet Amalfi Coast</strong> e su Google Calendar per la consultazione e la gestione.
                    </p>

                </div>

                <div style='background:#f8f8f8;border-top:1px solid #e5e5e5;padding:18px;text-align:center;font-size:12px;color:#777;'>
                    Questa è una notifica automatica generata dal sito web di Meet Amalfi Coast.
                </div>

            </div>

        </body>
        </html>";

        await SendAsync(
            _settings.To,
            subject,
            body);
    }
}
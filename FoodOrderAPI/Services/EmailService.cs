using System.Net;
using System.Net.Mail;

namespace FoodOrderAPI.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendOrderConfirmationAsync(string toEmail, string customerName, int orderId, decimal totalAmount, string deliveryAddress)
    {
        var host = _config["Smtp:Host"];
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var username = _config["Smtp:Username"];
        var password = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:FromEmail"] ?? "noreply@foodorder.com";
        var fromName = _config["Smtp:FromName"] ?? "FoodOrder Restaurant";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("SMTP credentials not configured. Simulated Order #{OrderId} confirmation email to {Email} (Total: ₹{Total}).",
                orderId, toEmail, totalAmount);
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = $"Order Confirmation - Order #{orderId}",
                IsBodyHtml = true,
                Body = $@"
                    <h2>Thank you for your order, {WebUtility.HtmlEncode(customerName)}!</h2>
                    <p>Your order <strong>#{orderId}</strong> has been received and is being prepared.</p>
                    <p><strong>Total Amount:</strong> ₹{totalAmount:F2}</p>
                    <p><strong>Delivery Address:</strong> {WebUtility.HtmlEncode(deliveryAddress)}</p>
                    <hr/>
                    <p>You can download your PDF invoice anytime from your Orders dashboard.</p>"
            };
            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send order confirmation email for Order #{OrderId}.", orderId);
        }
    }
}
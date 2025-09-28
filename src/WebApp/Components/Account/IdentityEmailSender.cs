using Blink.WebApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace Blink.WebApp.Components.Account;

internal sealed class IdentityEmailSender : IEmailSender<BlinkUser>
{
    private readonly IEmailSender _emailSender;

    public IdentityEmailSender(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public Task SendConfirmationLinkAsync(BlinkUser user, string email, string confirmationLink) =>
        _emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(BlinkUser user, string email, string resetLink) =>
        _emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public Task SendPasswordResetCodeAsync(BlinkUser user, string email, string resetCode) =>
        _emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
}

internal sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;

    public SmtpEmailSender(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var smtp = _options.CreateSmtpClient();

        var mailMessage = new MailMessage(_options.From, email)
        {
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };

        await smtp.SendMailAsync(mailMessage);
    }
}

public sealed record EmailOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string From { get; set; } = "";
    public bool EnableSsl { get; set; }

    public SmtpClient CreateSmtpClient() => new()
    {
        Host = Host,
        Port = Port,
        EnableSsl = EnableSsl
    };
}
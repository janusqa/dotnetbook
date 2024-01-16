using System.Net;
using System.Net.Mail;
using FluentEmail.Core;
using FluentEmail.MailKitSmtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Utility
{
    // we have mocked this class until we get to its implementation as it
    // will cause an error when seeding roles to the DB

    public class SmtpEmailSender : IEmailSender
    {
        private readonly string _server;
        private readonly int _port;
        private readonly string _uid;
        private readonly string _pid;



        public SmtpEmailSender(IConfiguration _config)
        {
            _server = _config.GetValue<string>("SmtpServer:Server") ?? "";
            _port = _config.GetValue<int>("SmtpServer:Port");
            _uid = _config.GetValue<string>("Smtp:uid") ?? "";
            _pid = _config.GetValue<string>("Smtp:pid") ?? "";
        }

        public Task SendEmailAsync(string email, string subject, string message)
        {
            // using Fluent.Smtp nuget package.
            var sender = new MailKitSender(new SmtpClientOptions
            {
                Server = _server,
                Port = _port,
                UseSsl = true,
                User = _uid,
                Password = _pid
            });

            Email.DefaultSender = sender;

            return Email.From(_uid)
                .To(email)
                .Subject(subject)
                .Body(message)
                .SendAsync();
            // return Task.CompletedTask; // use this to mock a service.
        }
    }
}
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Bookstore.Utility
{
    // we have mocked this class until we get to its implementation as it
    // will cause an error when seeding roles to the DB
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // TODO: implement logic to send an email
            return Task.CompletedTask;
        }
    }
}
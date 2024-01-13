using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Bookstore.Utility
{
    public static class StripeSettings
    {
        private static readonly IConfiguration Configuration;

        static StripeSettings()
        {
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<IStartup>();
            Configuration = builder.Build();
        }

        public static string? SecretKey => Configuration["StripeSecretKey"];
        public static string? PublishableKey => Configuration["StripePublishableKey"];
    }
}
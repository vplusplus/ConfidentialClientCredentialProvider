
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Identity.Extensions
{
    public static class ConfidentialClientExtensions
    {
        public static IServiceCollection AddConfidentialClientCredentialProvider(this IServiceCollection services)
        {
            return null == services 
                ? throw new ArgumentNullException(nameof(services))
                : services.AddSingleton<IConfidentialClientCredentialProvider, ConfidentialClientCredentialProvider>();
        }
    }
}

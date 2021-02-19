
using System;
using Azure.Identity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests
{
    class My
    {
        internal static IConfiguration Config => LazyConfiguration.Value;

        static readonly Lazy<IConfiguration> LazyConfiguration = new Lazy<IConfiguration>(BuildMyConfigurationOnce);
        
        static IConfiguration BuildMyConfigurationOnce()
        {
            try
            {
                return new ConfigurationBuilder()
                    .AddIniFile("AppSettings.ini", optional: true, reloadOnChange: true)
                    .Build();
            }
            catch (Exception err)
            {
                throw new Exception("Error preparing IConfiguration", err);
            }
        }

        internal static IServiceProvider Services => LazyServiceCollection.Value;

        static readonly Lazy<IServiceProvider> LazyServiceCollection = new Lazy<IServiceProvider>(BuildMyServicesOnce);

        static IServiceProvider BuildMyServicesOnce()
        {
            try
            {
                return new ServiceCollection()
                    .AddSingleton<IConfiguration>(My.Config)
                    .AddConfidentialClientCredentialProvider()
                    .BuildServiceProvider()
                    ;
            }
            catch (Exception err)
            {
                throw new Exception("Error preparing ServiceCollection", err);
            }
        }
    }
}

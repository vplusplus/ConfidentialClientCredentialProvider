
using System;
using Microsoft.Extensions.Configuration;

namespace Azure.Identity.Extensions
{
    /// <summary>
    /// An immutable data structure that represents Confidential Client Credential related options.
    /// </summary>
    internal sealed class ConfidentialClientCredentialOptions
    {
        public string Name { get; private set; }
        public bool UseManagedIdentity { get; private set; }
        public string AuthorityHost { get; private set; }
        public string TenantId { get; private set; }
        public string ClientId { get; private set; }
        public string ClientSecret { get; private set; }
        public string ClientCertificate { get; private set; }

        /// <summary>
        /// A thumbprint of this instance, repeatable only during the process lifetime.
        /// </summary>
        public override int GetHashCode() => HashCode.Combine(
            Name,
            UseManagedIdentity,
            AuthorityHost,
            TenantId,
            ClientId,
            ClientSecret,
            ClientCertificate
         );

        internal static bool IsDefined(IConfiguration configuration, string configSectionName)
        {
            if (null == configuration) throw new ArgumentNullException(nameof(configuration));
            if (null == configSectionName) throw new ArgumentNullException(nameof(configSectionName));

            // Grab the config section, which may not exist
            var configSection = configuration.GetSection(configSectionName);

            // GetSection() always returns a valid object. 
            // Use Exists() to verify if defined.
            return null != configSection && configSection.Exists();
        }

        internal static ConfidentialClientCredentialOptions ReadFromConfig(IConfiguration configuration, string configSectionName)
        {
            if (null == configuration) throw new ArgumentNullException(nameof(configuration));
            if (null == configSectionName) throw new ArgumentNullException(nameof(configSectionName));

            try
            {
                // Grab the config section, which may not exist
                var configSection = configuration.GetSection(configSectionName);

                // Missing config section DOESN'T return NULL; Rather all properties will be NULL.
                // configSection.Exists() adds 2 microSec, but only reliable way to check.
                var exists = null != configSection && configSection.Exists();
                if (!exists) throw new Exception($"Config section not defined. SectionName: [{configSectionName}]");

                // Avoiding IConfigSection.Get<T>(), which is slower and requires additional dependencies.
                return new ConfidentialClientCredentialOptions()
                {
                    Name = configSectionName,
                    UseManagedIdentity = ToBoolean(configSection[nameof(UseManagedIdentity)]),
                    AuthorityHost = NullIfEmpty(configSection[nameof(AuthorityHost)]),
                    TenantId = NullIfEmpty(configSection[nameof(TenantId)]),
                    ClientId = NullIfEmpty(configSection[nameof(ClientId)]),
                    ClientSecret = NullIfEmpty(configSection[nameof(ClientSecret)]),
                    ClientCertificate = NullIfEmpty(configSection[nameof(ClientCertificate)])
                };
            }
            catch (Exception err)
            {
                var errMsg = $"Error loading Confidential client credentials from the config section: [{configSectionName}]";
                throw new Exception(errMsg, err);
            }

            static string NullIfEmpty(string something) => string.IsNullOrWhiteSpace(something)
                ? null
                : something.Trim();

            static bool ToBoolean(string something) => string.IsNullOrWhiteSpace(something)
                ? false
                : Convert.ToBoolean(something);
        }
    }
}

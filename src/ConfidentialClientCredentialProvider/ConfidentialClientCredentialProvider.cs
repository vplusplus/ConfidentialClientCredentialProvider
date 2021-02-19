
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using Microsoft.Extensions.Configuration;

namespace Azure.Identity.Extensions
{
    /// <summary>
    /// Can cache and serve Confidential client token credentials. 
    /// </summary>
    internal sealed class ConfidentialClientCredentialProvider : IConfidentialClientCredentialProvider
    {
        // The runtime Configuration.
        readonly IConfiguration Configuration = null;

        // A thread-safe cache of TokenCredentials.
        readonly ConcurrentDictionary<string, TokenCredential> CredentialCache = null;

        public ConfidentialClientCredentialProvider(IConfiguration configuration)
        {
            this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.CredentialCache = new ConcurrentDictionary<string, TokenCredential>();
        }

        bool IConfidentialClientCredentialProvider.IsDefined(string credentialName)
        {
            if (null == credentialName) throw new ArgumentNullException(nameof(credentialName));

            // This provider maps credentialName to configSectionName. Check if section exists.
            return ConfidentialClientCredentialOptions.IsDefined(this.Configuration, credentialName);
        }

        // Performance: 1,000,000 iterations Avg: 2.755 µSec
        TokenCredential IConfidentialClientCredentialProvider.GetTokenCredential(string credentialName)
        {
            if (null == credentialName) throw new ArgumentNullException(nameof(credentialName));

            // Read the configuration, which could have changed.
            // Reading config costs ~2 µSec, but helps avoid restarting app if config changes.
            // Using IOptionMonitor is an option, not warranted for this use-case.
            var config = ConfidentialClientCredentialOptions.ReadFromConfig(this.Configuration, credentialName);

            // Compose an unique cache key that represents the name and values. (Cost: ~150 nanoSec)
            var cacheKey = $"{credentialName}#{config.GetHashCode()}";

            // Create once, cache and serve.
            return CredentialCache.GetOrAdd(cacheKey, (x) => CreateTokenCredentialOnce(config));
        }

        static TokenCredential CreateTokenCredentialOnce(ConfidentialClientCredentialOptions config)
        {
            if (null == config) throw new ArgumentNullException(nameof(config));

            try
            {
                return
                    config.UseManagedIdentity ? CreateManagedIdentityCredential(config) :
                    !string.IsNullOrWhiteSpace(config.ClientCertificate) ? CreateClientCertificateCredential(config) :
                    !string.IsNullOrWhiteSpace(config.ClientSecret) ? CreateClientSecretCredentials(config) :
                    throw new Exception($"Invalid config section. Expecting either 'UseManagedIdentity' or 'ClientCertificate' or 'ClientSecret'.");
            }
            catch (Exception err)
            {
                var errMsg = $"Error creating TokenCredentials using config section: '{config.Name}'";
                throw new Exception(errMsg, err);
            }
        }

        static TokenCredential CreateManagedIdentityCredential(ConfidentialClientCredentialOptions config)
        {
            if (null == config) throw new ArgumentNullException(nameof(config));

            var authorityHostUri = GetAuthorityHostUri(config.AuthorityHost);
            var options = new TokenCredentialOptions() { AuthorityHost = authorityHostUri };

            // ClientId can be null; 
            // ClientId is required only for user-assigned-managed-identity
            return new ManagedIdentityCredential(config.ClientId, options);
        }

        static TokenCredential CreateClientCertificateCredential(ConfidentialClientCredentialOptions config)
        {
            if (null == config) throw new ArgumentNullException(nameof(config));
            if (null == config.TenantId) throw new Exception("TenantId not specified.");
            if (null == config.ClientId) throw new Exception("ClientId not specified.");
            if (null == config.ClientCertificate) throw new Exception("ClientCertificate (thumbprint) not specified.");

            var clientCertificate = LoadX509Certificate(config.ClientCertificate, StoreName.My, StoreLocation.CurrentUser);
            var authorityHostUri = GetAuthorityHostUri(config.AuthorityHost);
            var options = new TokenCredentialOptions() { AuthorityHost = authorityHostUri };

            return new ClientCertificateCredential(config.TenantId, config.ClientId, clientCertificate, options);
        }

        static TokenCredential CreateClientSecretCredentials(ConfidentialClientCredentialOptions config)
        {
            if (null == config) throw new ArgumentNullException(nameof(config));
            if (null == config.TenantId) throw new Exception("TenantId not specified.");
            if (null == config.ClientId) throw new Exception("ClientId not specified.");
            if (null == config.ClientSecret) throw new Exception("ClientSecret not specified.");

            var authorityHostUri = GetAuthorityHostUri(config.AuthorityHost);
            var options = new TokenCredentialOptions() { AuthorityHost = authorityHostUri };

            return new ClientSecretCredential(config.TenantId, config.ClientId, config.ClientSecret, options);
        }

        static Uri GetAuthorityHostUri(string authorityHostHint)
        {
            const StringComparison CI = StringComparison.OrdinalIgnoreCase;

            var hint = authorityHostHint?.Trim();

            return string.IsNullOrWhiteSpace(hint) ? AzureAuthorityHosts.AzurePublicCloud
                : hint.Equals(nameof(AzureAuthorityHosts.AzurePublicCloud), CI) ? AzureAuthorityHosts.AzurePublicCloud
                : hint.Equals(nameof(AzureAuthorityHosts.AzureChina), CI) ? AzureAuthorityHosts.AzureChina
                : hint.Equals(nameof(AzureAuthorityHosts.AzureGermany), CI) ? AzureAuthorityHosts.AzureGermany
                : hint.Equals(nameof(AzureAuthorityHosts.AzureGovernment), CI) ? AzureAuthorityHosts.AzureGovernment
                : hint.StartsWith("https://", CI) ? new Uri(hint)
                : throw new Exception($"Invalid AuthorityHost: '{authorityHostHint}'");
        }

        static X509Certificate2 LoadX509Certificate(string certificateThumbprint, StoreName storeName, StoreLocation storeLocation)
        {
            if (null == certificateThumbprint) throw new ArgumentNullException(nameof(certificateThumbprint));

            using (var certificateStore = new X509Store(storeName, storeLocation))
            {
                // Open an existing store.
                certificateStore.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                // Look for the certificate by thumbPrint.
                // Not using X509Certificate2.Find(X509FindType.FindByThumbprint), to support case-insensitive search.
                // Given the search is by thumbPrint and not by subject name, the search doesn't use start/end-date filters.
                // Expecting Zero or One cert.
                var certs = certificateStore
                    .Certificates
                    .OfType<X509Certificate2>()
                    .Where(x => certificateThumbprint.Equals(x.Thumbprint, StringComparison.OrdinalIgnoreCase))
                    .Take(2)
                    .ToArray()
                    ;

                return
                    1 == certs.Length ? certs[0] :
                    0 == certs.Length ? throw new Exception($"X509Certificate not found: {storeLocation}/{storeName}/{certificateThumbprint}") :
                    throw new Exception($"Found more than ONE X509Certificate: {storeLocation}/{storeName}/{certificateThumbprint}");
            }
        }
    }
}

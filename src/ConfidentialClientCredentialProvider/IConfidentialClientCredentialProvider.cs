
using Azure.Core;

namespace Azure.Identity.Extensions
{
    /// <summary>
    /// Provides TokenCredentials for a Confidential Client Application.
    /// </summary>
    public interface IConfidentialClientCredentialProvider
    {
        /// <summary>
        /// Indicates if credentials are configured for the given name.
        /// </summary>
        bool IsDefined(string credentialName);

        /// <summary>
        /// Returns TokenCredential associated with the given name.
        /// </summary>
        TokenCredential GetTokenCredential(string credentialName);
    }
}

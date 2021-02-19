# ConfidentialClientCredentialProvider

The `ConfidentialClientCredentialProvider` serves cached versions of TokenCredential, targeted for confidential client applications. The TokenCredential manages cache/auto-refresh of AccessTokens. 

* Supports System-assigned Managed identities, User-assigned managed identities, ClientCertificate and ClientSecret.
* Targeted for deamons; no interactive login options.
* Supports multiple named client credentials.
* No need for app restart on config changes.
* AuthorityHost can be represented using names as defined in `Azure.Identity.AzureAuthorityHosts` or a specific URI.
* Average performance under 2.755 microSec (tested over 1 million iterations).
* AccessToken cache/auto-refresh endurance tested for 24 hours.

Consider `Azure.Identity.DefaultAzureCredential` for a more general purpose use.

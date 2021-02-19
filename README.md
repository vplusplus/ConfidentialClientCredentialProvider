# ConfidentialClientCredentialProvider

TokenCredential manages cache/refresh of Access Tokens. The `ConfidentialClientCredentialProvider` serves cached versions of TokenCredential, targeted for confidential client applications. Consider `Azure.Identity.DefaultAzureCredential` for a more general purpose use. 

* Supports Managed identities (system assigned or user assigned), ClientCertificate and ClientSecret.
* Targeted for deamons; no interactive login options.
* Supports multiple named client credentials.
* No need for app restart on config changes.
* AuthorityHost can be represented using names as defined in `Azure.Identity.AzureAuthorityHosts` or a specific URI.
* Average performance under 2.755 microSec (tested over 1 million iterations).
* AccessToken cache/auto-refresh endurance tested for 24 hours.



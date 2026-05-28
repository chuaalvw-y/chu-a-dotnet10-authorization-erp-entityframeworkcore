// Copyright (c) 2026 Alvin Wilsen Chan Chua
// GitHub: chuaalvw-y
// Licensed under the Alvin Wilsen Chan Chua Proprietary Use-Only License.
// See LICENSE.txt in the project root for full license information.

using System.Security.Claims;

namespace ChuA.Authorization.Erp;

/// <summary>
/// Configures ERP authorization identity resolution without coupling the authorization
/// layer to a specific authentication provider SDK.
/// </summary>
public sealed class ChuAErpAuthorizationOptions
{
    /// <summary>
    /// Gets or sets the claim type used when a trusted host already resolved the ERP user id.
    /// </summary>
    public string ErpUserIdClaimType { get; set; } = "erp_user_id";

    /// <summary>
    /// Gets or sets the external identity provider name stored in the ERP
    /// <c>ExternalLogins.Provider</c> column.
    /// </summary>
    public string ExternalLoginProvider { get; set; } = "Auth0";

    /// <summary>
    /// Gets or sets the primary external subject claim type. The default matches OIDC/JWT
    /// <c>sub</c>.
    /// </summary>
    public string SubjectClaimType { get; set; } = "sub";

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="ClaimTypes.NameIdentifier"/> should
    /// be used when <see cref="SubjectClaimType"/> is absent.
    /// </summary>
    public bool EnableNameIdentifierFallback { get; set; } = true;
}

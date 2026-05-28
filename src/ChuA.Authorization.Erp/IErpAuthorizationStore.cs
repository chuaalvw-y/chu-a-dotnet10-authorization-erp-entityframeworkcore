// Copyright (c) 2026 Alvin Wilsen Chan Chua
// GitHub: chuaalvw-y
// Licensed under the Alvin Wilsen Chan Chua Proprietary Use-Only License.
// See LICENSE.txt in the project root for full license information.

using System.Security.Claims;

namespace ChuA.Authorization.Erp;

/// <summary>
/// Provides persistence operations used by ChuA ERP authorization to resolve users,
/// effective roles, and effective permissions.
/// </summary>
public interface IErpAuthorizationStore
{
    /// <summary>
    /// Resolves the ERP user id represented by an authenticated principal.
    /// </summary>
    /// <param name="principal">The authenticated principal supplied by the host application.</param>
    /// <param name="cancellationToken">A token that can cancel the lookup.</param>
    /// <returns>The ERP user id, or <see langword="null"/> when the user is not an ERP member.</returns>
    Task<Guid?> ResolveUserIdAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active global role names assigned to an ERP user.
    /// </summary>
    /// <param name="userId">The ERP user id.</param>
    /// <param name="context">Request-level authorization context.</param>
    /// <param name="cancellationToken">A token that can cancel the query.</param>
    /// <returns>Distinct active role names.</returns>
    Task<IReadOnlyCollection<string>> GetRolesAsync(
        Guid userId,
        ErpAuthorizationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active global permission codes granted to an ERP user through active roles.
    /// </summary>
    /// <param name="userId">The ERP user id.</param>
    /// <param name="context">Request-level authorization context.</param>
    /// <param name="cancellationToken">A token that can cancel the query.</param>
    /// <returns>Distinct permission codes. Codes are returned exactly as stored.</returns>
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        ErpAuthorizationContext context,
        CancellationToken cancellationToken = default);
}

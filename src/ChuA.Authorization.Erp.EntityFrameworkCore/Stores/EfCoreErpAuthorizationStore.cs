// Copyright (c) 2026 Alvin Wilsen Chan Chua
// GitHub: chuaalvw-y
// Licensed under the Alvin Wilsen Chan Chua Proprietary Use-Only License.
// See LICENSE.txt in the project root for full license information.

using System.Security.Claims;
using ChuA.Authorization.Erp;
using ChuA.ERP.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChuA.Authorization.Erp.EntityFrameworkCore.Stores;

/// <summary>
/// EF Core implementation of <see cref="IErpAuthorizationStore"/> for the existing
/// ChuA ERP database schema.
/// </summary>
public sealed class EfCoreErpAuthorizationStore : IErpAuthorizationStore
{
    private readonly AppDbContext _dbContext;
    private readonly ChuAErpAuthorizationOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreErpAuthorizationStore"/> class.
    /// </summary>
    /// <param name="dbContext">The ERP application DbContext registered by the host.</param>
    /// <param name="options">Authorization options supplied by the host.</param>
    /// <param name="timeProvider">The clock used for expiry checks.</param>
    public EfCoreErpAuthorizationStore(
        AppDbContext dbContext,
        IOptions<ChuAErpAuthorizationOptions> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task<Guid?> ResolveUserIdAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var erpUserIdValue = principal.FindFirstValue(_options.ErpUserIdClaimType);
        if (Guid.TryParse(erpUserIdValue, out var erpUserId) && erpUserId != Guid.Empty)
        {
            return erpUserId;
        }

        var subject = principal.FindFirstValue(_options.SubjectClaimType);
        if (string.IsNullOrWhiteSpace(subject) && _options.EnableNameIdentifierFallback)
        {
            subject = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(_options.ExternalLoginProvider))
        {
            return null;
        }

        return await _dbContext.ExternalLogins
            .AsNoTracking()
            .Where(login => login.Provider == _options.ExternalLoginProvider && login.Subject == subject)
            .Select(login => (Guid?)login.UserId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetRolesAsync(
        Guid userId,
        ErpAuthorizationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (userId == Guid.Empty)
        {
            return [];
        }

        var now = _timeProvider.GetUtcNow();

        return await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId
                && userRole.IsActive
                && EF.Property<bool>(role, "IsActive")
                && (userRole.ExpiresUtc == null || userRole.ExpiresUtc > now)
            select role.Name)
            .Distinct()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(
        Guid userId,
        ErpAuthorizationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (userId == Guid.Empty)
        {
            return [];
        }

        var now = _timeProvider.GetUtcNow();

        return await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join rolePermission in _dbContext.RolePermissions.AsNoTracking() on role.Id equals rolePermission.RoleId
            join permission in _dbContext.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
            where userRole.UserId == userId
                && userRole.IsActive
                && EF.Property<bool>(role, "IsActive")
                && rolePermission.IsActive
                && permission.IsActive
                && (userRole.ExpiresUtc == null || userRole.ExpiresUtc > now)
            select permission.Code)
            .Distinct()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

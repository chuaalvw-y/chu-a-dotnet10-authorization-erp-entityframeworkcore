// Copyright (c) 2026 Alvin Wilsen Chan Chua
// GitHub: chuaalvw-y
// Licensed under the Alvin Wilsen Chan Chua Proprietary Use-Only License.
// See LICENSE.txt in the project root for full license information.

using System.Security.Claims;
using ChuA.Authorization.Erp;
using ChuA.Authorization.Erp.EntityFrameworkCore.Extensions;
using ChuA.Authorization.Erp.EntityFrameworkCore.Stores;
using ChuA.ERP.Database;
using ChuA.ERP.Database.Entities.Identity;
using ChuA.ERP.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using DomainRole = ChuA.ERP.Domain.Entities.Identity.Role;

namespace ChuA.Authorization.Erp.EntityFrameworkCore.Tests;

public sealed class EfCoreErpAuthorizationStoreTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 5, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ResolveUserIdAsync_ReturnsErpUserIdClaim_WhenPresent()
    {
        await using var dbContext = CreateDbContext();
        var store = CreateStore(dbContext);
        var userId = Guid.NewGuid();
        var principal = Principal(new("erp_user_id", userId.ToString()), new("sub", "auth0|ignored"));

        var resolved = await store.ResolveUserIdAsync(principal);

        Assert.Equal(userId, resolved);
    }

    [Fact]
    public async Task ResolveUserIdAsync_FallsBackToExternalLoginByProviderAndSubject()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        AddExternalLogin(dbContext, userId, "Auth0", "auth0|123");
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var resolved = await store.ResolveUserIdAsync(Principal(new Claim("sub", "auth0|123")));

        Assert.Equal(userId, resolved);
    }

    [Fact]
    public async Task ResolveUserIdAsync_UsesNameIdentifierFallback_WhenSubjectClaimIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        AddExternalLogin(dbContext, userId, "Auth0", "name-id-123");
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var resolved = await store.ResolveUserIdAsync(Principal(new Claim(ClaimTypes.NameIdentifier, "name-id-123")));

        Assert.Equal(userId, resolved);
    }

    [Fact]
    public async Task ResolveUserIdAsync_ReturnsNull_WhenExternalLoginIsMissing()
    {
        await using var dbContext = CreateDbContext();
        var store = CreateStore(dbContext);

        var resolved = await store.ResolveUserIdAsync(Principal(new Claim("sub", "auth0|missing")));

        Assert.Null(resolved);
    }

    [Fact]
    public async Task GetRolesAsync_ReturnsActiveRoleNames_WithoutCompanyFiltering()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var role = AddRole(dbContext, "FinanceUser");
        AddUserRole(dbContext, userId, role.Id, isActive: true, expiresUtc: FixedNow.AddDays(1));
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var roles = await store.GetRolesAsync(userId, new ErpAuthorizationContext(Guid.NewGuid()));

        Assert.Equal(["FinanceUser"], roles);
    }

    [Fact]
    public async Task GetRolesAsync_ExcludesInactiveUserRoles()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var role = AddRole(dbContext, "InactiveAssignment");
        AddUserRole(dbContext, userId, role.Id, isActive: false, expiresUtc: null);
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var roles = await store.GetRolesAsync(userId, new ErpAuthorizationContext());

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetRolesAsync_ExcludesInactiveRoles()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var role = AddRole(dbContext, "InactiveRole", isActive: false);
        AddUserRole(dbContext, userId, role.Id, isActive: true, expiresUtc: null);
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var roles = await store.GetRolesAsync(userId, new ErpAuthorizationContext());

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetRolesAsync_ExcludesExpiredUserRoles()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var role = AddRole(dbContext, "Expired");
        AddUserRole(dbContext, userId, role.Id, isActive: true, expiresUtc: FixedNow.AddSeconds(-1));
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var roles = await store.GetRolesAsync(userId, new ErpAuthorizationContext());

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetPermissionsAsync_ReturnsActivePermissionCodes_WithoutCompanyFiltering()
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        AddGrant(dbContext, userId, "Approver", "vendor:view");
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var permissions = await store.GetPermissionsAsync(userId, new ErpAuthorizationContext(Guid.NewGuid()));

        Assert.Equal(["vendor:view"], permissions);
    }

    [Theory]
    [InlineData(false, true, true, true, null)]
    [InlineData(true, false, true, true, null)]
    [InlineData(true, true, false, true, null)]
    [InlineData(true, true, true, false, null)]
    [InlineData(true, true, true, true, -1)]
    public async Task GetPermissionsAsync_ExcludesInactiveOrExpiredGrantParts(
        bool userRoleActive,
        bool roleActive,
        bool rolePermissionActive,
        bool permissionActive,
        int? expiresOffsetSeconds)
    {
        await using var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        DateTimeOffset? expiresUtc = expiresOffsetSeconds is null
            ? null
            : FixedNow.AddSeconds(expiresOffsetSeconds.Value);
        AddGrant(
            dbContext,
            userId,
            "Conditional",
            "vendor:edit",
            userRoleActive,
            roleActive,
            rolePermissionActive,
            permissionActive,
            expiresUtc);
        await dbContext.SaveChangesAsync();
        var store = CreateStore(dbContext);

        var permissions = await store.GetPermissionsAsync(userId, new ErpAuthorizationContext());

        Assert.Empty(permissions);
    }

    [Fact]
    public void AddChuAErpAuthorizationEntityFrameworkCore_RegistersStore_WithoutRegisteringAppDbContext()
    {
        var services = new ServiceCollection();

        services.AddChuAErpAuthorizationEntityFrameworkCore();

        using var provider = services.BuildServiceProvider();
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IErpAuthorizationStore));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(AppDbContext));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static EfCoreErpAuthorizationStore CreateStore(AppDbContext dbContext)
    {
        return new EfCoreErpAuthorizationStore(
            dbContext,
            Options.Create(new ChuAErpAuthorizationOptions()),
            new FixedTimeProvider(FixedNow));
    }

    private static ClaimsPrincipal Principal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static DomainRole AddRole(AppDbContext dbContext, string name, bool isActive = true)
    {
        var role = DomainRole.Create(name);
        dbContext.Roles.Add(role);
        SetRequiredShadowAuditProperties(dbContext, role);
        dbContext.Entry(role).Property("IsActive").CurrentValue = isActive;
        return role;
    }

    private static ExternalLogin AddExternalLogin(
        AppDbContext dbContext,
        Guid userId,
        string provider,
        string subject)
    {
        var externalLogin = ExternalLogin.Create(userId, provider, subject);
        dbContext.ExternalLogins.Add(externalLogin);
        SetRequiredShadowAuditProperties(dbContext, externalLogin);
        return externalLogin;
    }

    private static void SetRequiredShadowAuditProperties<TEntity>(AppDbContext dbContext, TEntity entity)
        where TEntity : class
    {
        var entry = dbContext.Entry(entity);
        entry.Property("CreatedBy").CurrentValue = "test";
        entry.Property("CreatedDate").CurrentValue = FixedNow;
        entry.Property("IsActive").CurrentValue = true;
        entry.Property("RowVersion").CurrentValue = new byte[] { 1 };
    }

    private static UserRole AddUserRole(
        AppDbContext dbContext,
        Guid userId,
        Guid roleId,
        bool isActive,
        DateTimeOffset? expiresUtc)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            IsActive = isActive,
            ExpiresUtc = expiresUtc,
        };

        dbContext.UserRoles.Add(userRole);
        return userRole;
    }

    private static void AddGrant(
        AppDbContext dbContext,
        Guid userId,
        string roleName,
        string permissionCode,
        bool userRoleActive = true,
        bool roleActive = true,
        bool rolePermissionActive = true,
        bool permissionActive = true,
        DateTimeOffset? expiresUtc = null)
    {
        var role = AddRole(dbContext, roleName, roleActive);
        var permission = new Permission
        {
            Code = permissionCode,
            Name = permissionCode,
            Module = "Testing",
            IsActive = permissionActive,
        };
        var rolePermission = new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            IsActive = rolePermissionActive,
        };

        AddUserRole(dbContext, userId, role.Id, userRoleActive, expiresUtc ?? FixedNow.AddDays(1));
        dbContext.Permissions.Add(permission);
        dbContext.RolePermissions.Add(rolePermission);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

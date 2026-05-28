# ChuA.Authorization.Erp.EntityFrameworkCore

EF Core persistence for `ChuA.Authorization.Erp` against the existing ChuA ERP database.

This package provides the schema-specific implementation of `IErpAuthorizationStore` using
`AppDbContext` from `ChuA.ERP.Database`. It is designed for ASP.NET Core Web API, MVC,
Razor Pages, Blazor Server, worker services, and internal enterprise applications without
pulling host, API, MVC, dashboard, Playwright, or identity-provider SDK dependencies into
the authorization persistence layer.

## Architecture

`ChuA.Authorization.Erp` owns the provider-neutral authorization contract:

- `IErpAuthorizationStore`
- `ChuAErpAuthorizationOptions`
- `ErpAuthorizationContext`

`ChuA.Authorization.Erp.EntityFrameworkCore` owns only the EF Core implementation for the
current ERP schema:

- `EfCoreErpAuthorizationStore`
- `services.AddChuAErpAuthorizationEntityFrameworkCore()`

The store uses explicit EF Core joins over:

- `AppDbContext.ExternalLogins`
- `AppDbContext.UserRoles`
- `AppDbContext.Roles`
- `AppDbContext.RolePermissions`
- `AppDbContext.Permissions`

Roles and permissions are global. `ActiveCompanyId` is carried in `ErpAuthorizationContext`
for future schema evolution, but it does not filter grants today.

## Setup

Register `AppDbContext` in the host application with the provider and connection string that
the host owns. Then register the ERP authorization EF store:

```csharp
using ChuA.Authorization.Erp.EntityFrameworkCore.Extensions;
using ChuA.ERP.Database;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ErpDatabase"));
});

builder.Services.AddChuAErpAuthorizationEntityFrameworkCore();
```

Configure identity resolution with the Options pattern:

```csharp
builder.Services.AddChuAErpAuthorizationEntityFrameworkCore(options =>
{
    options.ExternalLoginProvider = "Auth0";
    options.SubjectClaimType = "sub";
    options.EnableNameIdentifierFallback = true;
    options.ErpUserIdClaimType = "erp_user_id";
});
```

The extension method does not register `AppDbContext`; host applications remain responsible
for database provider, connection string, migrations, and tenant/current-user infrastructure.

## Authorization Behavior

User resolution prefers a valid `erp_user_id` claim. When that claim is absent or invalid,
the store resolves the external subject from the configured subject claim, defaulting to
`sub`, and optionally falls back to `ClaimTypes.NameIdentifier`. It then looks up
`ExternalLogins` by configured provider and subject. Missing ERP membership returns `null`
instead of throwing.

Role grants flow through active, unexpired `UserRoles` and active `Roles`.

Permission grants flow through:

`UserRoles -> Roles -> RolePermissions -> Permissions`

Inactive roles, inactive assignments, inactive role-permission rows, inactive permissions,
and expired user-role assignments do not grant access.

Permission codes are returned exactly as stored in `Permission.Code`. The implementation does
not alias or normalize permission codes.

## Extension Points

Future schema changes can extend the store implementation behind the same
`IErpAuthorizationStore` contract. Examples include company-scoped grants, direct user
permissions, role hierarchy, deny rules, or cache decorators. Those behaviors should be added
deliberately in the persistence layer or in separate decorators without changing current
canonical permission-code semantics.

## License

Copyright (c) 2026 Alvin Wilsen Chan Chua.  
GitHub: chuaalvw-y

This project is licensed under the Alvin Wilsen Chan Chua Proprietary Use-Only License.

You may use this software for personal, educational, or internal evaluation purposes only. You may not modify, sell, sublicense, redistribute, publish, or include this software in a commercial product or service without prior written permission.

See [LICENSE.txt](LICENSE.txt) for full license details.

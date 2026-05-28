// Copyright (c) 2026 Alvin Wilsen Chan Chua
// GitHub: chuaalvw-y
// Licensed under the Alvin Wilsen Chan Chua Proprietary Use-Only License.
// See LICENSE.txt in the project root for full license information.

using ChuA.Authorization.Erp;
using ChuA.Authorization.Erp.EntityFrameworkCore.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChuA.Authorization.Erp.EntityFrameworkCore.Extensions;

/// <summary>
/// Registers the ChuA ERP authorization EF Core persistence implementation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the schema-specific EF Core implementation of <see cref="IErpAuthorizationStore"/>.
    /// The host application remains responsible for registering
    /// <see cref="ERP.Database.AppDbContext"/> with its database provider and connection string.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddChuAErpAuthorizationEntityFrameworkCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ChuAErpAuthorizationOptions>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IErpAuthorizationStore, EfCoreErpAuthorizationStore>();

        return services;
    }

    /// <summary>
    /// Adds the EF Core ERP authorization store and configures
    /// <see cref="ChuAErpAuthorizationOptions"/>.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">The options configuration callback.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddChuAErpAuthorizationEntityFrameworkCore(
        this IServiceCollection services,
        Action<ChuAErpAuthorizationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.AddChuAErpAuthorizationEntityFrameworkCore();
        services.Configure(configure);

        return services;
    }
}

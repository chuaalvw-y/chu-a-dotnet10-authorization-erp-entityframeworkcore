// Copyright (c) 2026 Alvin Wilsen Chan Chua
// GitHub: chuaalvw-y
// Licensed under the Alvin Wilsen Chan Chua Proprietary Use-Only License.
// See LICENSE.txt in the project root for full license information.

namespace ChuA.Authorization.Erp;

/// <summary>
/// Carries request-level ERP authorization facts that may influence future grant
/// evaluation without changing the store contract.
/// </summary>
/// <param name="ActiveCompanyId">
/// The active ERP company selected by the user. Current role and permission grants are global,
/// so EF persistence does not filter by this value yet.
/// </param>
public sealed record ErpAuthorizationContext(Guid? ActiveCompanyId = null);

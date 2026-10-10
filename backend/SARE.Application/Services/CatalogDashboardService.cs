using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Services;

public sealed class CatalogDashboardService(ICatalogDashboardRepository repository)
{
    public Task<CatalogDashboardResponse> GetSummaryAsync(CancellationToken cancellationToken) =>
        repository.GetSummaryAsync(cancellationToken);
}

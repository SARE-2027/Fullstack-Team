using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Common.Interfaces;

public interface ICatalogDashboardRepository
{
    Task<CatalogDashboardResponse> GetSummaryAsync(CancellationToken cancellationToken);
}

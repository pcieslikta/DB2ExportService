using DB2ExportService.Models;

namespace DB2ExportService.Services;

public interface IDB2Service
{
    Task<List<BramkiData>> GetBramkiDataAsync(DateTime targetDate, VehicleConfig vehicleConfig);
    Task<List<BramkiDetailData>> GetBramkiDetailDataAsync(DateTime targetDate, VehicleConfig vehicleConfig);
    Task<int?> GetRecordCountAsync(DateTime targetDate);

    /// <summary>
    /// Pobiera listę pojazdów z bazy danych z opcjonalnymi filtrami
    /// </summary>
    /// <param name="nbFrom">Numer pojazdu od (nullable)</param>
    /// <param name="nbTo">Numer pojazdu do (nullable)</param>
    /// <param name="activeOnly">Tylko aktywne pojazdy (nullable - null = wszystkie)</param>
    /// <returns>Lista VehicleInfo</returns>
    Task<List<VehicleInfo>> GetVehiclesAsync(int? nbFrom = null, int? nbTo = null, bool? activeOnly = null);
}

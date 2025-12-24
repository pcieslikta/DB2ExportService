using DB2ExportService.Models;

namespace DB2ExportService.Services;

public interface IDB2Service
{
    Task<List<BramkiData>> GetBramkiDataAsync(DateTime targetDate, VehicleConfig vehicleConfig);
    Task<List<BramkiDetailData>> GetBramkiDetailDataAsync(DateTime targetDate, VehicleConfig vehicleConfig);
    Task<int?> GetRecordCountAsync(DateTime targetDate);
}

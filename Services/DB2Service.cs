using System.Data;
using System.Text;
using DB2ExportService.Configuration;
using DB2ExportService.Models;
using IBM.Data.DB2.Core;
using Microsoft.Extensions.Logging;

namespace DB2ExportService.Services;

public class DB2Service : IDB2Service
{
    private readonly ConfigurationHelper _configHelper;
    private readonly ILogger<DB2Service> _logger;
    private readonly DB2Config _db2Config;
    private readonly ResiliencePolicyService _resiliencePolicy;

    public DB2Service(ConfigurationHelper configHelper, ILogger<DB2Service> logger, ResiliencePolicyService resiliencePolicy)
    {
        _configHelper = configHelper;
        _logger = logger;
        _db2Config = _configHelper.GetDB2Config();
        _resiliencePolicy = resiliencePolicy;
    }

    private DB2Connection CreateConnection()
    {
        _logger.LogDebug("Budowanie connection stringu z następujących parametrów:");
        _logger.LogDebug("  Database: {Database}", _db2Config.Database);
        _logger.LogDebug("  Server: {Hostname}:{Port}", _db2Config.Hostname, _db2Config.Port);
        _logger.LogDebug("  UID: {User}", string.IsNullOrEmpty(_db2Config.User) ? "***EMPTY***" : _db2Config.User);
        _logger.LogDebug("  PWD: {Password}", string.IsNullOrEmpty(_db2Config.Password) ? "***EMPTY***" : "***SET***");

        // IBM.Data.DB2.Core connection string format (Protocol is NOT a valid parameter)
        // Format: Server=hostname:port;Database=dbname;UID=user;PWD=password;
        var connectionString = $"Server={_db2Config.Hostname}:{_db2Config.Port};" +
                              $"Database={_db2Config.Database};" +
                              $"UID={_db2Config.User};" +
                              $"PWD={_db2Config.Password};" +
                              $"Authentication=SERVER;" +
                              $"Connect Timeout=30;";

        // Connection string bez hasła do logów
        var safeConnectionString = $"Server={_db2Config.Hostname}:{_db2Config.Port};" +
                                   $"Database={_db2Config.Database};" +
                                   $"UID={_db2Config.User};" +
                                   $"PWD=***;" +
                                   $"Authentication=SERVER;" +
                                   $"Connect Timeout=30;";

        _logger.LogInformation("Łączenie z bazą danych {Database} na {Hostname}:{Port}",
            _db2Config.Database, _db2Config.Hostname, _db2Config.Port);
        _logger.LogDebug("Connection string: {ConnectionString}", safeConnectionString);

        var connection = new DB2Connection(connectionString);
        connection.Open();

        _logger.LogInformation("Połączenie z bazą danych nawiązane pomyślnie");
        return connection;
    }

    public async Task<int?> GetRecordCountAsync(DateTime targetDate)
    {
        return await _resiliencePolicy.ExecuteDbOperationAsync(async () =>
        {
            using var connection = CreateConnection();
            var sql = @"SELECT DT_KARTY, COUNT(1) AS ILE
                       FROM ALASKA.RAPJAZDY
                       WHERE DT_KARTY = @targetDate
                       GROUP BY DT_KARTY";

            using var command = new DB2Command(sql, connection);
            command.Parameters.Add("@targetDate", DB2Type.Date).Value = targetDate.Date;

            _logger.LogInformation("Sprawdzanie liczby rekordów dla dnia: {Date}", targetDate.ToString("yyyy-MM-dd"));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var count = reader.GetInt32(reader.GetOrdinal("ILE"));
                _logger.LogInformation("Liczba rekordów dla dnia {Date}: {Count}", targetDate.ToString("yyyy-MM-dd"), count);
                return (int?)count;
            }

            _logger.LogWarning("Brak rekordów dla dnia: {Date}", targetDate.ToString("yyyy-MM-dd"));
            return null;
        }, $"GetRecordCount-{targetDate:yyyy-MM-dd}");
    }

    public async Task<List<BramkiData>> GetBramkiDataAsync(DateTime targetDate, VehicleConfig vehicleConfig)
    {
        return await _resiliencePolicy.ExecuteDbOperationAsync(async () =>
        {
            using var connection = CreateConnection();

            var vehicleCondition = BuildVehicleCondition(vehicleConfig);

            var sql = $@"
                SELECT
                    rap.DT_KARTY AS DATA,
                    INT(rap.NB_WOZU) AS NR_POJAZDU,
                    INT(rap.LP_KURSU) AS NR_KURSU,
                    TRIM(rap.NR_KURSOWK) AS NR_KURSOWK,
                    INT(rap.LP_PRZYST) AS LP_PRZYSTANKU,
                    TRIM(prz.NAZWA_PELN) AS PRZYSTANEK,
                    INT(prz.NUM_SLUPKA) AS SLUPEK_STANOWISKO,
                    INT(prz.ID_PRZYST) AS KOD_PRZYSTANKU,
                    TRIM(rap.RJ_KIEDY) AS CZAS_NA_PRZYSTANKU_ROZKLADOWY,
                    INT(rap.BR_ORG_IN) AS WS,
                    INT(rap.BR_ORG_OUT) AS WYS,
                    INT(rap.ILE_POSAZ) AS NAPELN,
                    TRIM(rap.NR_LINI) AS NR_LINII,
                    INT(ID_KURSU) AS ID_KURSU,
                    TRIM(poj.NR) AS NR_WOZU
                FROM ALASKA.RAPJAZDY rap
                INNER JOIN ALASKA.PRZYSTAN prz ON prz.ID_PRZYST = rap.ID_PRZYST
                INNER JOIN ALASKA.RE_POJAZDY poj ON poj.NB = rap.NB_WOZU
                WHERE rap.DT_KARTY = @targetDate
                {vehicleCondition}
                ORDER BY rap.NR_KURSOWK, rap.LP_KURSU, rap.LP_PRZYST";

            using var command = new DB2Command(sql, connection);
            command.Parameters.Add("@targetDate", DB2Type.Date).Value = targetDate.Date.AddDays(-1);

            _logger.LogInformation("Wykonywanie zapytania dla danych BRAMKI na dzień {Date}", targetDate.ToString("yyyy-MM-dd"));

            var result = new List<BramkiData>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new BramkiData
                {
                    Data = reader.GetDateTime(reader.GetOrdinal("DATA")),
                    NrPojazdu = reader.GetInt32(reader.GetOrdinal("NR_POJAZDU")),
                    NrKursu = reader.GetInt32(reader.GetOrdinal("NR_KURSU")),
                    NrKursowk = GetStringValue(reader, "NR_KURSOWK"),
                    LpPrzystanku = reader.GetInt32(reader.GetOrdinal("LP_PRZYSTANKU")),
                    Przystanek = GetStringValue(reader, "PRZYSTANEK"),
                    SlupekStanowisko = reader.GetInt32(reader.GetOrdinal("SLUPEK_STANOWISKO")),
                    KodPrzystanku = reader.GetInt32(reader.GetOrdinal("KOD_PRZYSTANKU")),
                    CzasNaPrzystankuRozkladowy = GetStringValue(reader, "CZAS_NA_PRZYSTANKU_ROZKLADOWY"),
                    WS = reader.GetInt32(reader.GetOrdinal("WS")),
                    WYS = reader.GetInt32(reader.GetOrdinal("WYS")),
                    Napeln = reader.GetInt32(reader.GetOrdinal("NAPELN")),
                    NrLinii = GetStringValue(reader, "NR_LINII"),
                    IdKursu = reader.GetInt32(reader.GetOrdinal("ID_KURSU")),
                    NrWozu = GetStringValue(reader, "NR_WOZU")
                });
            }

            _logger.LogInformation("Pobrano {Count} wierszy z tabeli BRAMKI", result.Count);
            return result;
        }, $"GetBramkiData-{targetDate:yyyy-MM-dd}");
    }

    public async Task<List<BramkiDetailData>> GetBramkiDetailDataAsync(DateTime targetDate, VehicleConfig vehicleConfig)
    {
        return await _resiliencePolicy.ExecuteDbOperationAsync(async () =>
        {
            using var connection = CreateConnection();

            var vehicleCondition = BuildVehicleCondition(vehicleConfig);

            var sql = $@"
                SELECT
                    rap.DT_KARTY as DATA,
                    INT(rap.NB_WOZU) as NR_POJAZDU,
                    TRIM(rap.NR_LINI) as NR_LINII,
                    TRIM(rap.NR_KURSOWK) as NR_LINII_PLANU,
                    INT(rap.LP_KURSU) as NR_KURSU,
                    INT(rap.LP_PRZYST) as LP_PRZYST,
                    TRIM(prz.NAZWA_PELN) as PRZYSTANEK,
                    TRIM(rap.RJ_KIEDY) as CZAS_NA_PRZYSTANKU_ROZKLAD,
                    INT(d90_0.BRAMKA_IN) as WEJSCIE_DRZWI_1,
                    INT(d90_0.BRAMKA_OUT) as WYJSCIE_DRZWI_1,
                    INT(d90_1.BRAMKA_IN) as WEJSCIE_DRZWI_2,
                    INT(d90_1.BRAMKA_OUT) as WYJSCIE_DRZWI_2,
                    INT(d90_2.BRAMKA_IN) as WEJSCIE_DRZWI_3,
                    INT(d90_2.BRAMKA_OUT) as WYJSCIE_DRZWI_3,
                    INT(d90_3.BRAMKA_IN) as WEJSCIE_DRZWI_4,
                    INT(d90_3.BRAMKA_OUT) as WYJSCIE_DRZWI_4,
                    INT(rap.BR_ORG_IN) as WS,
                    INT(rap.BR_ORG_OUT) as WYS,
                    INT(rap.ILE_POSAZ) as NAPELN
                FROM ALASKA.RAPJAZDY rap
                INNER JOIN ALASKA.PRZYSTAN prz ON prz.ID_PRZYST = rap.ID_PRZYST
                INNER JOIN ALASKA.RE_POJAZDY poj ON poj.NB = rap.NB_WOZU
                LEFT JOIN (
                    SELECT ID_RPJ, NR_DRZWI, SUM(CZLON_IN) BRAMKA_IN, SUM(CZLON_OUT) BRAMKA_OUT FROM ALASKA.DILAX90
                    GROUP BY ID_RPJ, NR_DRZWI
                ) d90_0 ON d90_0.ID_RPJ = rap.RECNO_ AND d90_0.NR_DRZWI = 0
                LEFT JOIN (
                    SELECT ID_RPJ, NR_DRZWI, SUM(CZLON_IN) BRAMKA_IN, SUM(CZLON_OUT) BRAMKA_OUT FROM ALASKA.DILAX90
                    GROUP BY ID_RPJ, NR_DRZWI
                ) d90_1 ON d90_1.ID_RPJ = rap.RECNO_ AND d90_1.NR_DRZWI = 1
                LEFT JOIN (
                    SELECT ID_RPJ, NR_DRZWI, SUM(CZLON_IN) BRAMKA_IN, SUM(CZLON_OUT) BRAMKA_OUT FROM ALASKA.DILAX90
                    GROUP BY ID_RPJ, NR_DRZWI
                ) d90_2 ON d90_2.ID_RPJ = rap.RECNO_ AND d90_2.NR_DRZWI = 2
                LEFT JOIN (
                    SELECT ID_RPJ, NR_DRZWI, SUM(CZLON_IN) BRAMKA_IN, SUM(CZLON_OUT) BRAMKA_OUT FROM ALASKA.DILAX90
                    GROUP BY ID_RPJ, NR_DRZWI
                ) d90_3 ON d90_3.ID_RPJ = rap.RECNO_ AND d90_3.NR_DRZWI = 3
                WHERE rap.DT_KARTY = @targetDate
                {vehicleCondition}
                ORDER BY rap.NB_WOZU, rap.NR_KURSOWK, rap.LP_KURSU, rap.LP_PRZYST";

            using var command = new DB2Command(sql, connection);
            command.Parameters.Add("@targetDate", DB2Type.Date).Value = targetDate.Date.AddDays(-1);

            _logger.LogInformation("Wykonywanie zapytania dla danych BRAMKID na dzień {Date}", targetDate.ToString("yyyy-MM-dd"));

            var result = new List<BramkiDetailData>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new BramkiDetailData
                {
                    Data = reader.GetDateTime(reader.GetOrdinal("DATA")),
                    NrPojazdu = reader.GetInt32(reader.GetOrdinal("NR_POJAZDU")),
                    NrLinii = GetStringValue(reader, "NR_LINII"),
                    NrLiniiPlanu = GetStringValue(reader, "NR_LINII_PLANU"),
                    NrKursu = reader.GetInt32(reader.GetOrdinal("NR_KURSU")),
                    LpPrzyst = reader.GetInt32(reader.GetOrdinal("LP_PRZYST")),
                    Przystanek = GetStringValue(reader, "PRZYSTANEK"),
                    CzasNaPrzystankuRozklad = GetStringValue(reader, "CZAS_NA_PRZYSTANKU_ROZKLAD"),
                    WejścieDrzwi1 = GetIntValue(reader, "WEJSCIE_DRZWI_1"),
                    WyjścieDrzwi1 = GetIntValue(reader, "WYJSCIE_DRZWI_1"),
                    WejścieDrzwi2 = GetIntValue(reader, "WEJSCIE_DRZWI_2"),
                    WyjścieDrzwi2 = GetIntValue(reader, "WYJSCIE_DRZWI_2"),
                    WejścieDrzwi3 = GetIntValue(reader, "WEJSCIE_DRZWI_3"),
                    WyjścieDrzwi3 = GetIntValue(reader, "WYJSCIE_DRZWI_3"),
                    WejścieDrzwi4 = GetIntValue(reader, "WEJSCIE_DRZWI_4"),
                    WyjścieDrzwi4 = GetIntValue(reader, "WYJSCIE_DRZWI_4"),
                    WS = reader.GetInt32(reader.GetOrdinal("WS")),
                    WYS = reader.GetInt32(reader.GetOrdinal("WYS")),
                    Napełn = reader.GetInt32(reader.GetOrdinal("NAPELN"))
                });
            }

            _logger.LogInformation("Pobrano {Count} wierszy z tabeli BRAMKID", result.Count);
            return result;
        }, $"GetBramkiDetailData-{targetDate:yyyy-MM-dd}");
    }

#pragma warning disable CS0618 // Using obsolete members for backward compatibility
    private string BuildVehicleCondition(VehicleConfig config)
    {
        if (config.PojazdyMode.ToLower() == "lista" && config.PojazdyLista.Any())
        {
            var vehicleList = string.Join(", ", config.PojazdyLista);
            _logger.LogInformation("Tryb lista, pojazdy: {Vehicles}", vehicleList);
            return $"AND rap.NB_WOZU IN ({vehicleList})";
        }
        else if (config.PojazdyStart.HasValue && config.PojazdyEnd.HasValue)
        {
            _logger.LogInformation("Tryb zakres: od {Start} do {End}", config.PojazdyStart.Value, config.PojazdyEnd.Value);
            return $"AND rap.NB_WOZU BETWEEN {config.PojazdyStart.Value} AND {config.PojazdyEnd.Value}";
        }

        _logger.LogWarning("Brak filtra pojazdów - eksport dla wszystkich pojazdów");
        return string.Empty;
    }
#pragma warning restore CS0618

    private string GetStringValue(IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return string.Empty;

        var value = reader.GetValue(ordinal);

        // Obsługa polskich znaków (CP1250)
        if (value is byte[] bytes)
        {
            return Encoding.GetEncoding(1250).GetString(bytes).Trim();
        }

        return value?.ToString()?.Trim() ?? string.Empty;
    }

    private int GetIntValue(IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return 0;

        // DB2 DECIMAL columns need special handling
        var value = reader.GetValue(ordinal);
        return Convert.ToInt32(value);
    }

    private DateTime? GetDateTimeValue(IDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        if (reader.IsDBNull(ordinal))
            return null;

        return reader.GetDateTime(ordinal);
    }

    public async Task<List<VehicleInfo>> GetVehiclesAsync(int? nbFrom = null, int? nbTo = null, bool? activeOnly = null)
    {
        return await _resiliencePolicy.ExecuteDbOperationAsync(async () =>
        {
            using var connection = CreateConnection();

            // Budowanie dynamicznego WHERE
            var whereClauses = new List<string>();

            if (nbFrom.HasValue)
                whereClauses.Add($"NB >= {nbFrom.Value}");

            if (nbTo.HasValue)
                whereClauses.Add($"NB <= {nbTo.Value}");

            // Removed activeOnly filter as STATUS column doesn't exist
            // if (activeOnly.HasValue && activeOnly.Value)
            //     whereClauses.Add("STATUS = 'A'");

            var whereClause = whereClauses.Any()
                ? "WHERE " + string.Join(" AND ", whereClauses)
                : "";

            var sql = $@"
                SELECT
                    NB,
                    TRIM(NR) AS NR,
                    TYP_POJ,
                    ID_MARKI,
                    DEW,
                    ZEW,
                    MA_BRAMKI,
                    WGOTOWOSCI,
                    ZAJEZDNIA
                FROM ALASKA.RE_POJAZDY
                {whereClause}
                ORDER BY NB";

            using var command = new DB2Command(sql, connection);

            _logger.LogInformation(
                "Pobieranie pojazdów: NB {NbFrom}-{NbTo}",
                nbFrom, nbTo);

            var result = new List<VehicleInfo>();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new VehicleInfo
                {
                    NB = GetIntValue(reader, "NB"),
                    NR = GetStringValue(reader, "NR"),
                    TypPoj = GetIntValue(reader, "TYP_POJ"),
                    IdMarki = GetIntValue(reader, "ID_MARKI"),
                    Dew = GetDateTimeValue(reader, "DEW"),
                    Zew = GetDateTimeValue(reader, "ZEW"),
                    MaBramki = GetStringValue(reader, "MA_BRAMKI"),
                    WGotowosci = GetStringValue(reader, "WGOTOWOSCI"),
                    Zajezdnia = GetIntValue(reader, "ZAJEZDNIA")
                });
            }

            _logger.LogInformation("Pobrano {Count} pojazdów", result.Count);
            return result;
        }, $"GetVehicles-{nbFrom}-{nbTo}");
    }
}

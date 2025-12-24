namespace DB2ExportService.Models;

public class ExportConfig
{
    public string KodExportu { get; set; } = string.Empty;
    public string ExportPath { get; set; } = "C:\\EXPORT\\";
    public string LogPath { get; set; } = "C:\\EXPORT\\LOG\\";
    public string ScheduleTime { get; set; } = "13:15";
    public int DaysBack { get; set; } = -2;
}

public class DB2Config
{
    public string Database { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public int Port { get; set; } = 50000;
    public string Protocol { get; set; } = "TCPIP";
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseCredentialManager { get; set; } = false;
    public string CredentialKey { get; set; } = string.Empty;
    public int CCSID { get; set; } = 1250;
}

public class VehicleConfig
{
    public string KodExportu { get; set; } = string.Empty;
    public string PojazdyMode { get; set; } = "zakres"; // "zakres" lub "lista"
    public int? PojazdyStart { get; set; }
    public int? PojazdyEnd { get; set; }
    public List<int> PojazdyLista { get; set; } = new();
}

public class BramkiData
{
    public DateTime Data { get; set; }
    public int NrPojazdu { get; set; }
    public int NrKursu { get; set; }
    public string NrKursowk { get; set; } = string.Empty;
    public int LpPrzystanku { get; set; }
    public string Przystanek { get; set; } = string.Empty;
    public int SlupekStanowisko { get; set; }
    public int KodPrzystanku { get; set; }
    public string CzasNaPrzystankuRozkladowy { get; set; } = string.Empty;
    public int WS { get; set; }
    public int WYS { get; set; }
    public int Napeln { get; set; }
    public string NrLinii { get; set; } = string.Empty;
    public int IdKursu { get; set; }
    public string NrWozu { get; set; } = string.Empty;
}

public class BramkiDetailData
{
    public DateTime Data { get; set; }
    public int NrPojazdu { get; set; }
    public string NrLinii { get; set; } = string.Empty;
    public string NrLiniiPlanu { get; set; } = string.Empty;
    public int NrKursu { get; set; }
    public int LpPrzyst { get; set; }
    public string Przystanek { get; set; } = string.Empty;
    public string CzasNaPrzystankuRozklad { get; set; } = string.Empty;
    public int WejścieDrzwi1 { get; set; }
    public int WyjścieDrzwi1 { get; set; }
    public int WejścieDrzwi2 { get; set; }
    public int WyjścieDrzwi2 { get; set; }
    public int WejścieDrzwi3 { get; set; }
    public int WyjścieDrzwi3 { get; set; }
    public int WejścieDrzwi4 { get; set; }
    public int WyjścieDrzwi4 { get; set; }
    public int WS { get; set; }
    public int WYS { get; set; }
    public int Napełn { get; set; }
}

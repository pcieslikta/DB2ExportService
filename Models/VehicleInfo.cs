namespace DB2ExportService.Models;

/// <summary>
/// Model reprezentujący informacje o pojeździe z bazy DB2
/// </summary>
public class VehicleInfo
{
    /// <summary>
    /// Numer bazy pojazdu (klucz główny)
    /// </summary>
    public int NB { get; set; }

    /// <summary>
    /// Numer rejestracyjny pojazdu
    /// </summary>
    public string NR { get; set; } = string.Empty;

    /// <summary>
    /// Status pojazdu (A = aktywny, N = nieaktywny)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Czy pojazd jest aktywny
    /// </summary>
    public bool IsActive => Status?.Trim().ToUpper() == "A";

    /// <summary>
    /// Formatowane wyświetlenie dla listy
    /// </summary>
    public override string ToString()
    {
        return $"{NB} - {NR} ({(IsActive ? "Aktywny" : "Nieaktywny")})";
    }
}

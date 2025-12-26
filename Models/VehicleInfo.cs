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
    /// Typ pojazdu (DECIMAL(2))
    /// </summary>
    public int TypPoj { get; set; }

    /// <summary>
    /// ID marki pojazdu (DECIMAL(5))
    /// </summary>
    public int IdMarki { get; set; }

    /// <summary>
    /// Data dewizacji (DATE)
    /// </summary>
    public DateTime? Dew { get; set; }

    /// <summary>
    /// Data zewnętrzna (DATE)
    /// </summary>
    public DateTime? Zew { get; set; }

    /// <summary>
    /// Czy ma bramki (CHAR(1) FOR BIT DATA)
    /// </summary>
    public string MaBramki { get; set; } = string.Empty;

    /// <summary>
    /// W gotowości (CHAR(1) FOR BIT DATA)
    /// </summary>
    public string WGotowosci { get; set; } = string.Empty;

    /// <summary>
    /// Zajezdnia (DECIMAL(5))
    /// </summary>
    public int Zajezdnia { get; set; }

    /// <summary>
    /// Formatowane wyświetlenie dla listy
    /// </summary>
    public override string ToString()
    {
        return $"{NB} - {NR} ({TypPoj})";
    }
}

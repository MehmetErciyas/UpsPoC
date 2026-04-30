// UpsPoC.Api/Models/MetricDetail.cs
namespace UpsPoC.Api.Models;

// Python netagent_gui_v10 read_metric / read_info_item çıktısının paritesi.
public class MetricDetail
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;       // "live" | "info" | "f-equivalent"
    public string ValueText { get; set; } = string.Empty;   // Formatlı değer (örn. "220.0 V")
    public string RawValue { get; set; } = string.Empty;    // Ham SNMP cevabı
    public string Oid { get; set; } = string.Empty;         // Cevap veren OID
    public bool Ok { get; set; }
    public string? Error { get; set; }
}

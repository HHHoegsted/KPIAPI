namespace KPIAPI.DTOs
{
    public record RecordKpisRequest(DateTime? RecordedUtc, List<KPIDTO> Kpis);
}

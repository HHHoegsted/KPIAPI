namespace KPIAPI.Domain.Enums
{
    public enum LogicalRunOutcome
    {
        Unknown = 0,
        InProgress = 1,
        Succeeded = 2,
        SucceededAfterRetry = 3,
        Failed = 4,
    }
}
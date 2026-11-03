namespace EventsApi.Models.Enums;

public static class BookingStatusExtensions
{
    public static string GetDescription(this BookingStatus status)
    {
        return status switch
        {
            BookingStatus.Pending => "Бронь создана, ожидает обработки",
            BookingStatus.Confirmed => "Бронь подтверждена",
            BookingStatus.Rejected => "Бронь отклонена",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
    }
}

using EventsApi.Data.Repositories;
using EventsApi.Models;

namespace EventsApi.BackgroundServices;

public class BookingProcessingBackgroundService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    public BookingProcessingBackgroundService(IBookingRepository bookingRepository, ILogger<BookingProcessingBackgroundService> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Booking background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in booking background loop");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Booking background service stopped");
    }

    private async Task ProcessPendingBookingsAsync(CancellationToken stoppingToken)
    {
        var pending = _bookingRepository.FindByPendingStatus();
        if (pending.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} pending bookings", pending.Count);

        foreach (var booking in pending)
            await ProcessBookingAsync(booking, stoppingToken);
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);

            booking.Confirm(DateTime.UtcNow);
            _bookingRepository.Update(booking.Id, booking);

            _logger.LogInformation("Booking {BookingId} confirmed", booking.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to process booking {BookingId}", booking.Id);
        }
    }
}

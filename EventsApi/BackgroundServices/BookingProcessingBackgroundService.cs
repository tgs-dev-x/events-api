using EventsApi.Data.Repositories;
using EventsApi.Models;
using EventsApi.Services;

namespace EventsApi.BackgroundServices;

public class BookingProcessingBackgroundService : BackgroundService
{
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingProcessingBackgroundService> _logger;

    private readonly BookingSynchronization _synchronization;

    public BookingProcessingBackgroundService(IBookingRepository bookingRepository, IEventRepository eventRepository, ILogger<BookingProcessingBackgroundService> logger, BookingSynchronization synchronization)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _logger = logger;
        _synchronization = synchronization;
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
        var pendingBookings = _bookingRepository.FindByPendingStatus();
        if (pendingBookings.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} pending bookings", pendingBookings.Count);

        var tasks = pendingBookings
            .Select(booking => ProcessBookingAsync(booking, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        await Task.Delay(ProcessingDelay, stoppingToken);
        await _synchronization.Gate.WaitAsync(stoppingToken);

        try
        {
            var eventData = _eventRepository.FindById(booking.EventId);

            if (eventData is null)
            {
                booking.Reject(DateTime.UtcNow);
                _logger.LogWarning("Event for booking {BookingId} doesn't exist", booking.Id);
                _bookingRepository.Update(booking.Id, booking);
                return;
            }

            booking.Confirm(DateTime.UtcNow);
            _bookingRepository.Update(booking.Id, booking);
            _logger.LogInformation("Booking {BookingId} confirmed", booking.Id);

        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to process booking {BookingId}", booking.Id);

            booking.Reject(DateTime.UtcNow);

            var eventData = _eventRepository.FindById(booking.EventId);

            if (eventData is not null)
            {
                eventData.ReleaseSeats();
                _eventRepository.Update(eventData.Id, eventData);
            }

            _bookingRepository.Update(booking.Id, booking);
            return;
        }
        finally
        {
            _synchronization.Gate.Release();
        }
    }
}

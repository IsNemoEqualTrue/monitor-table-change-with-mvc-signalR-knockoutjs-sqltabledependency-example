using FlightBooking.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace FlightBooking
{
    [HubName("flightBookingTicker")]
    public class FlightBookingHub : Hub
    {
        private readonly FlightBookingService _flightBookingService;

        public FlightBookingHub() : this(FlightBookingService.Instance)
        {
        }

        public FlightBookingHub(FlightBookingService flightBookingHub)
        {
            _flightBookingService = flightBookingHub;
        }

        public FlightsAvailability GetAll()
        {
            return _flightBookingService.GetAll();
        }
    }
}
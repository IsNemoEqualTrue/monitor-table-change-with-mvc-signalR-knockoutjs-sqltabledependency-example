using Newtonsoft.Json;

namespace FlightBooking.Models
{
    public class FlightAvailability
    {
        [JsonProperty(PropertyName = "flightId")]
        public int FlightId { get; set; }

        [JsonProperty(PropertyName = "from")]
        public string From { get; set; }

        [JsonProperty(PropertyName = "to")]
        public string To { get; set; }

        [JsonProperty(PropertyName = "freeSeats")]
        public int Availability { get; set; }
    }
}
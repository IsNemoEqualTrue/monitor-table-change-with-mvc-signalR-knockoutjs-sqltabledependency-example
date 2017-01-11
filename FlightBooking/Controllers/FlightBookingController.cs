using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Web.Mvc;
using FlightBooking.Models;

namespace FlightBooking.Controllers
{
    public class FlightBookingController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Book(FlightBookViewModel viewModel)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"DECLARE @seat int = {viewModel.Seats}; UPDATE [FlightBookings] SET [SeatsAvailability] = [SeatsAvailability] - @seat WHERE [FlightId] = {viewModel.Id}";
                    sqlCommand.ExecuteNonQuery();                    
                }
            }

            return this.Json($"Booked {viewModel.Seats} seat(s)");
        }
    }
}
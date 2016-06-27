using System.Web.Mvc;

namespace FlightBooking.Controllers
{
    public class FlightBookingController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
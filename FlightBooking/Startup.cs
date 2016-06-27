using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(FlightBooking.Startup))]

namespace FlightBooking
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}

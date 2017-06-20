# Audit SQL Server table change by detect insert update delete and receive notifications

![alt text][shema]

[shema]: https://github.com/christiandelbianco/monitor-table-change-with-mvc-signalR-knockoutjs-sqltabledependency/blob/master/Splash-min.png "Notifications"

## Detect record table changes
One of the classic problems when writing software dealing with database is refreshing data.

Imagine a tool for display real time booking flight. After a first data access to retrieve records from database table, in order to maintain the cache up to date, further selects are needed over and over again. That is inefficient if the data rarely changes and it wastes resources and execution time on the database server.

Caching is one technique for minimizing demands on the database server. The data are queried once and stored in memory and the application then repeatedly accesses it. Occasionally, the cache is updated to refresh the data. The point is deciding when to update it. If we don’t do it often enough, users see old data; if we update too often, then we don’t optimally reduce our demand on the database.

**Wouldn't it be better if was our database to instantly notify our application about record changes, avoiding us to execute a periodical SELECT to refresh our server cache?**

Database table change notifications can help to solve this tricky problem. But how to monitor SQL Server table changes? Some options are:
* SQL Server Service Broker
* .NET SqlNotificationRequest
* .NET SqlDependency

All of them works based on a notifications infrastructure. The first two options require us a good T-SQL and database knowledge to create database objects as service broker and queue to monitor every change done on our records table and notify us about any record table change. Although the last one does not require us this low level knowledge, it presents a limitation: delivered notification does not report us any information about which record has been changed, forcing us to perform a further SELECT to refresh our cache.

**Is it possible receive record table change notifications containing modified, deleted or inserted records in order to avoid another SELECT to maintains update our cache?**

## Notification on table changes with SqlTableDependency
If we want get an alert about any table changes without paying attention to the underlying SQL Server infrastructure then 
[SqlTableDependency](https://github.com/christiandelbianco/monitor-table-change-with-sqltabledependency)'s record table change notifications will do that for us. Using notifications, an application can detect record changes saving us from having to continuously re-query the database to get new values.

SqlTableDependency record change audit, provides the low-level implementation to receive database notifications creating SQL Server trigger, queue and service broker that immediately notify us when any record table changes happens.

For any record change, SqlTableDependency's event handler will get a notification containing modified table record values as well as the insert, update, delete operation type executed on our table.

## Audit change with ASP.NET MVC, SignalR and Knockout JS
The following video show how to build a web application able to send real time notifications to clients. The code is visible below:

[![IMAGE ALT TEXT HERE](http://img.youtube.com/vi/FBkkdCuTO7g/0.jpg)](https://www.youtube.com/watch?v=FBkkdCuTO7g)

Get notify on insert update delete table changes
Let’s assume a Web application used to book flight tickets used by different booking terminals. Those terminals have to be update as soon as the availability change and the Web application must take the initiative of sending this information to clients instead of waiting for the client to request it.

Start installing the following Nuget Package:

```
PM> Install-Package Microsoft.AspNet.SignalR 
PM> Install-Package Knockoutjs
PM> Install-Package Knockout.Mapping
PM> Install-Package SqlTableDependency
```

Assuming a table as:
```SQL
CREATE TABLE [dbo].[FlightBookings](
	[FlightId] [int] PRIMARY KEY NOT NULL,
	[From] [nvarchar](50),
	[To] [nvarchar](50),
	[SeatsAvailability] [int])
```

We start defining our C# model:
```C#
public class SeatsAvailability
{
   public string From { get; set; }
   public string To { get; set; }
   public int Seats { get; set; }
}
```

Then initialize SignalR:
```C#
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
```

Create the view showing seats availability:
```HTML
<!DOCTYPE html>
<html>
<head>
    <title>SignalR, Knockout JS and SqlTableDependencly</title>
</head>
<body>
    <div class="container" style="margin-top: 20px">
        @RenderBody()        
    </div>

    <script src="~/Scripts/jquery-1.10.2.js"></script>
    <script src="~/Scripts/jquery.signalR-2.2.0.js"></script>
    <script src="~/signalr/hubs"></script>
    <script src="~/Scripts/knockout-3.4.0.js"></script>
    <script src="~/Scripts/knockout.mapping-latest.js"></script>
    
    @RenderSection("scripts", required: false)
</body>
</html>
```

Then defining our Hub class. We are going to use it to retrieve the first set of flight seats availability and then to push seats availability change from server. This class establish a communication channel between the server and clients:
```C#
[HubName("flightBookingTicker")]
public class FlightBookingHub : Hub
{
   private readonly FlightBookingService _flightBookingService;

   public FlightBookingHub() : this(FlightBookingService.Instance) { }

   public FlightBookingHub(FlightBookingService flightBookingHub)
   {
      _flightBookingService = flightBookingHub;
   }

   // used to get the first result set concerning seats availability
   public FlightsAvailability GetAll()
   {
      return _flightBookingService.GetAll();
   }
}
```

We define a singleton service class to constitute the channel between database and web application, able to be the listener for record modifications. For this we are going to use SqlTableDependency:
```C#
public class FlightBookingService : IDisposable
{
   // singleton instance
   private readonly static Lazy _instance = 
      new Lazy(() => 
         new FlightBookingService(
           GlobalHost.ConnectionManager.GetHubContext().Clients));

   private SqlTableDependency SqlTableDependency { get; }
   private IHubConnectionContext Clients { get; }

   private static connectionString = 
      ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;

   private FlightBookingService(IHubConnectionContext clients)
   {
      this.Clients = clients;

      // because our C# model has a property not matching database table name, 
      // an explicit mapping is required just for this property
      var mapper = new ModelToTableMapper();
      mapper.AddMapping(x => x.Availability, "SeatsAvailability");

      // because our C# model name differs from table name we have to 
      // specify database table name
      this.SqlTableDependency = new SqlTableDependency(
         connectionString, 
         "FlightBookings", 
         mapper);
      
      this.SqlTableDependency.OnChanged += this.TableDependency_OnChanged;
      this.SqlTableDependency.Start();
   }

   public static FlightBookingService Instance => _instance.Value;

   public FlightsAvailability GetAll()
   {
      var flightsAvailability = new List();

      using (var sqlConnection = new SqlConnection(connectionString))
      {
         sqlConnection.Open();
         using (var sqlCommand = sqlConnection.CreateCommand())
         {
            sqlCommand.CommandText = "SELECT * FROM [FlightBookings]";

            using (var sqlDataReader = sqlCommand.ExecuteReader())
            {
               while (sqlDataReader.Read())
               {
                  var flightId = sqlDataReader.GetInt32(0);
                  var from = sqlDataReader.GetString(1);
                  var to = sqlDataReader.GetString(2);
                  var seats = sqlDataReader.GetInt32(2);

                  flightsAvailability.Add(new FlightAvailability { 
                     FlightId = flightId, 
                     From = from, 
                     To = to, 
                     Availability = seats 
                  });
               }
            }
         }
      }

      return new FlightsAvailability() { 
         FlightCompanyId = "field not used", 
         FlightAvailability = flightsAvailability 
      };
   }

   private void TableDependency_OnChanged(object sender, 
      RecordChangedEventArgs e)
   {
      switch (e.ChangeType)
      {
         case ChangeType.Delete:                    
            this.Clients.All.removeFlightAvailability(e.Entity);
            break;

         case ChangeType.Insert:
            this.Clients.All.addFlightAvailability(e.Entity);
            break;

         case ChangeType.Update:
            this.Clients.All.updateFlightAvailability(e.Entity);
            break;
      }
   }

   public void Dispose()
   {
      // invoke Stop() in order to remove all DB objects 
      // generated from SqlTableDependency
      this.SqlTableDependency.Stop();
   }
}
```

Then create a simple controller just to render our view:
```C#
public class FlightBookingController : Controller
{
    public ActionResult Index()
    {
        return View();
    }
}
```

```HTML
<table class="table table-striped">
    <thead style="background-color: silver">
        <tr>
            <th>Flight Id</th>
            <th>From</th>
            <th>To</th>
            <th>Seats Availability</th>
        </tr>
    </thead>
    <tbody data-bind="foreach: flights">
        <tr>
            <td><span data-bind="text: $data.flightId"></span></td>
            <td><span data-bind="text: $data.from"></span></td>
            <td><span data-bind="text: $data.to"></span></td>
            <td><span data-bind="text: $data.freeSeats"></span></td>
        </tr>
    </tbody>
</table>

@section Scripts {
    <script src="~/Scripts/flightBookingViewModels.js"></script>
    <script src="~/Scripts/flightBookingTicker.js"></script>
}
```

To conclude we define the js view models:
```javascript
// flight ViewModel definition
function FlightBookingViewModel(flight) {
    var self = this;

    var mappingOptions = {
        key: function (data) {
            return ko.utils.unwrapObservable(data.flightId);
        }
    };

    ko.mapping.fromJS(flight, mappingOptions, self);
};

// flights view model definition
function FlightsBookingViewModel(flights) {
    var self = this;

    var flightsBookingMappingOptions = {
        flights: {
            create: function (options) {
                return new FlightBookingViewModel(options.data);
            }
        }
    };

    self.addFlightAvailability = function (flight) {
        self.flights.push(new FlightBookingViewModel(flight));
    };

    self.updateFlightAvailability = function (flight) {
        var flightMappingOptions = {
            update: function (options) {
                ko.utils.arrayForEach(options.target, function (item) {
                    if (item.flightId() === options.data.flightId) {
                        item.freeSeats(options.data.freeSeats);
                    }
                });
            }
        };

        ko.mapping.fromJS(flight, flightMappingOptions, self.flights);
    };

    self.removeFlightAvailability = function (flight) {
        self.flights.remove(function(item) {
             return item.flightId() === flight.flightId;
        });
    };

    ko.mapping.fromJS(flights, flightsBookingMappingOptions, self);
};
```

```javascript
$(function () {
    var viewModel = null;

    // generate client-side hub proxy and then 
    // add client-side hub methods that the server will call
    var ticker = $.connection.flightBookingTicker;

    // Add a client-side hub method that the server will call
    ticker.client.updateFlightAvailability = function (flight) {
        viewModel.updateFlightAvailability(flight);
    };

    ticker.client.addFlightAvailability = function (flight) {
        viewModel.addFlightAvailability(flight);
    };

    ticker.client.removeFlightAvailability = function (flight) {
        viewModel.removeFlightAvailability(flight);
    };

    // start the connection, load seats availability and set the knockout ViewModel
    $.connection.hub.start().done(function() {
        ticker.server.getAll().done(function (flightsBooking) {
            viewModel = new FlightsBookingViewModel(flightsBooking);
            ko.applyBindings(viewModel);
        });
    });
});
```

For more info about SqlTableDependency, refere to https://github.com/christiandelbianco/monitor-table-change-with-sqltabledependency 

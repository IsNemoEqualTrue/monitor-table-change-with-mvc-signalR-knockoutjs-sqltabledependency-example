using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using FlightBooking.Models;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.Mappers;
using TableDependency.SqlClient;

namespace FlightBooking
{
    public class FlightBookingService : IDisposable
    {
        #region Member variables

        // Singleton instance
        private readonly static Lazy<FlightBookingService> _instance = new Lazy<FlightBookingService>(() => new FlightBookingService(GlobalHost.ConnectionManager.GetHubContext<FlightBookingHub>().Clients));
        private SqlTableDependency<FlightAvailability> SqlTableDependency { get; }
        private IHubConnectionContext<dynamic> Clients { get; }

        #endregion

        #region Constructors

        private FlightBookingService(IHubConnectionContext<dynamic> clients)
        {
            this.Clients = clients;

            // Because our C# model has a property not matching database table name, an explicit mapping is required just for this property.
            var mapper = new ModelToTableMapper<FlightAvailability>();
            mapper.AddMapping(x => x.Availability, "SeatsAvailability");

            // Because our C# model name differs from table name we have to specify database table name.
            this.SqlTableDependency = new SqlTableDependency<FlightAvailability>(ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString, "FlightBookings", mapper);
            this.SqlTableDependency.OnChanged += this.TableDependency_OnChanged;
            this.SqlTableDependency.TraceLevel = TraceLevel.Verbose;
            this.SqlTableDependency.TraceListener = new TextWriterTraceListener(File.Create("c:\\Temp\\output.txt"));
            this.SqlTableDependency.Start();
        }

        #endregion

        #region Public Methods

        public static FlightBookingService Instance => _instance.Value;

        public FlightsAvailability GetAll()
        {
            var flightsAvailability = new List<FlightAvailability>();

            var connectionString = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
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
                            var flightId = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("FlightId"));
                            var from = sqlDataReader.GetString(sqlDataReader.GetOrdinal("From"));
                            var to = sqlDataReader.GetString(sqlDataReader.GetOrdinal("To"));
                            var seats = sqlDataReader.GetInt32(sqlDataReader.GetOrdinal("SeatsAvailability"));

                            flightsAvailability.Add(new FlightAvailability { FlightId = flightId, From = from, To = to, Availability = seats });
                        }
                    }
                }
            }

            return new FlightsAvailability() { FlightCompanyId = "not used", FlightAvailability = flightsAvailability };
        }

        #endregion

        #region Private Methods

        private void TableDependency_OnChanged(object sender, RecordChangedEventArgs<FlightAvailability> e)
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

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // Invoke Stop() in order to remove all DB objects genetated from SqlTableDependency.
            this.SqlTableDependency.Stop();
        }

        #endregion
    }
}
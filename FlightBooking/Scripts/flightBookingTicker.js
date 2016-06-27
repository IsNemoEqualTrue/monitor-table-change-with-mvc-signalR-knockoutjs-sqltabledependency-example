/// <reference path="jquery-1.10.2.js" />
/// <reference path="jquery.signalR-2.2.0.js" />
/// <reference path="flightBookingViewModels.js" />
/// <reference path="knockout-3.4.0.debug.js" />

$(function () {
    var viewModel = null;

    // Generated client-side hub proxy and then 
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

    // Start the connection and load flights
    $.connection.hub.start().done(function() {
        ticker.server.getAll().done(function (flightsBooking) {
            viewModel = new FlightsBookingViewModel(flightsBooking);
            ko.applyBindings(viewModel);
        });
    });
});
/// <reference path="knockout.mapping-latest.js" />
/// <reference path="knockout-3.4.0.debug.js" />

// flight view model definition
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

    self.flightId = ko.observable(null);
    self.seats = ko.observable(null);
    self.result = ko.observable(null);

    self.book = function ()
    {
        $.ajax({
            type: "POST",
            url: "/FlightBooking/Book",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify({ Id: self.flightId(), Seats: self.seats() }),
            dataType: "json",
            success: function (data) 
            {
                self.result(data.Message);
                self.flightId(null);
                self.seats(null);                
            },
            error: function (xmlHttpRequest, textStatus, errorThrown)
            {
                self.result(errorThrown);
            }
        });
    };

    ko.mapping.fromJS(flights, flightsBookingMappingOptions, self);
};
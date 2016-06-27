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

    ko.mapping.fromJS(flights, flightsBookingMappingOptions, self);
};
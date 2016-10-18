CREATE TABLE [dbo].[FlightBookings](
	[FlightId] [int] PRIMARY KEY NOT NULL,
	[From] [nvarchar](50),
	[To] [nvarchar](50),
	[SeatsAvailability] [int])
namespace Gite_Planning.Controllers

open System

open Microsoft.AspNetCore.Mvc

open Gite_Planning.Calendar
open Gite_Planning.Models
open Gite_Planning.Services

[<ApiController>]
[<Route("api/[controller]")>]
type CalendarApiController () =
    inherit ControllerBase()

    [<HttpGet("month")>]
    member this.GetMonth(year: int, month: int) =
        let referenceDate = DateTime(year, month, 1)

        let events = []
        let rooms = HotelDataService.getRooms()
        let reservations = HotelDataService.getReservations()

        let model =
            CalendarService.BuildMonthView
                referenceDate
                events
                rooms
                reservations

        this.Ok(model)

    [<HttpPost("event")>]
    member this.AddEvent([<FromBody>] form: CalendarEventForm) : IActionResult =

        try
            let eventDate =
                DateTime.Parse(form.Date)

            let timeParts =
                form.Time.Split(':')

            let hour =
                Int32.Parse(timeParts.[0])

            let minute =
                if timeParts.Length > 1 then
                    Int32.Parse(timeParts.[1])
                else
                    0

            let startDate =
                eventDate.Date.AddHours(float hour).AddMinutes(float minute)

            let endDate =
                startDate.AddMinutes(float form.DurationMinutes)

            let newEvent : CalendarEvent =
                { Id = 0
                  Title = form.Title
                  Description = form.Description
                  Start = startDate
                  End = endDate }

            this.Ok(newEvent)
        with
        | ex ->
            this.BadRequest(ex.Message)

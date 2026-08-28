namespace Gite_Planning.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Gite_Planning.Calendar
open Gite_Planning.Models

[<ApiController>]
[<Route("api/[controller]")>]
type CalendarController() =
    inherit ControllerBase()

    static let mutable events: CalendarEvent list =
        [
            { Id = 1; Title = "Réunion projet"; Description = "Suivi sprint"; Start = DateTime(2026, 2, 10, 9, 0, 0); End = DateTime(2026, 2, 10, 10, 0, 0) }
            { Id = 2; Title = "Rendez-vous médical"; Description = "Consultation"; Start = DateTime(2026, 2, 15, 14, 30, 0); End = DateTime(2026, 2, 15, 15, 30, 0) }
        ]

    [<HttpGet("month")>]
    member this.GetMonth([<FromQuery>] year:int, [<FromQuery>] month:int) =
        let referenceDate = DateTime(year, month, 1)
        CalendarService.BuildMonthView referenceDate events [] []

    [<HttpPost("events")>]
    member this.CreateEvent([<FromBody>] model: CalendarEventForm) =
        let dateValue = DateTime.Parse(model.Date)
        let timeValue = TimeSpan.Parse(model.Time)
        let start = dateValue.Date.Add(timeValue)
        let endAt = start.AddMinutes(float model.DurationMinutes)

        let nextId =
            match events with
            | [] -> 1
            | _ -> (events |> List.map (fun e -> e.Id) |> List.max) + 1

        let item =
            { Id = nextId
              Title = model.Title
              Description = model.Description
              Start = start
              End = endAt }

        events <- item :: events
        this.Ok(item)

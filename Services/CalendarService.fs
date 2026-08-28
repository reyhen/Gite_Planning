namespace Gite_Planning.Calendar

open System
open System.Globalization
open Gite_Planning.Models

module CalendarService =
    let private toMondayBasedStart (source: DateTime) =
        let offset = ((int source.DayOfWeek + 6) % 7)
        source.Date.AddDays(float (-offset))

    let GetMonthEvents (referenceDate: DateTime) (events: List<CalendarEvent>) =
        events
        |> List.filter (fun ev -> ev.Start.Year = referenceDate.Year && ev.Start.Month = referenceDate.Month)
        |> List.sortBy (fun ev -> ev.Start)

    let BuildMonthGrid (referenceDate: DateTime) (events: List<CalendarEvent>) (rooms: List<Room>) (reservations: List<Reservation>) =
        let firstOfMonth = DateTime(referenceDate.Year, referenceDate.Month, 1)
        let firstVisibleDate = toMondayBasedStart firstOfMonth

        [ 0 .. 41 ]
        |> List.map (fun offset ->
            let day = firstVisibleDate.AddDays(float offset)
            let dayEvents =
                events
                |> List.filter (fun ev -> ev.Start.Date = day.Date)
                |> List.sortBy (fun ev -> ev.Start)

            let dayReservations =
                reservations
                |> List.filter (fun reservation -> reservation.Status <> "cancelled" && day.Date >= reservation.ArrivalDate.Date && day.Date < reservation.DepartureDate.Date)
                |> List.sortBy (fun reservation -> reservation.ArrivalDate)

            let totalBeds = rooms |> List.sumBy (fun room -> room.NumberOfBeds)
            let occupiedBeds =
                dayReservations
                |> List.sumBy (fun reservation -> max 1 reservation.ReservedBeds)

            let availableBeds = max 0 (totalBeds - occupiedBeds)
            let occupancyPercent =
                if totalBeds = 0 then 0M else (decimal occupiedBeds * 100M) / decimal totalBeds

            let status =
                if occupancyPercent >= 100M then "red"
                elif occupancyPercent >= 80M then "orange"
                else "green"

            { Date = day.Date
              InMonth = day.Month = referenceDate.Month
              Events = List.ofSeq dayEvents
              Reservations = dayReservations
              OccupiedBeds = occupiedBeds
              AvailableBeds = availableBeds
              TotalBeds = totalBeds
              OccupancyPercent = occupancyPercent
              Status = status })

    let BuildMonthView (referenceDate: DateTime) (events: List<CalendarEvent>) (rooms: List<Room>) (reservations: List<Reservation>) =
        let monthStart = DateTime(referenceDate.Year, referenceDate.Month, 1)
        let culture = CultureInfo("fr-FR")

        { ReferenceDate = monthStart
          MonthLabel = monthStart.ToString("MMMM yyyy", culture)
          Days = BuildMonthGrid monthStart events rooms reservations
          Events = GetMonthEvents monthStart events }

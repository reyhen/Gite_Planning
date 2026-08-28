namespace Gite_Planning.Models

open System

[<CLIMutable>]
type CalendarEvent =
    { Id: int
      Title: string
      Description: string
      Start: DateTime
      End: DateTime }

[<CLIMutable>]
type CalendarDay =
    { Date: DateTime
      InMonth: bool
      Events: CalendarEvent list
      Reservations: Reservation list
      OccupiedBeds: int
      AvailableBeds: int
      TotalBeds: int
      OccupancyPercent: decimal
      Status: string }

[<CLIMutable>]
type CalendarMonthView =
    { ReferenceDate: DateTime
      MonthLabel: string
      Days: CalendarDay list
      Events: CalendarEvent list }

[<CLIMutable>]
type CalendarEventForm =
    { Title: string
      Description: string
      Date: string
      Time: string
      DurationMinutes: int }

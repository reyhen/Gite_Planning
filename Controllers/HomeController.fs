namespace Gite_Planning.Controllers

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open System.Diagnostics

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

open Gite_Planning.Calendar
open Gite_Planning.Models
open Gite_Planning.Services

type HomeController (logger : ILogger<HomeController>) =
    inherit Controller()

    member this.Index () =
        let today = DateTime.Today
        
        // Récupérer les paramètres de query
        let queryYear = this.HttpContext.Request.Query.["year"]
        let queryMonth = this.HttpContext.Request.Query.["month"]
        
        let displayYear = 
            if System.String.IsNullOrEmpty(queryYear) then today.Year
            else
                match System.Int32.TryParse(queryYear.ToString()) with
                | (true, y) -> y
                | (false, _) -> today.Year
        
        let displayMonth = 
            if System.String.IsNullOrEmpty(queryMonth) then today.Month
            else
                match System.Int32.TryParse(queryMonth.ToString()) with
                | (true, m) -> if m >= 1 && m <= 12 then m else today.Month
                | (false, _) -> today.Month
        
        let referenceDate = DateTime(displayYear, displayMonth, 1)
        
        // Événements en dur pour le test (À améliorer: récupérer du localStorage ou d'une BD)
        let events = []
        
        let rooms = HotelDataService.getRooms()
        let reservations = HotelDataService.getReservations()
        let model = CalendarService.BuildMonthView referenceDate events rooms reservations
        this.View("Calendar", model)

    member this.Privacy () =
        this.View()

    [<ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)>]
    member this.Error () =
        let reqId = 
            if isNull Activity.Current then
                this.HttpContext.TraceIdentifier
            else
                Activity.Current.Id

        this.View({ RequestId = reqId })

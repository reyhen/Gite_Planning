namespace Gite_Planning.Controllers

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Gite_Planning.Models
open Gite_Planning.Services

[<Route("Hotel")>]
type HotelController(logger: ILogger<HotelController>) =
    inherit Controller()
    
    // ====== ROOMS ======
    
    [<HttpGet("Rooms")>]
    member this.Rooms() : IActionResult =
        let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
        this.View(rooms) :> IActionResult
    
    [<HttpGet("RoomDetail/{id:int}")>]
    member this.RoomDetail(id: int) : IActionResult =
        match HotelDataService.getRoomById(id) with
        | Some room ->
            let reservations = 
                new System.Collections.Generic.List<Reservation>(
                    HotelDataService.getReservationsByRoom(id)
                )

            let model = Tuple.Create(room, reservations)

            this.View("RoomDetail", model) :> IActionResult
            
        | None ->
            this.NotFound() :> IActionResult
    
    [<HttpGet("CreateRoom")>]
    member this.CreateRoom() : IActionResult =
        this.View() :> IActionResult
    
    [<HttpPost("CreateRoom")>]
    member this.CreateRoom(form: RoomForm) : IActionResult =
        try
            HotelDataService.addRoom(form) |> ignore
            this.RedirectToAction("Rooms") :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error creating room")
            this.View(form) :> IActionResult
    
    [<HttpGet("EditRoom/{id:int}")>]
    member this.EditRoom(id: int) : IActionResult =
        match HotelDataService.getRoomById(id) with
        | Some room ->
            let form: RoomForm =
                { Name = room.Name
                  NumberOfBeds = room.NumberOfBeds
                  SemiPensionPrice = room.SemiPensionPrice
                  NightWithBreakfastPrice = room.NightWithBreakfastPrice
                  NightWithMealPrice = room.NightWithMealPrice
                  SimpleNightPrice = room.SimpleNightPrice
                  Description = room.Description }
            this.ViewData.["RoomId"] <- id
            this.View("CreateRoom", form) :> IActionResult
        | None ->
            this.NotFound() :> IActionResult
    
    [<HttpPost("EditRoom/{id:int}")>]
    member this.EditRoom(id: int, form: RoomForm) : IActionResult =
        try
            if HotelDataService.updateRoom(id, form) then
                this.RedirectToAction("Rooms") :> IActionResult
            else
                this.NotFound() :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error updating room")
            this.View("CreateRoom", form) :> IActionResult
    
    [<HttpPost("DeleteRoom")>]
    member this.DeleteRoom([<FromForm>] id: int) : IActionResult =
        try
            if HotelDataService.deleteRoom(id) then
                this.RedirectToAction("Rooms") :> IActionResult
            else
                this.BadRequest() :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error deleting room")
            this.BadRequest() :> IActionResult

    [<HttpPost("ClearRooms")>]
    member this.ClearRooms() : IActionResult =
        try
            HotelDataService.clearRooms()
            this.RedirectToAction("Rooms") :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error clearing rooms")
            this.BadRequest() :> IActionResult

    [<HttpGet("CompanySettings")>]
    member this.CompanySettings() : IActionResult =
        let settings = HotelDataService.getCompanySettings()
        this.View(settings) :> IActionResult

    [<HttpPost("CompanySettings")>]
    member this.CompanySettings(form: CompanySettings, [<FromForm>] logoFile: IFormFile) : IActionResult =
        try
            let resolvedLogoUrl =
                match HotelDataService.saveUploadedCompanyLogo logoFile with
                | Some uploadedLogo -> uploadedLogo
                | None -> form.LogoImageUrl

            let updatedForm = { form with LogoImageUrl = resolvedLogoUrl }
            let saved = HotelDataService.saveCompanySettings(updatedForm)
            this.TempData.["SuccessMessage"] <- "Paramètres de l'entreprise enregistrés."
            this.View(saved) :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error updating company settings")
            this.View(form) :> IActionResult

    [<HttpGet("Hosts")>]
    member this.Hosts() : IActionResult =
        let hosts = new System.Collections.Generic.List<Host>(HotelDataService.getHosts())
        this.View(hosts) :> IActionResult

    [<HttpPost("CreateHost")>]
    member this.CreateHost(form: HostForm) : IActionResult =
        if not (String.IsNullOrWhiteSpace(form.Name) || String.IsNullOrWhiteSpace(form.PhoneNumber) || String.IsNullOrWhiteSpace(form.Email) || String.IsNullOrWhiteSpace(form.Address)) then
            HotelDataService.addHost(form) |> ignore
        this.RedirectToAction("Hosts") :> IActionResult

    [<HttpGet("EditHost/{id:int}")>]
    member this.EditHost(id: int) : IActionResult =
        match HotelDataService.getHostById(id) with
        | Some host ->
            let form =
                { Name = host.Name
                  PhoneNumber = host.PhoneNumber
                  Email = host.Email
                  Address = host.Address }
            this.ViewData.["HostId"] <- id
            this.View(form) :> IActionResult
        | None -> this.NotFound() :> IActionResult

    [<HttpPost("EditHost/{id:int}")>]
    member this.EditHost(id: int, form: HostForm) : IActionResult =
        if String.IsNullOrWhiteSpace(form.Name) || String.IsNullOrWhiteSpace(form.PhoneNumber) || String.IsNullOrWhiteSpace(form.Email) || String.IsNullOrWhiteSpace(form.Address) then
            this.ViewData.["HostId"] <- id
            this.ViewData.["ErrorMessage"] <- "Veuillez remplir tous les champs de l'hébergement."
            this.View(form) :> IActionResult
        elif HotelDataService.updateHost(id, form) then
            this.RedirectToAction("Hosts") :> IActionResult
        else
            this.NotFound() :> IActionResult

    [<HttpPost("DeleteHost")>]
    member this.DeleteHost([<FromForm>] id: int) : IActionResult =
        HotelDataService.deleteHost(id) |> ignore
        this.RedirectToAction("Hosts") :> IActionResult

    [<HttpPost("ClearHosts")>]
    member this.ClearHosts() : IActionResult =
        try
            HotelDataService.clearHosts()
            this.RedirectToAction("Hosts") :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error clearing hosts")
            this.BadRequest() :> IActionResult
    
    // ====== RESERVATIONS ======
    
    [<HttpGet("ExportReservationsCsv")>]
    member this.ExportReservationsCsv() : IActionResult =
        let reservations = HotelDataService.getReservations() |> List.sortByDescending (fun r -> r.ArrivalDate)
        let csv = HotelDataService.exportReservationsCsv reservations
        let bytes = System.Text.Encoding.UTF8.GetBytes(csv)
        this.File(bytes, "text/csv", "reservations.csv") :> IActionResult

    [<HttpPost("ImportReservationsCsv")>]
    member this.ImportReservationsCsv([<FromForm>] file: IFormFile) : IActionResult =
        try
            if isNull file || file.Length = 0L then
                this.TempData. ["ErrorMessage"] <- "Veuillez sélectionner un fichier CSV à importer."
            else
                use stream = file.OpenReadStream()
                use reader = new System.IO.StreamReader(stream)
                let csvText = reader.ReadToEnd()
                let importedReservations = HotelDataService.importReservationsCsv csvText
                if importedReservations.IsEmpty then
                    this.TempData. ["ErrorMessage"] <- "Le fichier CSV est invalide ou vide."
                else
                    HotelDataService.saveReservations importedReservations
                    this.TempData. ["SuccessMessage"] <- sprintf "%d réservation(s) importée(s) avec succès." importedReservations.Length
            this.RedirectToAction("Reservations") :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error importing reservations CSV")
            this.TempData. ["ErrorMessage"] <- "Une erreur est survenue lors de l'import CSV."
            this.RedirectToAction("Reservations") :> IActionResult

    [<HttpPost("ClearReservations")>]
    member this.ClearReservations() : IActionResult =
        try
            HotelDataService.clearReservations()
            this.RedirectToAction("Reservations") :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error clearing reservations")
            this.BadRequest() :> IActionResult

    [<HttpGet("Reservations")>]
    member this.Reservations([<FromQuery>] day: string, [<FromQuery>] roomId: Nullable<int>, [<FromQuery>] fromDate: string, [<FromQuery>] toDate: string, [<FromQuery>] name: string) : IActionResult =
        let rooms = HotelDataService.getRooms()
        let allReservations = HotelDataService.getReservations()

        let parseDate (value: string) : DateTime option =
            if String.IsNullOrWhiteSpace(value) then
                None
            else
                let formats = [| "dd/MM/yyyy"; "dd/MM"; "yyyy-MM-dd"; "yyyy/MM/dd"; "d/M/yyyy"; "d/M" |]
                let success, parsed = DateTime.TryParseExact(value.Trim(), formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None)
                if success then Some parsed.Date else None

        let normalizedName = if String.IsNullOrWhiteSpace(name) then "" else name.Trim()

        let filteredReservations =
            allReservations
            |> List.filter (fun reservation ->
                let matchesDay =
                    match parseDate day with
                    | Some selectedDay -> reservation.ArrivalDate.Date <= selectedDay.Date && reservation.DepartureDate.Date >= selectedDay.Date
                    | None -> true

                let matchesRoom =
                    if roomId.HasValue then reservation.RoomId = roomId.Value else true

                let matchesFrom =
                    match parseDate fromDate with
                    | Some startDate -> reservation.DepartureDate.Date >= startDate.Date
                    | None -> true

                let matchesTo =
                    match parseDate toDate with
                    | Some endDate -> reservation.ArrivalDate.Date <= endDate.Date
                    | None -> true

                let matchesName =
                    if String.IsNullOrWhiteSpace(normalizedName) then true
                    else
                        let fullName = sprintf "%s %s" reservation.FirstName reservation.LastName
                        fullName.Contains(normalizedName, StringComparison.OrdinalIgnoreCase)
                        || reservation.FirstName.Contains(normalizedName, StringComparison.OrdinalIgnoreCase)
                        || reservation.LastName.Contains(normalizedName, StringComparison.OrdinalIgnoreCase)

                matchesDay && matchesRoom && matchesFrom && matchesTo && matchesName)
            |> List.sortByDescending (fun reservation -> reservation.ArrivalDate)

        let selectedRoomId = if roomId.HasValue then roomId.Value else 0
        this.ViewData.["SelectedDay"] <- day
        this.ViewData.["SelectedRoomId"] <- selectedRoomId
        this.ViewData.["SelectedFromDate"] <- fromDate
        this.ViewData.["SelectedToDate"] <- toDate
        this.ViewData.["SelectedName"] <- name

        let reservations = new System.Collections.Generic.List<Reservation>(filteredReservations)
        let roomList = new System.Collections.Generic.List<Room>(rooms)
        let model = new System.ValueTuple<System.Collections.Generic.List<Reservation>, System.Collections.Generic.List<Room>>(reservations, roomList)
        this.View(model) :> IActionResult
    
    [<HttpGet("CreateReservation")>]
    member this.CreateReservation() : IActionResult =
        let today = DateTime.Today
        let selectedDate =
            match this.HttpContext.Request.Query. ["date"].ToString() with
            | value when String.IsNullOrWhiteSpace(value) -> today.ToString("yyyy-MM-dd")
            | value -> value

        let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
        this.ViewData.["Rooms"] <- rooms
        this.ViewData.["SelectedDate"] <- selectedDate
        let model =
            { FirstName = ""
              LastName = ""
              PhoneNumber = ""
              Email = ""
              ArrivalDate = selectedDate
              DepartureDate = selectedDate
              NumberOfNights = 1
              ReservedBeds = 1
              RoomId = 0
              PriceType = "semi-pension"
              Comment = ""
              AdditionalRoomId = 0
              AdditionalReservedBeds = 0
              AdditionalRoomId2 = 0
              AdditionalReservedBeds2 = 0 }
        this.View(model) :> IActionResult
    
    [<HttpPost("CreateReservation")>]
    member this.CreateReservation(form: ReservationForm) : IActionResult =
        try
            if form.RoomId <= 0 then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["Error"] <- "Veuillez sélectionner une chambre disponible."
                this.View(form) :> IActionResult
            elif String.IsNullOrWhiteSpace(form.FirstName) || String.IsNullOrWhiteSpace(form.LastName) then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["Error"] <- "Veuillez remplir le prénom et le nom du client."
                this.View(form) :> IActionResult
            elif String.IsNullOrWhiteSpace(form.PhoneNumber) && String.IsNullOrWhiteSpace(form.Email) then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["Error"] <- "Veuillez saisir au moins un moyen de contact : téléphone ou e-mail."
                this.View(form) :> IActionResult
            elif (form.AdditionalRoomId > 0 && form.AdditionalReservedBeds <= 0) || (form.AdditionalRoomId2 > 0 && form.AdditionalReservedBeds2 <= 0) then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["Error"] <- "Si vous ajoutez une chambre supplémentaire, indiquez le nombre de lits dans cette chambre."
                this.View(form) :> IActionResult
            else
                match HotelDataService.addReservation(form) with
                | Some _ ->
                    this.RedirectToAction("Reservations") :> IActionResult
                | None ->
                    let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                    this.ViewData.["Rooms"] <- rooms
                    this.ViewData.["SelectedDate"] <- form.ArrivalDate
                    this.ViewData.["Error"] <- "Les chambres disponibles ne couvrent pas la demande pour ces dates. Réduisez le nombre de lits ou choisissez une autre chambre."
                    this.View(form) :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error creating reservation")
            let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
            this.ViewData.["Rooms"] <- rooms
            this.ViewData.["SelectedDate"] <- form.ArrivalDate
            this.View(form) :> IActionResult
    
    [<HttpGet("EditReservation/{id:int}")>]
    member this.EditReservation(id: int) : IActionResult =
        let reservations = HotelDataService.getReservations()
        let reservation = reservations |> List.tryFind (fun r -> r.Id = id)
        
        match reservation with
        | Some r ->
            // Récupérer toutes les réservations du groupe
            let groupReservations = 
                if r.GroupId = 0 then
                    [r]
                else
                    reservations |> List.filter (fun x -> x.GroupId = r.GroupId) |> List.sortBy (fun x -> x.Id)
            
            let firstRes = groupReservations |> List.head
            let secondRes = groupReservations |> List.tryItem 1
            
            let thirdRes = groupReservations |> List.tryItem 2
            let form: ReservationForm =
                { FirstName = firstRes.FirstName
                  LastName = firstRes.LastName
                  PhoneNumber = firstRes.PhoneNumber
                  Email = firstRes.Email
                  ArrivalDate = firstRes.ArrivalDate.ToString("yyyy-MM-dd")
                  DepartureDate = firstRes.DepartureDate.ToString("yyyy-MM-dd")
                  NumberOfNights = firstRes.NumberOfNights
                  ReservedBeds = firstRes.ReservedBeds
                  RoomId = firstRes.RoomId
                  PriceType = firstRes.PriceType
                  Comment = firstRes.Comment
                  AdditionalRoomId = (match secondRes with Some s -> s.RoomId | None -> 0)
                  AdditionalReservedBeds = (match secondRes with Some s -> s.ReservedBeds | None -> 0)
                  AdditionalRoomId2 = (match thirdRes with Some s -> s.RoomId | None -> 0)
                  AdditionalReservedBeds2 = (match thirdRes with Some s -> s.ReservedBeds | None -> 0) }
            
            let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
            this.ViewData.["Rooms"] <- rooms
            this.ViewData.["ReservationId"] <- id
            this.View("CreateReservation", form) :> IActionResult
        | None ->
            this.NotFound() :> IActionResult
    
    [<HttpPost("EditReservation/{id:int}")>]
    member this.EditReservation(id: int, form: ReservationForm) : IActionResult =
        try
            if form.RoomId <= 0 then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["ReservationId"] <- id
                this.ViewData.["Error"] <- "Veuillez sélectionner une chambre disponible."
                this.View("CreateReservation", form) :> IActionResult
            elif String.IsNullOrWhiteSpace(form.FirstName) || String.IsNullOrWhiteSpace(form.LastName) then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["ReservationId"] <- id
                this.ViewData.["Error"] <- "Veuillez remplir le prénom et le nom du client."
                this.View("CreateReservation", form) :> IActionResult
            elif String.IsNullOrWhiteSpace(form.PhoneNumber) && String.IsNullOrWhiteSpace(form.Email) then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["ReservationId"] <- id
                this.ViewData.["Error"] <- "Veuillez saisir au moins un moyen de contact : téléphone ou e-mail."
                this.View("CreateReservation", form) :> IActionResult
            elif (form.AdditionalRoomId > 0 && form.AdditionalReservedBeds <= 0) || (form.AdditionalRoomId2 > 0 && form.AdditionalReservedBeds2 <= 0) then
                let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                this.ViewData.["Rooms"] <- rooms
                this.ViewData.["SelectedDate"] <- form.ArrivalDate
                this.ViewData.["ReservationId"] <- id
                this.ViewData.["Error"] <- "Si vous ajoutez une chambre supplémentaire, indiquez le nombre de lits dans cette chambre."
                this.View("CreateReservation", form) :> IActionResult
            else
                match HotelDataService.updateReservation(id, form) with
                | Some _ ->
                    this.RedirectToAction("Reservations") :> IActionResult
                | None ->
                    let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
                    this.ViewData.["Rooms"] <- rooms
                    this.ViewData.["SelectedDate"] <- form.ArrivalDate
                    this.ViewData.["ReservationId"] <- id
                    this.ViewData.["Error"] <- "Impossible de mettre à jour la réservation. Vérifiez la disponibilité des chambres pour les dates choisies."
                    this.View("CreateReservation", form) :> IActionResult
        with
        | ex ->
            logger.LogError(ex, "Error updating reservation")
            let rooms = new System.Collections.Generic.List<Room>(HotelDataService.getRooms())
            this.ViewData.["Rooms"] <- rooms
            this.ViewData.["SelectedDate"] <- form.ArrivalDate
            this.View("CreateReservation", form) :> IActionResult
    
    [<HttpPost("CancelReservation")>]
    member this.CancelReservation([<FromForm>] id: int) : IActionResult =
        try
            printfn "=== CANCEL RESERVATION ==="
            printfn "ID reçu : %d" id

            let result = HotelDataService.cancelReservation(id)

            printfn "Résultat annulation : %b" result

            if result then
                this.RedirectToAction("Reservations") :> IActionResult
            else
                this.BadRequest("La réservation n'a pas pu être annulée.") :> IActionResult

        with
        | ex ->
            logger.LogError(ex, "Error cancelling reservation")
            this.BadRequest("Erreur lors de l'annulation : " + ex.Message) :> IActionResult



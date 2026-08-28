namespace Gite_Planning.Services

open System
open System.IO
open System.Text.Json
open System.Globalization
open Microsoft.AspNetCore.Http
open Gite_Planning.Models

module HotelDataService =

    // ============================
    // FICHIERS / DOSSIERS
    // ============================

    let private dataDir = "wwwroot/data"
    let private roomsFile = Path.Combine(dataDir, "rooms.json")
    let private reservationsFile = Path.Combine(dataDir, "reservations.json")
    let private hostsFile = Path.Combine(dataDir, "hosts.json")
    let private companySettingsFile = Path.Combine(dataDir, "company-settings.json")
    let private logoUploadDir = Path.Combine("wwwroot", "images", "uploads")

    let private jsonOptions =
        JsonSerializerOptions(
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        )

    let private ensureDataDir() =
        if not (Directory.Exists(dataDir)) then
            Directory.CreateDirectory(dataDir) |> ignore

    let private ensureLogoUploadDir() =
        if not (Directory.Exists(logoUploadDir)) then
            Directory.CreateDirectory(logoUploadDir) |> ignore

        logoUploadDir

    // ============================
    // JSON
    // ============================

    let private readJsonFile<'T> (filePath: string) : 'T list =
        try
            if File.Exists(filePath) then
                let json = File.ReadAllText(filePath)

                if String.IsNullOrWhiteSpace(json) then
                    []
                else
                    let result =
                        JsonSerializer.Deserialize<'T list>(
                            json,
                            jsonOptions
                        )

                    if Object.ReferenceEquals(box result, null) then
                        []
                    else
                        result
            else
                []
        with
        | ex ->
            printfn "Erreur lecture JSON [%s] : %s" filePath ex.Message
            []

    let private writeJsonFile<'T> (filePath: string) (data: 'T list) =
        ensureDataDir()

        let json =
            JsonSerializer.Serialize(data, jsonOptions)

        let temporaryFile =
            filePath + ".tmp"

        try
            File.WriteAllText(temporaryFile, json)

            if File.Exists(filePath) then
                File.Replace(temporaryFile, filePath, null)
            else
                File.Move(temporaryFile, filePath)

        with
        | ex ->
            if File.Exists(temporaryFile) then
                try
                    File.Delete(temporaryFile)
                with
                | _ -> ()

            printfn "Erreur écriture JSON [%s] : %s" filePath ex.Message
            raise ex

    // ============================
    // LOGO
    // ============================

    let saveUploadedCompanyLogo (file: IFormFile) : string option =
        if isNull file || file.Length <= 0L then
            None
        else
            // Limite de 5 Mo
            let maxFileSize =
                5L * 1024L * 1024L

            if file.Length > maxFileSize then
                None
            else
                let allowedExtensions =
                    [ ".png"; ".jpg"; ".jpeg"; ".gif"; ".svg"; ".webp" ]

                let extension =
                    Path.GetExtension(file.FileName)

                if
                    String.IsNullOrWhiteSpace(extension)
                    || not (
                        allowedExtensions
                        |> List.exists (fun ext ->
                            String.Equals(
                                ext,
                                extension,
                                StringComparison.OrdinalIgnoreCase
                            ))
                    )
                then
                    None
                else
                    try
                        let directory =
                            ensureLogoUploadDir()

                        let uniqueFileName =
                            sprintf "%s%s"
                                (Guid.NewGuid().ToString("N"))
                                (extension.ToLowerInvariant())

                        let fullPath =
                            Path.Combine(directory, uniqueFileName)

                        use stream = file.OpenReadStream()
                        use target = File.Create(fullPath)

                        stream.CopyTo(target)

                        Some ("/images/uploads/" + uniqueFileName)

                    with
                    | ex ->
                        printfn "Erreur upload logo : %s" ex.Message
                        None

    let normalizeLogoImageUrl (value: string) =
        let normalized =
            if isNull value then
                ""
            else
                value.Trim()

        let lowered =
            normalized.ToLowerInvariant()

        if
            lowered.StartsWith("http://")
            || lowered.StartsWith("https://")
            || lowered.StartsWith("data:")
        then
            normalized

        elif normalized.StartsWith("~/") then
            "/" + normalized.Substring(2).TrimStart('/')

        elif normalized.StartsWith("/") then
            normalized

        elif
            normalized.StartsWith("images/")
            || normalized.StartsWith("wwwroot/")
        then
            "/" + normalized.TrimStart('/').Replace("wwwroot/", "")

        else
            "/images/logo-hotelia.svg"

    // ============================
    // CSV ESCAPE
    // ============================

    let escapeCsvField (value: string) =
        if String.IsNullOrWhiteSpace(value) then
            ""
        else
            let escaped =
                value.Replace("\"", "\"\"")

            if
                escaped.Contains(";")
                || escaped.Contains(",")
                || escaped.Contains("\"")
                || escaped.Contains("\n")
                || escaped.Contains("\r")
            then
                sprintf "\"%s\"" escaped
            else
                escaped

    // ============================
    // CSV EXPORT
    // ============================

    let exportReservationsCsv (reservations: Reservation list) : string =

        let header =
            "Id;GroupId;RoomId;RoomName;FirstName;LastName;PhoneNumber;Email;ArrivalDate;DepartureDate;NumberOfNights;ReservedBeds;PriceType;TotalPrice;Comment;Status;CreatedAt;UpdatedAt"

        let rows =
            reservations
            |> List.map (fun r ->
                String.concat ";"
                    [
                        r.Id.ToString()
                        r.GroupId.ToString()
                        r.RoomId.ToString()
                        escapeCsvField r.RoomName
                        escapeCsvField r.FirstName
                        escapeCsvField r.LastName
                        escapeCsvField r.PhoneNumber
                        escapeCsvField r.Email
                        r.ArrivalDate.ToString("yyyy-MM-dd")
                        r.DepartureDate.ToString("yyyy-MM-dd")
                        r.NumberOfNights.ToString()
                        r.ReservedBeds.ToString()
                        escapeCsvField r.PriceType
                        r.TotalPrice.ToString(CultureInfo.InvariantCulture)
                        escapeCsvField r.Comment
                        escapeCsvField r.Status
                        r.CreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                        r.UpdatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                    ])

        String.concat "\n" (header :: rows)

    // ============================
    // CSV PARSE
    // ============================

    let private parseCsvLine
        (delimiter: char)
        (line: string)
        : string list =

        let results =
            System.Collections.Generic.List<string>()

        let mutable current = ""
        let mutable inQuotes = false
        let mutable index = 0

        while index < line.Length do
            let character =
                line.[index]

            if character = '"' then
                if
                    inQuotes
                    && index + 1 < line.Length
                    && line.[index + 1] = '"'
                then
                    current <- current + "\""
                    index <- index + 2
                else
                    inQuotes <- not inQuotes
                    index <- index + 1

            elif character = delimiter && not inQuotes then
                results.Add(current)
                current <- ""
                index <- index + 1

            elif character = '\r' then
                index <- index + 1

            else
                current <- current + string character
                index <- index + 1

        results.Add(current)

        results
        |> Seq.toList
        |> List.map (fun value -> value.Trim())

    // ============================
    // CSV DATE
    // ============================

    let private parseCsvDate (value: string) : DateTime =

        let trimmedValue =
            if isNull value then
                ""
            else
                value.Trim()

        if String.IsNullOrWhiteSpace(trimmedValue) then
            DateTime.UtcNow
        else
            let formats =
                [|
                    "dd/MM/yyyy"
                    "dd/MM"
                    "yyyy-MM-dd"
                    "yyyy/MM/dd"
                    "d/M/yyyy"
                    "d/M"
                    "M/d/yyyy"
                    "yyyy-MM-ddTHH:mm:ssZ"
                    "yyyy-MM-ddTHH:mm:ss"
                    "O"
                    "o"
                    "s"
                |]

            let mutable parsed =
                DateTime.MinValue

            let styles =
                DateTimeStyles.AllowWhiteSpaces
                ||| DateTimeStyles.RoundtripKind

            if
                DateTime.TryParseExact(
                    trimmedValue,
                    formats,
                    CultureInfo.InvariantCulture,
                    styles,
                    &parsed
                )
            then
                parsed

            elif
                DateTime.TryParse(
                    trimmedValue,
                    CultureInfo.InvariantCulture,
                    styles,
                    &parsed
                )
            then
                parsed

            else
                DateTime.UtcNow

    // ============================
    // CSV IMPORT
    // ============================

    let importReservationsCsv (csvText: string) : Reservation list =

        if String.IsNullOrWhiteSpace(csvText) then
            []
        else
            let lines =
                csvText.Split(
                    [| "\r\n"; "\n" |],
                    StringSplitOptions.RemoveEmptyEntries
                )
                |> Array.toList

            match lines with
            | [] ->
                []

            | header :: rest ->

                let delimiter =
                    let semicolonCount =
                        header
                        |> Seq.filter ((=) ';')
                        |> Seq.length

                    let commaCount =
                        header
                        |> Seq.filter ((=) ',')
                        |> Seq.length

                    if semicolonCount >= commaCount then
                        ';'
                    else
                        ','

                let parseInt (v: string) =
                    let mutable parsed = 0

                    if
                        Int32.TryParse(
                            v.Trim(),
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            &parsed
                        )
                    then
                        parsed
                    else
                        0

                let parseDecimal (v: string) =
                    let mutable parsed = 0M

                    if
                        Decimal.TryParse(
                            v.Trim(),
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            &parsed
                        )
                    then
                        parsed

                    elif
                        Decimal.TryParse(
                            v.Trim(),
                            NumberStyles.Any,
                            CultureInfo.CurrentCulture,
                            &parsed
                        )
                    then
                        parsed

                    else
                        0M

                rest
                |> List.choose (fun line ->
                    try
                        let values =
                            parseCsvLine delimiter line

                        match values.Length with

                        // ============================
                        // NOUVEAU FORMAT : 18 COLONNES
                        // Id;GroupId;RoomId;RoomName;...
                        // ============================

                        | count when count >= 18 ->

                            let importedRoomId =
                                parseInt values.[2]

                            let importedRoomName =
                                values.[3].Trim()

                            let rooms =
                                readJsonFile<Room> roomsFile

                            let resolvedRoomId =
                                if importedRoomId > 0 then
                                    importedRoomId
                                elif not (String.IsNullOrWhiteSpace(importedRoomName)) then
                                    rooms
                                    |> List.tryFind (fun room ->
                                        room.Name.Trim().Equals(
                                            importedRoomName.Trim(),
                                            StringComparison.OrdinalIgnoreCase
                                        ))
                                    |> Option.map (fun room -> room.Id)
                                    |> Option.defaultValue 0
                                else
                                    0

                            let room =
                                rooms
                                |> List.tryFind (fun r ->
                                    r.Id = resolvedRoomId)

                            let numberOfNights =
                                parseInt values.[10]

                            let reservedBeds =
                                parseInt values.[11]

                            let priceType =
                                values.[12].Trim()

                            let totalPrice =
                                match room with
                                | Some room ->
                                    let price =
                                        match priceType with
                                        | "semi-pension" ->
                                            room.SemiPensionPrice

                                        | "night-breakfast" ->
                                            room.NightWithBreakfastPrice

                                        | "night-meal" ->
                                            room.NightWithMealPrice

                                        | "simple-night" ->
                                            room.SimpleNightPrice

                                        | _ ->
                                            0M

                                    price
                                    * decimal numberOfNights
                                    * decimal reservedBeds

                                | None ->
                                    0M

                            Some {
                                Id = parseInt values.[0]
                                GroupId = parseInt values.[1]
                                RoomId = resolvedRoomId
                                RoomName = importedRoomName
                                FirstName = values.[4]
                                LastName = values.[5]
                                PhoneNumber = values.[6]
                                Email = values.[7]
                                ArrivalDate = parseCsvDate values.[8]
                                DepartureDate = parseCsvDate values.[9]
                                NumberOfNights = numberOfNights
                                ReservedBeds = reservedBeds
                                PriceType = priceType
                                TotalPrice = totalPrice
                                Comment = values.[14]
                                Status = values.[15]
                                CreatedAt = parseCsvDate values.[16]
                                UpdatedAt = parseCsvDate values.[17]
                            }

                        // ============================
                        // ANCIEN FORMAT : 17 COLONNES
                        // Id;RoomId;RoomName;...
                        // ============================

                        | count when count >= 17 ->

                            let importedRoomId =
                                parseInt values.[1]

                            let importedRoomName =
                                values.[2].Trim()

                            let rooms =
                                readJsonFile<Room> roomsFile

                            let resolvedRoomId =
                                if importedRoomId > 0 then
                                    importedRoomId
                                elif not (String.IsNullOrWhiteSpace(importedRoomName)) then
                                    rooms
                                    |> List.tryFind (fun room ->
                                        room.Name.Trim().Equals(
                                            importedRoomName,
                                            StringComparison.OrdinalIgnoreCase
                                        ))
                                    |> Option.map (fun room -> room.Id)
                                    |> Option.defaultValue 0
                                else
                                    0

                            let room =
                                rooms
                                |> List.tryFind (fun r ->
                                    r.Id = resolvedRoomId)

                            let numberOfNights =
                                parseInt values.[9]

                            let reservedBeds =
                                parseInt values.[10]

                            let priceType =
                                values.[11].Trim()

                            let totalPrice =
                                match room with
                                | Some room ->
                                    let price =
                                        match priceType with
                                        | "semi-pension" ->
                                            room.SemiPensionPrice

                                        | "night-breakfast" ->
                                            room.NightWithBreakfastPrice

                                        | "night-meal" ->
                                            room.NightWithMealPrice

                                        | "simple-night" ->
                                            room.SimpleNightPrice

                                        | _ ->
                                            0M

                                    price
                                    * decimal numberOfNights
                                    * decimal reservedBeds

                                | None ->
                                    0M

                            Some {
                                Id = parseInt values.[0]
                                GroupId = 0
                                RoomId = resolvedRoomId
                                RoomName = importedRoomName
                                FirstName = values.[3]
                                LastName = values.[4]
                                PhoneNumber = values.[5]
                                Email = values.[6]
                                ArrivalDate = parseCsvDate values.[7]
                                DepartureDate = parseCsvDate values.[8]
                                NumberOfNights = numberOfNights
                                ReservedBeds = reservedBeds
                                PriceType = priceType
                                TotalPrice = totalPrice
                                Comment = values.[13]
                                Status = values.[14]
                                CreatedAt = parseCsvDate values.[15]
                                UpdatedAt = parseCsvDate values.[16]
                            }

                        | _ ->
                            None

                    with
                    | ex ->
                        printfn "Erreur import CSV : %s" ex.Message
                        None)

    // ============================
    // ROOMS
    // ============================

    let getRooms() : Room list =
        readJsonFile<Room> roomsFile

    let getRoomById(id: int) : Room option =
        getRooms()
        |> List.tryFind (fun r -> r.Id = id)

    let addRoom(room: RoomForm) : Room =

        let rooms =
            getRooms()

        let nextId =
            if List.isEmpty rooms then
                1
            else
                rooms
                |> List.map (fun r -> r.Id)
                |> List.max
                |> (+) 1

        let newRoom : Room =
            {
                Id = nextId
                Name = room.Name.Trim()
                NumberOfBeds = max 1 room.NumberOfBeds
                SemiPensionPrice = max 0M room.SemiPensionPrice
                NightWithBreakfastPrice =
                    max 0M room.NightWithBreakfastPrice
                NightWithMealPrice =
                    max 0M room.NightWithMealPrice
                SimpleNightPrice =
                    max 0M room.SimpleNightPrice
                Description =
                    if isNull room.Description then
                        ""
                    else
                        room.Description.Trim()
                IsActive = true
                CreatedAt = DateTime.UtcNow
            }

        writeJsonFile roomsFile (rooms @ [ newRoom ])

        newRoom

    let updateRoom(id: int, room: RoomForm) : bool =

        let rooms =
            getRooms()

        if rooms |> List.exists (fun r -> r.Id = id) then

            let updated =
                rooms
                |> List.map (fun r ->
                    if r.Id = id then
                        {
                            r with
                                Name = room.Name.Trim()
                                NumberOfBeds =
                                    max 1 room.NumberOfBeds
                                SemiPensionPrice =
                                    max 0M room.SemiPensionPrice
                                NightWithBreakfastPrice =
                                    max 0M room.NightWithBreakfastPrice
                                NightWithMealPrice =
                                    max 0M room.NightWithMealPrice
                                SimpleNightPrice =
                                    max 0M room.SimpleNightPrice
                                Description =
                                    if isNull room.Description then
                                        ""
                                    else
                                        room.Description.Trim()
                        }
                    else
                        r)

            writeJsonFile roomsFile updated
            true

        else
            false

    let deleteRoom(id: int) : bool =

        let rooms =
            getRooms()

        let filtered =
            rooms
            |> List.filter (fun r -> r.Id <> id)

        if List.length filtered < List.length rooms then
            writeJsonFile roomsFile filtered
            true
        else
            false

    let clearRooms() =
        writeJsonFile roomsFile []

    // ============================
    // HOSTS
    // ============================

    let private defaultHosts() : Host list =
        [
            {
                Id = 1
                Name = "Gîte d'étape l'Abeille Lulu à Lauzerte"
                PhoneNumber = "06 87 05 53 60"
                Email = "abeillelulu82@gmail.com"
                Address = "1, chemin de la Fontaine 82110 Lauzerte"
            }

            {
                Id = 2
                Name = "Gîte des Figuiers"
                PhoneNumber = "06 85 31 71 31"
                Email = "accueil@lesfiguiers-lauzerte.com"
                Address = "25 Chemin du Coudounié 82110 Lauzerte"
            }

            {
                Id = 3
                Name = "Gîte Fleuri du Tuc de Saint-Paul"
                PhoneNumber = "06 32 14 64 95"
                Email = "bourrieresmelanie@gmail.com"
                Address =
                    "269, route de Saint-Laurent Lolmie 82110 Lauzerte"
            }

            {
                Id = 4
                Name = "La Luciole du Chemin"
                PhoneNumber = "07-78-47-63-77"
                Email = "lalucioleduchemin@icloud.com"
                Address = "3 rue de la Brèche 82110 Lauzerte"
            }

            {
                Id = 5
                Name = "Gîte Chez Serge"
                PhoneNumber = "06-72-24-19-85"
                Email = "serge.pradin@orange.fr"
                Address = "32 rue de la Garrigue 82110 Lauzerte"
            }
        ]

    let getHosts() : Host list =
        if File.Exists(hostsFile) then
            readJsonFile<Host> hostsFile
        else
            defaultHosts()

    let getHostById(id: int) : Host option =
        getHosts()
        |> List.tryFind (fun host -> host.Id = id)

    let addHost(host: HostForm) : Host =

        let hosts =
            getHosts()

        let nextId =
            if List.isEmpty hosts then
                1
            else
                hosts
                |> List.map (fun h -> h.Id)
                |> List.max
                |> (+) 1

        let newHost =
            {
                Id = nextId
                Name = host.Name.Trim()
                PhoneNumber = host.PhoneNumber.Trim()
                Email = host.Email.Trim()
                Address = host.Address.Trim()
            }

        writeJsonFile hostsFile (hosts @ [ newHost ])

        newHost

    let deleteHost(id: int) : bool =

        let hosts =
            getHosts()

        let remaining =
            hosts
            |> List.filter (fun h -> h.Id <> id)

        if List.length remaining < List.length hosts then
            writeJsonFile hostsFile remaining
            true
        else
            false

    let updateHost(id: int, form: HostForm) : bool =

        let hosts =
            getHosts()

        if hosts |> List.exists (fun h -> h.Id = id) then

            let updated =
                hosts
                |> List.map (fun h ->
                    if h.Id = id then
                        {
                            h with
                                Name = form.Name.Trim()
                                PhoneNumber = form.PhoneNumber.Trim()
                                Email = form.Email.Trim()
                                Address = form.Address.Trim()
                        }
                    else
                        h)

            writeJsonFile hostsFile updated
            true

        else
            false

    let clearHosts() =
        writeJsonFile hostsFile []

    // ============================
    // RESERVATIONS
    // ============================

    let getReservations() : Reservation list =
        readJsonFile<Reservation> reservationsFile

    let saveReservations(reservations: Reservation list) =
        writeJsonFile reservationsFile reservations

    let clearReservations() =
        writeJsonFile reservationsFile []

    let getReservationsByRoom(roomId: int) : Reservation list =
        getReservations()
        |> List.filter (fun r -> r.RoomId = roomId)

    let getReservationsByDate
        (startDate: DateTime, endDate: DateTime)
        : Reservation list =

        getReservations()
        |> List.filter (fun r ->
            r.Status <> "cancelled"
            && r.ArrivalDate.Date < endDate.Date
            && r.DepartureDate.Date > startDate.Date)

    // ============================
    // DISPONIBILITE
    // ============================

    let private getAvailableBedsForRoom
        (roomId: int)
        (arrivalDate: DateTime)
        (departureDate: DateTime)
        : int =

        if departureDate.Date <= arrivalDate.Date then
            0

        else
            match getRoomById roomId with
            | None ->
                0

            | Some room ->

                if not room.IsActive then
                    0

                else

                    let bookedBeds =
                        getReservationsByRoom roomId
                        |> List.filter (fun r ->
                            r.Status <> "cancelled"
                            && arrivalDate.Date < r.DepartureDate.Date
                            && departureDate.Date > r.ArrivalDate.Date)
                        |> List.sumBy (fun r ->
                            max 1 r.ReservedBeds)

                    max 0 (room.NumberOfBeds - bookedBeds)

    let private hasEnoughBeds
        (roomId: int)
        (arrivalDate: DateTime)
        (departureDate: DateTime)
        (requestedBeds: int)
        : bool =

        let requested =
            max 1 requestedBeds

        getAvailableBedsForRoom
            roomId
            arrivalDate
            departureDate
            >= requested

    let isRoomAvailable
        (roomId: int)
        (arrivalDate: DateTime)
        (departureDate: DateTime)
        : bool =

        getAvailableBedsForRoom
            roomId
            arrivalDate
            departureDate
            > 0

    let getRoomPrice
        (room: Room)
        (priceType: string)
        : decimal =

        match priceType with
        | "semi-pension" ->
            room.SemiPensionPrice

        | "night-breakfast" ->
            room.NightWithBreakfastPrice

        | "night-meal" ->
            if room.NightWithMealPrice > 0M then
                room.NightWithMealPrice
            else
                room.NightWithBreakfastPrice

        | "simple-night" ->
            room.SimpleNightPrice

        | _ ->
            room.SimpleNightPrice

    let getDailyAvailability
        (date: DateTime)
        : (int * int * decimal * int * string) =

        let rooms =
            getRooms()
            |> List.filter (fun r -> r.IsActive)

        let totalCapacity =
            rooms
            |> List.sumBy (fun r ->
                max 0 r.NumberOfBeds)

        let occupiedBeds =
            getReservations()
            |> List.filter (fun r ->
                r.Status <> "cancelled"
                && date.Date >= r.ArrivalDate.Date
                && date.Date < r.DepartureDate.Date)
            |> List.sumBy (fun r ->
                max 1 r.ReservedBeds)

        let availableBeds =
            max 0 (totalCapacity - occupiedBeds)

        let occupancyPercent =
            if totalCapacity = 0 then
                0M
            else
                decimal occupiedBeds * 100M
                / decimal totalCapacity

        let status =
            if occupancyPercent >= 100M then
                "red"
            elif occupancyPercent >= 80M then
                "orange"
            else
                "green"

        (
            occupiedBeds,
            availableBeds,
            occupancyPercent,
            totalCapacity,
            status
        )

    // ============================
    // DATES RESERVATION
    // ============================

    let private tryParseReservationDate
        (value: string)
        : DateTime option =

        if String.IsNullOrWhiteSpace(value) then
            None

        else

            let formats =
                [|
                    "yyyy-MM-dd"
                    "dd/MM/yyyy"
                    "d/M/yyyy"
                    "yyyy/MM/dd"
                |]

            let mutable parsed =
                DateTime.MinValue

            if
                DateTime.TryParseExact(
                    value.Trim(),
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    &parsed
                )
            then
                Some parsed

            else
                if
                    DateTime.TryParse(
                        value.Trim(),
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.None,
                        &parsed
                    )
                then
                    Some parsed
                else
                    None

    let private tryGetStayDates
        (arrivalValue: string)
        (departureValue: string)
        : (DateTime * DateTime * int) option =

        match
            tryParseReservationDate arrivalValue,
            tryParseReservationDate departureValue
        with
        | Some arrivalDate, Some departureDate ->

            let arrival =
                arrivalDate.Date

            let departure =
                departureDate.Date

            let numberOfNights =
                (departure - arrival).Days

            if numberOfNights > 0 then
                Some (arrival, departure, numberOfNights)
            else
                None

        | _ ->
            None

    // ============================
    // CHAMBRES DEMANDEES
    // ============================

    let private buildRequestedRooms
        (reservation: ReservationForm)
        : (int * int) list =

        let primary =
            if reservation.RoomId > 0 then
                [
                    (
                        reservation.RoomId,
                        max 1 reservation.ReservedBeds
                    )
                ]
            else
                []

        let secondary =
            if
                reservation.AdditionalRoomId > 0
                && reservation.AdditionalReservedBeds > 0
            then
                [
                    (
                        reservation.AdditionalRoomId,
                        max 1 reservation.AdditionalReservedBeds
                    )
                ]
            else
                []

        let tertiary =
            if
                reservation.AdditionalRoomId2 > 0
                && reservation.AdditionalReservedBeds2 > 0
            then
                [
                    (
                        reservation.AdditionalRoomId2,
                        max 1 reservation.AdditionalReservedBeds2
                    )
                ]
            else
                []

        primary @ secondary @ tertiary

    // ============================
    // AJOUT RESERVATION
    // ============================

    let addReservation
        (reservation: ReservationForm)
        : Reservation option =

        try
            match
                tryGetStayDates
                    reservation.ArrivalDate
                    reservation.DepartureDate
            with
            | None ->
                None

            | Some (arrivalDate, departureDate, numberOfNights) ->

                let requestedRooms =
                    buildRequestedRooms reservation

                if List.isEmpty requestedRooms then
                    None

                elif
                    requestedRooms
                    |> List.map fst
                    |> List.distinct
                    |> List.length
                    <> requestedRooms.Length
                then
                    // Même chambre sélectionnée plusieurs fois.
                    None

                else

                    let allRoomsExist =
                        requestedRooms
                        |> List.forall (fun (roomId, _) ->
                            getRoomById roomId
                            |> Option.exists (fun room ->
                                room.IsActive))

                    if not allRoomsExist then
                        None

                    else

                        // IMPORTANT :
                        // on vérifie le nombre de lits demandé,
                        // et pas seulement qu'il reste au moins 1 lit.
                        let allRoomsAvailable =
                            requestedRooms
                            |> List.forall (fun (roomId, requestedBeds) ->
                                hasEnoughBeds
                                    roomId
                                    arrivalDate
                                    departureDate
                                    requestedBeds)

                        if not allRoomsAvailable then
                            None

                        else

                            let reservations =
                                getReservations()

                            let nextId =
                                if List.isEmpty reservations then
                                    1
                                else
                                    reservations
                                    |> List.map (fun r -> r.Id)
                                    |> List.max
                                    |> (+) 1

                            let groupId =
                                if requestedRooms.Length > 1 then
                                    nextId
                                else
                                    0

                            let buildReservation
                                roomId
                                roomBeds
                                recordId =

                                match getRoomById roomId with
                                | None ->
                                    failwith "Chambre introuvable"

                                | Some room ->

                                    let price =
                                        getRoomPrice
                                            room
                                            reservation.PriceType

                                    let totalPrice =
                                        price
                                        * decimal numberOfNights
                                        * decimal roomBeds

                                    {
                                        Id = recordId
                                        GroupId = groupId
                                        RoomId = roomId

                                        FirstName =
                                            if isNull reservation.FirstName then
                                                ""
                                            else
                                                reservation.FirstName.Trim()

                                        LastName =
                                            if isNull reservation.LastName then
                                                ""
                                            else
                                                reservation.LastName.Trim()

                                        PhoneNumber =
                                            if isNull reservation.PhoneNumber then
                                                ""
                                            else
                                                reservation.PhoneNumber.Trim()

                                        Email =
                                            if isNull reservation.Email then
                                                ""
                                            else
                                                reservation.Email.Trim()

                                        ArrivalDate = arrivalDate
                                        DepartureDate = departureDate
                                        NumberOfNights = numberOfNights
                                        ReservedBeds = roomBeds
                                        RoomName = room.Name
                                        PriceType = reservation.PriceType
                                        TotalPrice = totalPrice

                                        Comment =
                                            if isNull reservation.Comment then
                                                ""
                                            else
                                                reservation.Comment

                                        Status = "confirmed"
                                        CreatedAt = DateTime.UtcNow
                                        UpdatedAt = DateTime.UtcNow
                                    }

                            let finalReservations =
                                requestedRooms
                                |> List.mapi (fun index (roomId, roomBeds) ->
                                    buildReservation
                                        roomId
                                        roomBeds
                                        (nextId + index))

                            writeJsonFile
                                reservationsFile
                                (reservations @ finalReservations)

                            finalReservations
                            |> List.tryHead

        with
        | ex ->
            printfn "Erreur addReservation : %s" ex.Message
            None

    // ============================
    // MODIFICATION RESERVATION
    // ============================

    let updateReservation
        (id: int, reservation: ReservationForm)
        : Reservation option =

        try
            let reservations =
                getReservations()

            match
                reservations
                |> List.tryFind (fun r -> r.Id = id)
            with
            | None ->
                None

            | Some target ->

                match
                    tryGetStayDates
                        reservation.ArrivalDate
                        reservation.DepartureDate
                with
                | None ->
                    None

                | Some (arrivalDate, departureDate, numberOfNights) ->

                    let requestedRooms =
                        buildRequestedRooms reservation

                    if List.isEmpty requestedRooms then
                        None

                    elif
                        requestedRooms
                        |> List.map fst
                        |> List.distinct
                        |> List.length
                        <> requestedRooms.Length
                    then
                        None

                    else

                        // Toutes les réservations appartenant au groupe
                        // sont temporairement exclues du calcul.
                        let currentGroupReservations =
                            if target.GroupId > 0 then
                                reservations
                                |> List.filter (fun r ->
                                    r.GroupId = target.GroupId)
                            else
                                [ target ]

                        let excludedIds =
                            currentGroupReservations
                            |> List.map (fun r -> r.Id)

                        let otherReservations =
                            reservations
                            |> List.filter (fun r ->
                                r.Status <> "cancelled"
                                && not (
                                    List.contains r.Id excludedIds
                                ))

                        let hasEnoughBedsForUpdate
                            roomId
                            requestedBeds =

                            match getRoomById roomId with
                            | None ->
                                false

                            | Some room ->

                                if not room.IsActive then
                                    false

                                else

                                    let alreadyBooked =
                                        otherReservations
                                        |> List.filter (fun r ->
                                            r.RoomId = roomId
                                            && arrivalDate < r.DepartureDate.Date
                                            && departureDate > r.ArrivalDate.Date)
                                        |> List.sumBy (fun r ->
                                            max 1 r.ReservedBeds)

                                    let available =
                                        max 0 (
                                            room.NumberOfBeds
                                            - alreadyBooked
                                        )

                                    available >= requestedBeds

                        let allRoomsAvailable =
                            requestedRooms
                            |> List.forall (fun (roomId, requestedBeds) ->
                                hasEnoughBedsForUpdate
                                    roomId
                                    requestedBeds)

                        if not allRoomsAvailable then
                            None

                        else

                            let maxGroupId =
                                reservations
                                |> List.map (fun r -> r.GroupId)
                                |> List.filter (fun x -> x > 0)
                                |> function
                                    | [] ->
                                        0

                                    | ids ->
                                        List.max ids

                            let newGroupId =
                                if requestedRooms.Length > 1 then
                                    if target.GroupId > 0 then
                                        target.GroupId
                                    else
                                        maxGroupId + 1
                                else
                                    0

                            let baseReservations =
                                reservations
                                |> List.filter (fun r ->
                                    not (
                                        List.contains
                                            r.Id
                                            excludedIds
                                    ))

                            let nextGeneratedId =
                                if List.isEmpty reservations then
                                    1
                                else
                                    reservations
                                    |> List.map (fun r -> r.Id)
                                    |> List.max
                                    |> (+) 1

                            let existingByRoom =
                                currentGroupReservations
                                |> List.map (fun r -> r.RoomId, r)
                                |> Map.ofList

                            let mutable generatedIndex = 0

                            let buildReservation
                                roomId
                                roomBeds
                                recordId
                                existingReservation =

                                match getRoomById roomId with
                                | None ->
                                    failwith "Chambre introuvable"

                                | Some room ->

                                    let price =
                                        getRoomPrice
                                            room
                                            reservation.PriceType

                                    let totalPrice =
                                        price
                                        * decimal numberOfNights
                                        * decimal roomBeds

                                    {
                                        Id = recordId

                                        GroupId =
                                            if requestedRooms.Length > 1 then
                                                newGroupId
                                            else
                                                0

                                        RoomId = roomId

                                        FirstName =
                                            if isNull reservation.FirstName then
                                                ""
                                            else
                                                reservation.FirstName.Trim()

                                        LastName =
                                            if isNull reservation.LastName then
                                                ""
                                            else
                                                reservation.LastName.Trim()

                                        PhoneNumber =
                                            if isNull reservation.PhoneNumber then
                                                ""
                                            else
                                                reservation.PhoneNumber.Trim()

                                        Email =
                                            if isNull reservation.Email then
                                                ""
                                            else
                                                reservation.Email.Trim()

                                        ArrivalDate = arrivalDate
                                        DepartureDate = departureDate
                                        NumberOfNights = numberOfNights
                                        ReservedBeds = roomBeds
                                        RoomName = room.Name
                                        PriceType = reservation.PriceType
                                        TotalPrice = totalPrice

                                        Comment =
                                            if isNull reservation.Comment then
                                                ""
                                            else
                                                reservation.Comment

                                        Status = "confirmed"

                                        CreatedAt =
                                            match existingReservation with
                                            | Some existing ->
                                                existing.CreatedAt
                                            | None ->
                                                DateTime.UtcNow

                                        UpdatedAt = DateTime.UtcNow
                                    }

                            let desiredReservations =
                                requestedRooms
                                |> List.map (fun (roomId, roomBeds) ->

                                    match Map.tryFind roomId existingByRoom with
                                    | Some existing ->
                                        buildReservation
                                            roomId
                                            roomBeds
                                            existing.Id
                                            (Some existing)

                                    | None ->

                                        let recordId =
                                            if generatedIndex = 0 then
                                                id
                                            else
                                                nextGeneratedId
                                                + generatedIndex
                                                - 1

                                        generatedIndex <-
                                            generatedIndex + 1

                                        buildReservation
                                            roomId
                                            roomBeds
                                            recordId
                                            None)

                            let finalReservations =
                                baseReservations
                                @ desiredReservations

                            writeJsonFile
                                reservationsFile
                                finalReservations

                            finalReservations
                            |> List.tryFind (fun r ->
                                r.Id = id)

        with
        | ex ->
            printfn "Erreur updateReservation : %s" ex.Message
            None

    // ============================
    // ANNULATION
    // ============================

    let cancelReservation(id: int) : bool =

        try
            let reservations =
                getReservations()

            match
                reservations
                |> List.tryFind (fun r -> r.Id = id)
            with
            | None ->
                printfn
                    "Annulation impossible : réservation %d introuvable."
                    id

                false

            | Some target ->

                let updated =
                    reservations
                    |> List.map (fun r ->
                        if target.GroupId > 0 then
                            if r.GroupId = target.GroupId then
                                {
                                    r with
                                        Status = "cancelled"
                                        UpdatedAt = DateTime.UtcNow
                                }
                            else
                                r

                        elif r.Id = id then
                            {
                                r with
                                    Status = "cancelled"
                                    UpdatedAt = DateTime.UtcNow
                            }

                        else
                            r)

                writeJsonFile
                    reservationsFile
                    updated

                // Vérification après écriture
                let verification =
                    getReservations()

                let stillActive =
                    verification
                    |> List.exists (fun r ->
                        if target.GroupId > 0 then
                            r.GroupId = target.GroupId
                            && r.Status <> "cancelled"
                        else
                            r.Id = id
                            && r.Status <> "cancelled")

                if stillActive then
                    printfn
                        "ERREUR : la réservation %d est toujours active après annulation."
                        id

                    false
                else
                    printfn
                        "Réservation %d annulée avec succès."
                        id

                    true

        with
        | ex ->
            printfn
                "Erreur cancelReservation(%d) : %s"
                id
                ex.Message

            false

    // ============================
    // COMPANY SETTINGS
    // ============================

    let private defaultCompanySettings() : CompanySettings =
        {
            CompanyName = "Hotelia"
            CompanySubtitle = "Gestion hôtel"
            LogoImageUrl = "/images/logo-hotelia.svg"
        }

    let getCompanySettings() : CompanySettings =

        try
            if not (File.Exists(companySettingsFile)) then
                defaultCompanySettings()

            else

                let json =
                    File.ReadAllText(companySettingsFile)

                if String.IsNullOrWhiteSpace(json) then
                    defaultCompanySettings()

                else

                    let settings =
                        JsonSerializer.Deserialize<CompanySettings>(
                            json,
                            jsonOptions
                        )

                    if Object.ReferenceEquals(box settings, null) then
                        defaultCompanySettings()

                    else

                        let safeName =
                            if String.IsNullOrWhiteSpace(settings.CompanyName) then
                                "Hotelia"
                            else
                                settings.CompanyName.Trim()

                        let safeSubtitle =
                            if String.IsNullOrWhiteSpace(settings.CompanySubtitle) then
                                "Gestion hôtel"
                            else
                                settings.CompanySubtitle.Trim()

                        let safeLogo =
                            normalizeLogoImageUrl
                                settings.LogoImageUrl

                        {
                            CompanyName = safeName
                            CompanySubtitle = safeSubtitle
                            LogoImageUrl = safeLogo
                        }

        with
        | ex ->
            printfn
                "Erreur getCompanySettings : %s"
                ex.Message

            defaultCompanySettings()

    let saveCompanySettings
        (settings: CompanySettings)
        : CompanySettings =

        ensureDataDir()

        let normalized =
            {
                CompanyName =
                    if String.IsNullOrWhiteSpace(settings.CompanyName) then
                        "Hotelia"
                    else
                        settings.CompanyName.Trim()

                CompanySubtitle =
                    if String.IsNullOrWhiteSpace(settings.CompanySubtitle) then
                        "Gestion hôtel"
                    else
                        settings.CompanySubtitle.Trim()

                LogoImageUrl =
                    normalizeLogoImageUrl
                        settings.LogoImageUrl
            }

        let json =
            JsonSerializer.Serialize(
                normalized,
                jsonOptions
            )

        File.WriteAllText(
            companySettingsFile,
            json
        )

        normalized

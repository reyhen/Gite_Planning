namespace Gite_Planning.Models

open System

[<CLIMutable>]
type Room =
    { Id: int
      Name: string
      NumberOfBeds: int
      SemiPensionPrice: decimal
      NightWithBreakfastPrice: decimal
      NightWithMealPrice: decimal
      SimpleNightPrice: decimal
      Description: string
      IsActive: bool
      CreatedAt: DateTime }

[<CLIMutable>]
type Reservation =
    { Id: int
      GroupId: int  // 0 = pas de groupe; sinon ID du premier élément du groupe
      RoomId: int
      FirstName: string
      LastName: string
      PhoneNumber: string
      Email: string
      ArrivalDate: DateTime
      DepartureDate: DateTime
      NumberOfNights: int
      ReservedBeds: int
      RoomName: string
      PriceType: string // "semi-pension" | "night-breakfast" | "simple-night"
      TotalPrice: decimal
      Comment: string
      Status: string // "confirmed" | "pending" | "cancelled"
      CreatedAt: DateTime
      UpdatedAt: DateTime }

[<CLIMutable>]
type RoomForm =
    { Name: string
      NumberOfBeds: int
      SemiPensionPrice: decimal
      NightWithBreakfastPrice: decimal
      NightWithMealPrice: decimal
      SimpleNightPrice: decimal
      Description: string }

[<CLIMutable>]
type ReservationForm =
    { FirstName: string
      LastName: string
      PhoneNumber: string
      Email: string
      ArrivalDate: string
      DepartureDate: string
      NumberOfNights: int
      ReservedBeds: int
      RoomId: int
      PriceType: string
      Comment: string
      AdditionalRoomId: int  // 0 = pas de deuxième chambre
      AdditionalReservedBeds: int
      AdditionalRoomId2: int  // 0 = pas de troisième chambre
      AdditionalReservedBeds2: int }

[<CLIMutable>]
type CompanySettings =
    { CompanyName: string
      CompanySubtitle: string
      LogoImageUrl: string }

[<CLIMutable>]
type Host =
    { Id: int
      Name: string
      PhoneNumber: string
      Email: string
      Address: string }

[<CLIMutable>]
type HostForm =
    { Name: string
      PhoneNumber: string
      Email: string
      Address: string }

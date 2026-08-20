namespace BigRetail.Receiving.Domain
{
    public enum ReceivingAreaChangeFailure
    {
        None = 0,
        EmptyArea = 1,
        OutsideMap = 2,
        OutsideOwnedProperty = 3,
        MissingFloor = 4,
        Obstructed = 5,
        OccupiedByDelivery = 6
    }
}

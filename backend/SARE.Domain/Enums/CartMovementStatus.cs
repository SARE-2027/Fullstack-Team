namespace SARE.Domain.Enums;

public enum CartMovementStatus
{
    Accepted,
    NeedsConfirmation,
    Cancelled
}

public enum CartItemAction
{
    Scan,
    Enter,
    Exit,
    CancelScan
}

public enum CartItemEffect
{
    None,
    Added,
    Removed,
    PlacementConfirmed,
    NoChange
}

public enum BasketPresence
{
    Unconfirmed,
    Inside,
    Outside
}

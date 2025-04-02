namespace GagauziaChatBot.Core.Models.States;

public enum MarketplaceState
{
    Default,
    AwaitingRepostLink,
    AwaitingTitle,
    AwaitingDescription,
    AwaitingPhotos,
    AwaitingContact,
    ReadyToPost
}
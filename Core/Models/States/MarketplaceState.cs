namespace GagauziaChatBot.Core.Models.States;

public enum MarketplaceState
{
    Default,
    AwaitingTitle,
    AwaitingDescription,
    AwaitingPhotos,
    AwaitingContact,
    ReadyToPost
}
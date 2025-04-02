namespace GagauziaChatBot.Core.Models.States;

public enum PrivateServicesState
{
    Default,
    AwaitingTitle,
    AwaitingDescription,
    AwaitingPrice,
    AwaitingPhotos,
    AwaitingContact,  
    ReadyToPost
}
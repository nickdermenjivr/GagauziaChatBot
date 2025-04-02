namespace GagauziaChatBot.Core.Models.States;

public enum PrivateServicesState
{
    Default,
    AwaitingRepostLink,
    AwaitingTitle,
    AwaitingDescription,
    AwaitingPrice,
    AwaitingPhotos,
    AwaitingContact,  
    ReadyToPost
}
namespace GagauziaChatBot.Core.Models.States;

public enum CarpoolingState
{
    Default,
    AwaitingDate,
    AwaitingTime,
    AwaitingFrom,
    AwaitingTo,
    AwaitingPhone,
    ReadyToPost
}
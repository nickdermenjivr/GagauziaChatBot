using Telegram.Bot.Types;

namespace GagauziaChatBot.Core.Services;

public interface ICommandService
{
    Task HandleCommand(Message message, CancellationToken cancellationToken);
}
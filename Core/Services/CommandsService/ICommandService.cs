using Telegram.Bot.Types;

namespace GagauziaChatBot.Core.Services.CommandsService;

public interface ICommandService
{
    Task HandleCommand(Message message, CancellationToken cancellationToken);
}
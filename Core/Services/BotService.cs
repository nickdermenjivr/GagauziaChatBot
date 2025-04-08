using GagauziaChatBot.Core.Services.CommandsService;
using GagauziaChatBot.Core.Services.DiscountsService;
using GagauziaChatBot.Core.Services.NewsService;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;

namespace GagauziaChatBot.Core.Services;

public class BotService(ITelegramBotClient botClient, CancellationToken cancellationToken, ICommandService commandService, INewsService newsService, IDiscountsService discountsService)
{
    public void StartReceiving()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [],
            DropPendingUpdates = true
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken
        );
    }

    public void StartNewsPostingJob()
    { 
        newsService.StartNewsPostingAsync(cancellationToken);
    }

    public void StartDiscountsPostingJob()
    {
        discountsService.PublishCatalogAsync(cancellationToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient localBotClient, Update update, CancellationToken ctx)
    {
        try
        {
            if (update.Message is { } message)
            {
                await commandService.HandleCommand(message, ctx);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient localBotClient, Exception exception, CancellationToken ctx)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException 
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}
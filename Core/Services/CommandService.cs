using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using GagauziaChatBot.Core.Services.PostHandlers;

namespace GagauziaChatBot.Core.Services;

public class CommandService : ICommandService
{
    private readonly ITelegramBotClient _botClient;
    private readonly Dictionary<string, BasePostHandler> _postHandlers;

    public CommandService(ITelegramBotClient botClient)
    {
        _botClient = botClient;
        _postHandlers = InitializePostHandlers(botClient);
    }

    public async Task HandleCommand(Message message, CancellationToken cancellationToken)
    {
        try
        {
            if (IsResetCommand(message.Text))
            {
                await ResetAllHandlers(message.Chat.Id, cancellationToken);
                return;
            }

            var activeHandler = GetActiveHandler();
            if (activeHandler != null)
            {
                await ProcessActiveHandler(activeHandler, message, cancellationToken);
                return;
            }

            await ProcessMainCommands(message, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleError(message.Chat.Id, ex, cancellationToken);
        }
    }

    private Dictionary<string, BasePostHandler> InitializePostHandlers(ITelegramBotClient botClient)
    {
        return new Dictionary<string, BasePostHandler>
        {
            { TelegramConstants.ButtonTitles.Carpooling, new CarpoolingPostHandler(botClient) },
            { TelegramConstants.ButtonTitles.Marketplace, new MarketplacePostHandler(botClient) }
        };
    }

    private static bool IsResetCommand(string? messageText)
    {
        return messageText is TelegramConstants.ButtonTitles.MainMenu or TelegramConstants.ButtonTitles.Cancel;
    }

    private BasePostHandler? GetActiveHandler()
    {
        return _postHandlers.Values.FirstOrDefault(h => h.IsActive);
    }

    private async Task ProcessActiveHandler(BasePostHandler handler, Message message, CancellationToken ct)
    {
        if (message.Photo != null)
        {
            await handler.HandlePhoto(message, ct);
        }
        else if (message.Text != null)
        {
            await handler.HandleMessage(message, ct);
        }
    }

    private async Task ProcessMainCommands(Message message, CancellationToken ct)
    {
        switch (message.Text)
        {
            case "/menu":
            case TelegramConstants.ButtonTitles.MainMenu:
                await ShowMainMenu(message.Chat.Id, ct);
                break;
            
            case TelegramConstants.ButtonTitles.NewPost:
                await ShowChooseCategory(message.Chat.Id, ct);
                break;
                
            case var text when _postHandlers.ContainsKey(text!):
                await _postHandlers[text!].StartCreation(message.Chat.Id, ct);
                break;
        }
    }

    private async Task ResetAllHandlers(long chatId, CancellationToken ct)
    {
        var resetTasks = _postHandlers.Values
            .Where(h => h.IsActive)
            .Select(h => h.CancelCreation(chatId, ct));

        await Task.WhenAll(resetTasks);
        await ShowMainMenu(chatId, ct);
    }

    private async Task ShowMainMenu(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton(TelegramConstants.ButtonTitles.NewPost) },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await _botClient.SendMessage(
            chatId: chatId,
            text: "🏠 <b>Главное меню</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowChooseCategory(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(
            _postHandlers.Keys
                .Select(title => new[] { new KeyboardButton(title) })
                .Concat(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
        )
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await _botClient.SendMessage(
            chatId: chatId,
            text: "📋 <b>Выберите категорию</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task HandleError(long chatId, Exception ex, CancellationToken ct)
    {
        await _botClient.SendMessage(
            chatId: chatId,
            text: "⚠️ Произошла ошибка при обработке запроса. Пожалуйста, попробуйте еще раз.",
            cancellationToken: ct);

        Console.WriteLine($"Error in HandleCommand: {ex}");
    }
}
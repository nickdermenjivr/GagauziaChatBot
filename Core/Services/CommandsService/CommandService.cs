using GagauziaChatBot.Core.Configuration;
using GagauziaChatBot.Core.Services.CommandsService.PostHandlers;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services.CommandsService;

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
        //if (message.Type != MessageType.Text || message.From == null)
          //  return;
          
        try
        {
            if (await RestrictMessageInThreads(message, cancellationToken)) return;
            if (message.Chat.Type != ChatType.Private)
                return;
            
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
        catch (Exception)
        {
            await HandleError(message.Chat.Id, cancellationToken);
        }
    }

    private Dictionary<string, BasePostHandler> InitializePostHandlers(ITelegramBotClient botClient)
    {
        return new Dictionary<string, BasePostHandler>
        {
            { TelegramConstants.ButtonTitles.Carpooling, new CarpoolingPostHandler(botClient) },
            { TelegramConstants.ButtonTitles.Marketplace, new MarketplacePostHandler(botClient) },
            { TelegramConstants.ButtonTitles.PrivateServices, new PrivateServicesPostHandler(botClient) }
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
        try
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
        catch (Exception)
        {
            // ignored
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

    private async Task HandleError(long chatId, CancellationToken ct)
    {
        try
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: "⚠️ Произошла ошибка. Попробуйте позже.",
                cancellationToken: ct);
        }
        catch
        {
            // ignored
        }
    }
    
    private async Task<bool> RestrictMessageInThreads(Message message, CancellationToken ct)
    {
        if (message.Chat.Id != TelegramConstants.GagauziaChatId) return false;
        
        var isMainThread = message.MessageThreadId == null 
                            || message.MessageThreadId == TelegramConstants.MainThreadId;

        var restrictedThreadIds = new[] 
        { 
            //TelegramConstants.CarpoolingThreadId,
            TelegramConstants.MarketplaceThreadId,
            TelegramConstants.PrivateServicesThreadId
        };

        var shouldRestrict = isMainThread || 
                              (message.MessageThreadId.HasValue && 
                               restrictedThreadIds.Contains(message.MessageThreadId.Value));

        if (!shouldRestrict)
            return false;

        try
        {
            await _botClient.DeleteMessage(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                cancellationToken: ct);

            await _botClient.RestrictChatMember(
                chatId: message.Chat.Id,
                userId: message.From!.Id,
                permissions: new ChatPermissions { CanSendMessages = false },
                untilDate: DateTime.UtcNow.AddMinutes(1),
                cancellationToken: ct);

            var chatInfo = await _botClient.GetChat(message.Chat.Id, ct);
            await _botClient.SendMessage(
                chatId: message.From.Id,
                text: $"✋ В {(isMainThread ? "основном чате" : "разделе")} {chatInfo.Title} " +
                      "можно писать только через бота.\nИспользуйте /menu",
                cancellationToken: ct);

            return true;
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 400 || ex.ErrorCode == 403)
        {
            return false;
        }
    }
    
    private async Task DeleteMessageFromChat(long chatId, int messageId)
    {
        try
        {
            await _botClient.DeleteMessage(chatId, messageId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
        }
    }
}
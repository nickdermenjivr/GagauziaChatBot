using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services.PostHandlers;

public abstract class BasePostHandler(ITelegramBotClient botClient)
{
    protected readonly ITelegramBotClient BotClient = botClient;
    protected internal bool IsActive;
    
    public abstract string PostTypeName { get; }
    public abstract string PostButtonTitle { get; }

    public abstract Task StartCreation(long chatId, CancellationToken ct);
    public abstract Task HandleMessage(Message message, CancellationToken ct);
    public abstract Task HandlePhoto(Message message, CancellationToken ct);
    
    public virtual async Task CancelCreation(long chatId, CancellationToken ct)
    {
        IsActive = false;
        await BotClient.SendMessage(
            chatId: chatId,
            text: "Создание поста отменено",
            cancellationToken: ct);
    }

    protected async Task ShowMainMenu(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton(TelegramConstants.ButtonTitles.NewPost) },
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await BotClient.SendMessage(
            chatId: chatId,
            text: "🏠 <b>Главное меню</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}
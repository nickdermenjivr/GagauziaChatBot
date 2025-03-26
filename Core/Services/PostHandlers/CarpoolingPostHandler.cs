using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using GagauziaChatBot.Core.Models.Posts;
using GagauziaChatBot.Core.Models.States;

namespace GagauziaChatBot.Core.Services.PostHandlers;

public class CarpoolingPostHandler(ITelegramBotClient botClient) : BasePostHandler(botClient)
{
    private CarpoolingPost _carpoolingPost = new ();
    private CarpoolingState _state = CarpoolingState.Default;
    
    public override string PostTypeName => "🚗 Попутчики";
    public override string PostButtonTitle => "✅ Отправить попутчикам";

    public override async Task StartCreation(long chatId, CancellationToken ct)
    {
        IsActive = true;
        _state = CarpoolingState.AwaitingDate;
        _carpoolingPost = new CarpoolingPost();
        
        var messageText = @"🚗 <b>Раздел Попутчики</b>

Следуйте инструкциям, чтобы указать все необходимые детали:
1. 📅 <b>Дата</b> (сегодня/завтра)
2. ⌛ <b>Время</b>
3. 🚩 <b>Пункт отправления</b>
4. 🏁 <b>Пункт прибытия</b>
5. ☎ <b>Номер телефона</b>

Выберите дату:";

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton(TelegramConstants.ButtonTitles.CarpoolingToday), new KeyboardButton(TelegramConstants.ButtonTitles.CarpoolingTomorrow) },
            new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await BotClient.SendMessage(
            chatId: chatId,
            text: messageText,
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public override async Task HandleMessage(Message message, CancellationToken ct)
    {
        if (message.Text == null) return;

        switch (_state)
        {
            case CarpoolingState.AwaitingDate:
                await HandleDateInput(message, ct);
                break;
                
            case CarpoolingState.AwaitingTime:
                _carpoolingPost.Time = message.Text;
                _state = CarpoolingState.AwaitingFrom;
                await ShowFromInput(message.Chat.Id, ct);
                break;
                
            case CarpoolingState.AwaitingFrom:
                _carpoolingPost.From = message.Text;
                _state = CarpoolingState.AwaitingTo;
                await ShowToInput(message.Chat.Id, ct);
                break;
                
            case CarpoolingState.AwaitingTo:
                _carpoolingPost.To = message.Text;
                _state = CarpoolingState.AwaitingPhone;
                await ShowPhoneInput(message.Chat.Id, ct);
                break;
                
            case CarpoolingState.AwaitingPhone:
                _carpoolingPost.Phone = message.Text;
                _carpoolingPost.Username = message.Chat.Username!;
                _state = CarpoolingState.ReadyToPost;
                await ShowPreview(message.Chat.Id, ct);
                break;
                
            case CarpoolingState.ReadyToPost when message.Text == PostButtonTitle:
                await PostToChannel(message.Chat.Id, ct);
                break;
            case CarpoolingState.Default:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override Task HandlePhoto(Message message, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    private async Task HandleDateInput(Message message, CancellationToken ct)
    {
        switch (message.Text)
        {
            case TelegramConstants.ButtonTitles.CarpoolingToday:
                _carpoolingPost.Date = "Сегодня";
                break;
            case TelegramConstants.ButtonTitles.CarpoolingTomorrow:
                _carpoolingPost.Date = "Завтра";
                break;
            default:
                await BotClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Пожалуйста, выберите дату из предложенных вариантов",
                    cancellationToken: ct);
                return;
        }

        _state = CarpoolingState.AwaitingTime;
        await ShowTimeInput(message.Chat.Id, ct);
    }

    private async Task ShowTimeInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "<b>⌛ Укажите время:</b>\n\nПример: <i>15:30 или словами 'сейчас, в течение часа, после обеда...'</i>",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowFromInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "<b>🚩 Укажите пункт отправления:</b>\n\nПример: <i>Чадыр-Лунга (через Конгаз)</i>",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowToInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "<b>🏁 Укажите пункт назначения:</b>\n\nПример: <i>Кишинев, Рышкановка</i>",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowPhoneInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "<b>☎ Укажите номер телефона:</b>\n\nФормат: <i>78717171</i> (без +373)",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowPreview(long chatId, CancellationToken ct)
    {
        var previewText = _carpoolingPost.ToFormattedString();
        
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton(PostButtonTitle) },
            new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) }
        })
        {
            ResizeKeyboard = true
        };

        await BotClient.SendMessage(
            chatId: chatId,
            text: $"<b>Ваше объявление:</b>\n{previewText}",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task PostToChannel(long chatId, CancellationToken ct)
    {
        var postText = _carpoolingPost.ToFormattedString();

        await BotClient.SendMessage(
            chatId: TelegramConstants.GagauziaChatId,
            messageThreadId: TelegramConstants.CarpoolingThreadId,
            text: postText,
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        await BotClient.SendMessage(
            chatId: chatId,
            text: "✅ Ваше объявление опубликовано в разделе 'Попутчики'!",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.MainMenu) } })
            {
                ResizeKeyboard = true
            },
            cancellationToken: ct);

        IsActive = false;
    }
}
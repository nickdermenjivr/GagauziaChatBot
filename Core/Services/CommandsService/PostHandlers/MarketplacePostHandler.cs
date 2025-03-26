using GagauziaChatBot.Core.Configuration;
using GagauziaChatBot.Core.Models.Posts;
using GagauziaChatBot.Core.Models.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services.CommandsService.PostHandlers;

public class MarketplacePostHandler(ITelegramBotClient botClient) : BasePostHandler(botClient)
{
    private MarketplacePost _marketplacePost = new();
    private MarketplaceState _state = MarketplaceState.Default;
    private const int MaxPhotos = 10;
    
    public override string PostTypeName => "🛒 Рынок";
    public override string PostButtonTitle => "✅ Опубликовать на рынке";

    public override async Task StartCreation(long chatId, CancellationToken ct)
    {
        IsActive = true;
        _state = MarketplaceState.AwaitingTitle;
        _marketplacePost = new MarketplacePost { PhotoIds = new List<string>() };

        await BotClient.SendMessage(
            chatId: chatId,
            text: "🛒 <b>Введите название товара:</b>\n\nПример: <i>Продам iPhone 13, 128GB</i>",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    public override async Task HandleMessage(Message message, CancellationToken ct)
    {
        if (message.Text == null) return;

        switch (_state)
        {
            case MarketplaceState.AwaitingTitle:
                _marketplacePost.Title = message.Text;
                _state = MarketplaceState.AwaitingDescription;
                await ShowDescriptionInput(message.Chat.Id, ct);
                break;

            case MarketplaceState.AwaitingDescription:
                _marketplacePost.Description = message.Text;
                _state = MarketplaceState.AwaitingPhotos;
                await ShowPhotosInput(message.Chat.Id, ct);
                break;

            case MarketplaceState.AwaitingPhotos when message.Text == TelegramConstants.ButtonTitles.SkipPhotos:
                _state = MarketplaceState.AwaitingContact;
                await ShowContactInput(message.Chat.Id, ct);
                break;

            case MarketplaceState.AwaitingContact:
                _marketplacePost.Contact = message.Text;
                _marketplacePost.Username = message.Chat.Username!;
                _state = MarketplaceState.ReadyToPost;
                await ShowPreview(message.Chat.Id, ct);
                break;

            case MarketplaceState.ReadyToPost when message.Text == PostButtonTitle:
                await PostToChannel(message.Chat.Id, ct);
                break;
        }
    }

    public override async Task HandlePhoto(Message message, CancellationToken ct)
    {
        if (_state != MarketplaceState.AwaitingPhotos) return;

        if (_marketplacePost.PhotoIds!.Count >= MaxPhotos)
        {
            await BotClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"⚠️ Максимум {MaxPhotos} фото. Нажмите \"{TelegramConstants.ButtonTitles.SkipPhotos}\" для продолжения",
                cancellationToken: ct);
            return;
        }

        var photo = message.Photo!.Last();
        _marketplacePost.PhotoIds.Add(photo.FileId);

        await BotClient.SendMessage(
            chatId: message.Chat.Id,
            text: $"📸 Добавлено фото {_marketplacePost.PhotoIds.Count}/{MaxPhotos}. Отправьте еще или нажмите \"{TelegramConstants.ButtonTitles.SkipPhotos}\"",
            replyMarkup: new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(TelegramConstants.ButtonTitles.SkipPhotos) },
                new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) }
            })
            {
                ResizeKeyboard = true
            },
            cancellationToken: ct);
    }

    private async Task ShowDescriptionInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "📝 <b>Опишите товар:</b>\n\nУкажите:\n- Состояние\n- Цену\n- Особенности\n\nПример: <i>Отличное состояние, батарея 100%. Цена: 12000 лей. В комплекте чехол и зарядка.</i>",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowPhotosInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: $"📸 <b>Пришлите фото товара (до {MaxPhotos} шт.):</b>\n\nИли нажмите \"{TelegramConstants.ButtonTitles.SkipPhotos}\"",
            replyMarkup: new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(TelegramConstants.ButtonTitles.SkipPhotos) },
                new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) }
            })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowContactInput(long chatId, CancellationToken ct)
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
        var previewText = _marketplacePost.ToFormattedString();
        
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
        var postText = _marketplacePost.ToFormattedString();
        
        if (_marketplacePost.PhotoIds!.Any())
        {
            var media = _marketplacePost.PhotoIds!
                .Select((id, index) => new InputMediaPhoto(id)
                {
                    Caption = index == 0 ? postText : null,
                    ParseMode = index == 0 ? ParseMode.Html : ParseMode.None
                })
                .ToList();

            await BotClient.SendMediaGroup(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.MarketplaceThreadId,
                media: media,
                cancellationToken: ct);
        }
        else
        {
            await BotClient.SendMessage(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.MarketplaceThreadId,
                text: postText,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }

        await BotClient.SendMessage(
            chatId: chatId,
            text: "✅ Ваше объявление опубликовано в разделе 'Рынок'!",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.MainMenu) } })
            {
                ResizeKeyboard = true
            },
            cancellationToken: ct);

        IsActive = false;
    }
}
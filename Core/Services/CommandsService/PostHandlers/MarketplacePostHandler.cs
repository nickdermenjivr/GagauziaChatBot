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
        _state = MarketplaceState.Default;
        _marketplacePost = new MarketplacePost { PhotoIds = new List<string>() };

        await BotClient.SendMessage(
            chatId: chatId,
            text: "Вы хотите создать новое объявление или переопубликовать существующее?",
            replyMarkup: new ReplyKeyboardMarkup(new[] 
            {
                new[] { new KeyboardButton(TelegramConstants.ButtonTitles.CreateNew) },
                new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Repost) },
                new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) }
            })
            {
                ResizeKeyboard = true
            },
            cancellationToken: ct);
    }

    public override async Task HandleMessage(Message message, CancellationToken ct)
    {
        if (message.Text == null) return;

        switch (_state)
        {
            case MarketplaceState.Default when message.Text == TelegramConstants.ButtonTitles.CreateNew:
                _state = MarketplaceState.AwaitingTitle;
                await ShowTitleInput(message.Chat.Id, ct);
                break;
                
            case MarketplaceState.Default when message.Text == TelegramConstants.ButtonTitles.Repost:
                _state = MarketplaceState.AwaitingRepostLink;
                await ShowRepostLinkInput(message.Chat.Id, ct);
                break;
                
            case MarketplaceState.AwaitingRepostLink:
                if (TryParseMessageIdFromLink(message.Text, out var messageId))
                {
                    await RepostMessageById(message.Chat.Id,messageId, ct);
                }
                else
                {
                    await BotClient.SendMessage(
                        chatId: message.Chat.Id,
                        text: "❌ Неверная ссылка на сообщение. Пожалуйста, отправьте корректную ссылку на ваше объявление.",
                        replyMarkup: new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) }
                        })
                        {
                            ResizeKeyboard = true
                        },
                        cancellationToken: ct);
                }
                break;
            
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
            default:
                throw new ArgumentOutOfRangeException();
        }

        await DeleteMessageFromChat(message.Chat.Id, message.MessageId);
    }

    public override async Task HandlePhoto(Message message, CancellationToken ct)
    {
        Console.WriteLine("HandlePhoto");
        if (_state != MarketplaceState.AwaitingPhotos) return;
        Console.WriteLine("Check amount");

        if (_marketplacePost.PhotoIds!.Count >= MaxPhotos)
        {
            await BotClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"⚠️ Максимум {MaxPhotos} фото. Нажмите \"{TelegramConstants.ButtonTitles.SkipPhotos}\" для продолжения",
                cancellationToken: ct);
            return;
        }

        Console.WriteLine("Adding Photo");

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
    private async Task ShowRepostLinkInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "🔗 Пожалуйста, отправьте ссылку на ваше предыдущее объявление, которое вы хотите переопубликовать.\n\nВы можете найти его в истории переписки с ботом.",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            cancellationToken: ct);
    }
    private async Task ShowTitleInput(long chatId, CancellationToken ct)
    {
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
        Message postedMessage;

        if (_marketplacePost.PhotoIds!.Any())
        {
            var media = _marketplacePost.PhotoIds!
                .Select((id, index) => new InputMediaPhoto(id)
                {
                    Caption = index == 0 ? postText : null,
                    ParseMode = index == 0 ? ParseMode.Html : ParseMode.None
                })
                .ToList();

            var messages = await BotClient.SendMediaGroup(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.MarketplaceThreadId,
                media: media,
                cancellationToken: ct);
            
            postedMessage = messages.First();
        }
        else
        {
            postedMessage = await BotClient.SendMessage(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.MarketplaceThreadId,
                text: postText,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }

        var messageLink = $"https://t.me/c/{TelegramConstants.GagauziaChatId.ToString().Replace("-100", "")}/{postedMessage.MessageId}";
        await BotClient.SendMessage(
            chatId: chatId,
            text: $"✅ Ваше объявление опубликовано!\n\nСсылка на объявление: {messageLink}",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.MainMenu) } })
            {
                ResizeKeyboard = true
            },
            cancellationToken: ct);

        IsActive = false;
    }
    private async Task RepostMessageById(long botChatId, int messageId, CancellationToken ct)
    {
        try
        {
            await BotClient.ForwardMessage(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.MarketplaceThreadId,
                fromChatId: TelegramConstants.GagauziaChatId,
                messageId: messageId,
                cancellationToken: ct);
            
            await BotClient.SendMessage(
                chatId: botChatId,
                text: "✅ Ваше предложение услуг переопубликованно!",
                replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.MainMenu) } })
                {
                    ResizeKeyboard = true
                },
                linkPreviewOptions: true,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при пересылке сообщения: {ex.Message}");
        }
    }
    private bool TryParseMessageIdFromLink(string link, out int messageId)
    {
        messageId = 0;
    
        if (string.IsNullOrEmpty(link)) 
            return false;

        var uriParts = link.Split(new[] { '/', '?' }, StringSplitOptions.RemoveEmptyEntries);
    
        if (uriParts.Length < 2 || !int.TryParse(uriParts.LastOrDefault(x => int.TryParse(x, out _)), out messageId))
            return false;

        var threadParam = uriParts.FirstOrDefault(x => x.StartsWith("thread="));
        if (threadParam == null || !int.TryParse(threadParam.Split('=')[1], out var threadId)) return true;
        return threadId == TelegramConstants.MarketplaceThreadId;
    }
    public async Task DeleteMessageFromChat(long chatId, int messageId)
    {
        try
        {
            await BotClient.DeleteMessage(chatId, messageId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
        }
    }
}
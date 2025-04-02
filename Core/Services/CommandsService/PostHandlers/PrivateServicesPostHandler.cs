using GagauziaChatBot.Core.Configuration;
using GagauziaChatBot.Core.Models.Posts;
using GagauziaChatBot.Core.Models.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services.CommandsService.PostHandlers;

public class PrivateServicesPostHandler(ITelegramBotClient botClient) : BasePostHandler(botClient)
{
    private PrivateServicesPost _servicePost = new();
    private PrivateServicesState _state = PrivateServicesState.Default;
    private const int MaxPhotos = 5;
    
    public override string PostTypeName => "💼 Частные услуги";
    public override string PostButtonTitle => "✅ Опубликовать услугу";

    public override async Task StartCreation(long chatId, CancellationToken ct)
    {
        IsActive = true;
        _state = PrivateServicesState.Default;
        _servicePost = new PrivateServicesPost { PhotoIds = new List<string>() };

        await BotClient.SendMessage(
            chatId: chatId,
            text: "Вы хотите создать новое объявление или переопубликовать существующее?",
            replyMarkup: new ReplyKeyboardMarkup(new[] 
            {
                new[] { new KeyboardButton("Создать новое") },
                new[] { new KeyboardButton("Переопубликовать") },
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
            case PrivateServicesState.Default when message.Text == "Создать новое":
                _state = PrivateServicesState.AwaitingTitle;
                await ShowTitleInput(message.Chat.Id, ct);
                break;
                
            case PrivateServicesState.Default when message.Text == "Переопубликовать":
                _state = PrivateServicesState.AwaitingRepostLink;
                await ShowRepostLinkInput(message.Chat.Id, ct);
                break;
                
            case PrivateServicesState.AwaitingRepostLink:
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
            
            case PrivateServicesState.AwaitingTitle:
                _servicePost.Title = message.Text;
                _state = PrivateServicesState.AwaitingDescription;
                await ShowDescriptionInput(message.Chat.Id, ct);
                break;

            case PrivateServicesState.AwaitingDescription:
                _servicePost.Description = message.Text;
                _state = PrivateServicesState.AwaitingPrice;
                await ShowPriceInput(message.Chat.Id, ct);
                break;

            case PrivateServicesState.AwaitingPrice:
                _servicePost.Price = message.Text;
                _state = PrivateServicesState.AwaitingPhotos;
                await ShowPhotosInput(message.Chat.Id, ct);
                break;

            case PrivateServicesState.AwaitingPhotos when message.Text == TelegramConstants.ButtonTitles.SkipPhotos:
                _state = PrivateServicesState.AwaitingContact;
                await ShowContactInput(message.Chat.Id, ct);
                break;

            case PrivateServicesState.AwaitingContact:
                _servicePost.Contact = message.Text;
                _servicePost.Username = message.Chat.Username!;
                _state = PrivateServicesState.ReadyToPost;
                await ShowPreview(message.Chat.Id, ct);
                break;

            case PrivateServicesState.ReadyToPost when message.Text == PostButtonTitle:
                await PostToChannel(message.Chat.Id, ct);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override async Task HandlePhoto(Message message, CancellationToken ct)
    {
        if (_state != PrivateServicesState.AwaitingPhotos) return;

        if (_servicePost.PhotoIds!.Count >= MaxPhotos)
        {
            await BotClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"⚠️ Максимум {MaxPhotos} фото. Нажмите \"{TelegramConstants.ButtonTitles.SkipPhotos}\" для продолжения",
                cancellationToken: ct);
            return;
        }

        var photo = message.Photo!.Last();
        _servicePost.PhotoIds.Add(photo.FileId);

        await BotClient.SendMessage(
            chatId: message.Chat.Id,
            text: $"📸 Добавлено фото {_servicePost.PhotoIds.Count}/{MaxPhotos}. Отправьте еще или нажмите \"{TelegramConstants.ButtonTitles.SkipPhotos}\"",
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

    private async Task ShowTitleInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "💼 <b>Введите название вашей услуги:</b>\n\nПримеры:\n<i>- Пассажирские перевозки Молдова-Болгария\n- Маникюр с выездом на дом\n- Ремонт компьютеров</i>",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
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
    private async Task ShowDescriptionInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "📝 <b>Опишите вашу услугу:</b>\n\nУкажите:\n- Ваш опыт\n- Особенности услуги\n- Возможные варианты\n\nПример: <i>Опыт работы 5 лет. Делаю аппаратный маникюр, покрытие гель-лаком. Выезд на дом.</i>",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.Cancel) } })
            {
                ResizeKeyboard = true
            },
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task ShowPriceInput(long chatId, CancellationToken ct)
    {
        await BotClient.SendMessage(
            chatId: chatId,
            text: "💰 <b>Укажите стоимость услуги:</b>\n\nПримеры:\n<i>- 100 лей за маникюр\n- 200 лей/час за репетиторство\n- Договорная</i>",
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
            text: $"📸 <b>Пришлите фото ваших работ (до {MaxPhotos} шт.):</b>\n\nИли нажмите \"{TelegramConstants.ButtonTitles.SkipPhotos}\"",
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
        var previewText = _servicePost.ToFormattedString();
        
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
        var postText = _servicePost.ToFormattedString();
        Message postedMessage;
        
        if (_servicePost.PhotoIds!.Any())
        {
            var media = _servicePost.PhotoIds!
                .Select((id, index) => new InputMediaPhoto(id)
                {
                    Caption = index == 0 ? postText : null,
                    ParseMode = index == 0 ? ParseMode.Html : ParseMode.None
                })
                .ToList();

            var messages =await BotClient.SendMediaGroup(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.PrivateServicesThreadId,
                media: media,
                cancellationToken: ct);
            
            postedMessage = messages.First();
        }
        else
        {
            postedMessage = await BotClient.SendMessage(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.PrivateServicesThreadId,
                text: postText,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }

        var messageLink = $"https://t.me/c/{TelegramConstants.GagauziaChatId.ToString().Replace("-100", "")}/{postedMessage.MessageId}";
        
        await BotClient.SendMessage(
            chatId: chatId,
            text: $"✅ Ваше предложение услуг опубликовано!\n\nСсылка на объявление: {messageLink}",
            replyMarkup: new ReplyKeyboardMarkup(new[] { new[] { new KeyboardButton(TelegramConstants.ButtonTitles.MainMenu) } })
            {
                ResizeKeyboard = true
            },
            linkPreviewOptions: true,
            cancellationToken: ct);

        IsActive = false;
    }
    private async Task RepostMessageById(long botChatId, int messageId, CancellationToken ct)
    {
        try
        {
            await BotClient.ForwardMessage(
                chatId: TelegramConstants.GagauziaChatId,
                messageThreadId: TelegramConstants.PrivateServicesThreadId,
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
        if (string.IsNullOrEmpty(link)) return false;
        
        // Telegram message links are in format https://t.me/c/CHAT_ID/MESSAGE_ID
        // or https://t.me/username/MESSAGE_ID for public channels
        var parts = link.Split('/');
        return parts.Length >= 2 && int.TryParse(parts.Last(), out messageId);
    }
}
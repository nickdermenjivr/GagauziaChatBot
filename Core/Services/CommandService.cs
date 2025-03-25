using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services;

public class CommandService(ITelegramBotClient botClient) : ICommandService
{
    // Общие данные
    private const long GagauziaChatId = -1002696920941;
    private const int CarpoolingThreadId = 15;
    private const int MarketplaceThreadId = 18;

    // Carpooling
    private CarpoolingData _carpoolingData;
    private CarpoolingState _carpoolingState = CarpoolingState.Default;
    
    private struct CarpoolingData
    {
        public string Date;
        public string Time;
        public string From;
        public string To;
        public string Phone;
        public string Result;
    }
    
    private enum CarpoolingState
    {
        Default,
        AwaitingCarpoolingTime,
        AwaitingCarpoolingFrom,
        AwaitingCarpoolingTo,
        AwaitingCarpoolingPhone,
        AwaitingCarPoolingResult
    }

    // Marketplace
    private MarketplaceData _marketplaceData;
    private MarketplaceState _marketplaceState = MarketplaceState.Default;
    
    private struct MarketplaceData
    {
        public string Title;
        public string Description;
        public List<string> PhotoUrls;
        public string Contact;
        public string Result;
    }
    
    private enum MarketplaceState
    {
        Default,
        AwaitingTitle,
        AwaitingDescription,
        AwaitingPhotos,
        AwaitingContact,
        AwaitingResult
    }

    // Кнопки
    private static class ButtonTitles
    {
        public const string MainMenu = "🏠 Главное меню";
        public const string NewPost = "📋 Разместить объявление";
        public const string PostCarpooling = "✅ Отправить попутчикам";
        public const string PostMarketplace = "✅ Опубликовать на рынке";
        public const string Cancel = "❌ Отмена";
        public const string Carpooling = "🚗 Попутчики";
        public const string CarpoolingDataToday = "🏃 Сегодня";
        public const string CarpoolingDataTomorrow = "🚶 Завтра";
        public const string Marketplace = "🛒 Рынок";
        public const string SkipPhotos = "⏭ Продолжить";
    }

    public async Task HandleCommand(Message message, CancellationToken cancellationToken)
    {
        // Photo
        if (message.Photo != null)
        {
            var messagePhoto = message.Photo;
            switch (messagePhoto)
            {
                // Marketplace
                case not null when _marketplaceState == MarketplaceState.AwaitingPhotos:
                    await HandleMarketplacePhotos(message, cancellationToken);
                    break;
            }
        }
        
        // Text
        if (message.Text == null) return;

        switch (message.Text)
        {
            case "/menu":
            case ButtonTitles.MainMenu:
            case ButtonTitles.Cancel:
                ResetAllStates();
                await ShowMainMenu(message.Chat.Id, cancellationToken);
                break;
            
            case ButtonTitles.NewPost:
                await ShowChooseCategory(message.Chat.Id, cancellationToken);
                break;
                
            // Carpooling
            case ButtonTitles.Carpooling:
                await ShowCarpoolingMenu(message.Chat.Id, cancellationToken);
                break;
            case ButtonTitles.CarpoolingDataToday:
                _carpoolingData.Date = "Сегодня";
                _carpoolingState = CarpoolingState.AwaitingCarpoolingTime;
                await ShowCarpoolingTimeInput(message.Chat.Id, cancellationToken);
                break;
            case ButtonTitles.CarpoolingDataTomorrow:
                _carpoolingData.Date = "Завтра";
                _carpoolingState = CarpoolingState.AwaitingCarpoolingTime;
                await ShowCarpoolingTimeInput(message.Chat.Id, cancellationToken);
                break;
            case not null when _carpoolingState == CarpoolingState.AwaitingCarpoolingTime:
                _carpoolingData.Time = message.Text;
                _carpoolingState = CarpoolingState.AwaitingCarpoolingFrom;
                await ShowCarpoolingFromInput(message.Chat.Id, cancellationToken);
                break;
            case not null when _carpoolingState == CarpoolingState.AwaitingCarpoolingFrom:
                _carpoolingData.From = message.Text;
                _carpoolingState = CarpoolingState.AwaitingCarpoolingTo;
                await ShowCarpoolingToInput(message.Chat.Id, cancellationToken);
                break;
            case not null when _carpoolingState == CarpoolingState.AwaitingCarpoolingTo:
                _carpoolingData.To = message.Text;
                _carpoolingState = CarpoolingState.AwaitingCarpoolingPhone;
                await ShowCarpoolingPhoneInput(message.Chat.Id, cancellationToken);
                break;
            case not null when _carpoolingState == CarpoolingState.AwaitingCarpoolingPhone:
                _carpoolingData.Phone = message.Text;
                _carpoolingState = CarpoolingState.AwaitingCarPoolingResult;
                _carpoolingData.Result = await ShowCarpoolingResult(message.Chat.Username!, message.Chat.Id, cancellationToken);
                break;
            case ButtonTitles.PostCarpooling:
                await PostCarpooling(message.Chat.Id, GagauziaChatId, CarpoolingThreadId, _carpoolingData.Result, cancellationToken);
                break;
                
            // Marketplace
            case ButtonTitles.Marketplace:
                _marketplaceState = MarketplaceState.AwaitingTitle;
                _marketplaceData = new MarketplaceData { PhotoUrls = [] };
                await ShowMarketplaceTitleInput(message.Chat.Id, cancellationToken);
                break;
            case not null when _marketplaceState == MarketplaceState.AwaitingTitle:
                _marketplaceData.Title = message.Text;
                _marketplaceState = MarketplaceState.AwaitingDescription;
                await ShowMarketplaceDescInput(message.Chat.Id, cancellationToken);
                break;
            case not null when _marketplaceState == MarketplaceState.AwaitingDescription:
                _marketplaceData.Description = message.Text;
                _marketplaceState = MarketplaceState.AwaitingPhotos;
                await ShowMarketplacePhotosInput(message.Chat.Id, cancellationToken);
                break;
            case ButtonTitles.SkipPhotos when _marketplaceState == MarketplaceState.AwaitingPhotos:
                _marketplaceState = MarketplaceState.AwaitingContact;
                await ShowMarketplaceContactInput(message.Chat.Id, cancellationToken);
                break;
            case not null when _marketplaceState == MarketplaceState.AwaitingContact:
                _marketplaceData.Contact = message.Text;
                _marketplaceState = MarketplaceState.AwaitingResult;
                _marketplaceData.Result = await ShowMarketplaceResult(message.Chat.Id, message.Chat.Username!, cancellationToken);
                break;
            case ButtonTitles.PostMarketplace:
                await PostMarketplace(message.Chat.Id, GagauziaChatId, MarketplaceThreadId, _marketplaceData.Result, cancellationToken);
                break;
        }
    }

    private void ResetAllStates()
    {
        _carpoolingState = CarpoolingState.Default;
        _marketplaceState = MarketplaceState.Default;
        _carpoolingData = new CarpoolingData();
        _marketplaceData = new MarketplaceData { 
            PhotoUrls = []
        };
    }

    #region Common Methods
    private async Task ShowMainMenu(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.NewPost)},
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "🏠 <b>Главное меню</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowChooseCategory(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Carpooling)},
            new[] {new KeyboardButton(ButtonTitles.Marketplace)},
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "📋 <b>Выберите категорию</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }
    
    #endregion

    #region Carpooling Methods
    private async Task ShowCarpoolingMenu(long chatId, CancellationToken ct)
    {
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
            [new KeyboardButton(ButtonTitles.CarpoolingDataToday), new KeyboardButton(ButtonTitles.CarpoolingDataTomorrow)],
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: messageText,
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowCarpoolingTimeInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "<b>⌛ Укажите время:</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowCarpoolingFromInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "<b>🚩 Укажите пункт отправления:</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowCarpoolingToInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "<b>🏁 Укажите пункт назначения:</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowCarpoolingPhoneInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "<b>☎ Укажите номер телефона:</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task<string> ShowCarpoolingResult(string username, long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.PostCarpooling)},
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        var text = @$"
<b>🚗 Здравствуйте, дорогие попутчики! 🚗</b>

<b>📅 Когда:</b> {_carpoolingData.Date}
<b>⏰ Во сколько:</b> {_carpoolingData.Time}
<b>📍 Откуда:</b> {_carpoolingData.From}
<b>🏁 Куда:</b> {_carpoolingData.To}
<b>📲 Контакты:</b> +373{_carpoolingData.Phone}
<b>💌👉 @{username}</b>

<i>✨ Счастливого пути! ✨</i>";
        
        await botClient.SendMessage(
            chatId: chatId,
            text: $"<b>Ваше объявление:</b>{text}",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
        return text;
    }

    private async Task PostCarpooling(long chatId, long postChatId, int? postThreadId, string text, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.MainMenu)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };
        await botClient.SendMessage(
            chatId: chatId,
            "Ваше объявление опубликовано в группе 'Попутчики!'",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: ct
        );
        
        await botClient.SendMessage(
            chatId: postChatId,
            messageThreadId: postThreadId,
            text: text,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
    #endregion

    #region Marketplace Methods
    private async Task ShowMarketplaceTitleInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "🛒 <b>Введите название товара:</b>\n\nПример: <i>Продам iPhone 13, 128GB</i>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowMarketplaceDescInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "📝 <b>Опишите товар:</b>\n\nУкажите:\n- Состояние\n- Цену\n- Особенности\n\nПример: <i>Отличное состояние, батарея 100%. Цена: 12000 лей. В комплекте чехол и зарядка.</i>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowMarketplacePhotosInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.SkipPhotos)},
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "📸 <b>Пришлите фото товара (до 10 шт.):</b>\n\nИли нажмите \"Пропустить фото\"",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }
    private async Task HandleMarketplacePhotos(Message message, CancellationToken cancellationToken)
    {
        if (_marketplaceData.PhotoUrls.Count >= 10)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "⚠️ Достигнут лимит в 10 фото. Нажмите 'Продолжить' для завершения.",
                cancellationToken: cancellationToken);
        }
        else
        {
            var photo = message.Photo!.Last();
            _marketplaceData.PhotoUrls.Add(photo.FileId);

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: $"📸 Фото добавлено ({_marketplaceData.PhotoUrls.Count}/10). Отправьте еще или нажмите 'Пропустить фото'.",
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton(ButtonTitles.SkipPhotos) },
                    new[] { new KeyboardButton(ButtonTitles.Cancel) }
                })
                {
                    ResizeKeyboard = true
                },
                cancellationToken: cancellationToken);
        }
    }
    private async Task ShowMarketplaceContactInput(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "☎ <b>Укажите номер телефона для связи:</b>",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task<string> ShowMarketplaceResult(long chatId, string username, CancellationToken ct)
    {
        var photosText = _marketplaceData.PhotoUrls.Any() 
            ? "📸 Фото прилагаются" 
            : "🖼 Без фото";

        var text = $"""
        🛒 <b>{_marketplaceData.Title}</b>
        
        {_marketplaceData.Description}
        
        {photosText}
        ☎ Телефон: +373{_marketplaceData.Contact}
        💌👉 @{username}
        """;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.PostMarketplace)},
            new[] {new KeyboardButton(ButtonTitles.Cancel)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        // Отправка превью пользователю
        await botClient.SendMessage(
            chatId: chatId,
            text: $"<b>Ваше объявление:</b>\n{text}",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        return text;
    }

    private async Task PostMarketplace(long chatId, long postChatId, int? threadId, string text, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.MainMenu)}
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        if (_marketplaceData.PhotoUrls.Any())
        {
            var media = _marketplaceData.PhotoUrls
                .Select((url, index) => new InputMediaPhoto(url)
                {
                    Caption = index == 0 ? text : null,
                    ParseMode = index == 0 ? ParseMode.Html : ParseMode.None
                })
                .ToList();

            await botClient.SendMediaGroup(
                chatId: postChatId,
                messageThreadId: threadId,
                media: media,
                cancellationToken: ct);
        }
        else
        {
            await botClient.SendMessage(
                chatId: postChatId,
                messageThreadId: threadId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }

        await botClient.SendMessage(
            chatId: chatId,
            text: "✅ Ваше объявление опубликовано в разделе 'Рынок'!",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
    #endregion
}
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services;

public class CommandService(ITelegramBotClient botClient) : ICommandService
{
    private CarpoolingData _carpoolingData;
    private CarpoolingState _carpoolingState = CarpoolingState.Default;
    private const long GagauziaChatId = -1002696920941;
    private const int CarpoolingThreadId = 15;

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
    private static class ButtonTitles
    {
        public const string MainMenu = "🏠 Главное меню";
        public const string Help = "ℹ️ Помощь";
        public const string NewPost = "📋 Разместить объявление";
        public const string PostCarpooling = "✅ Отправить попутчикам";
        public const string Cancel = "❌ Отмена";
        public const string Carpooling = "🚗 Попутчики";
        public const string CarpoolingDataToday = "🏃 Сегодня";
        public const string CarpoolingDataTomorrow = "🚶 Завтра";
        public const string Marketplace = "🛒 Рынок";
    }

    public async Task HandleCommand(Message message, CancellationToken cancellationToken)
    {
        if (message.Text == null) return;
        
        switch (message.Text)
        {
            case "/start":
                await ShowStartMenu(message.Chat.Id, cancellationToken);
                break;
            case "/menu":
            case ButtonTitles.MainMenu:
            case ButtonTitles.Cancel:
                _carpoolingData = new CarpoolingData();
                _carpoolingState = CarpoolingState.Default;
                await ShowMainMenu(message.Chat.Id, cancellationToken);
                break;
            case "/help":
            case ButtonTitles.Help:
                await ShowHelpMenu(message.Chat.Id, cancellationToken);
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
                await ShowCarpoolingFillTime(message.Chat.Id, cancellationToken);
                break;
            case ButtonTitles.CarpoolingDataTomorrow:
                _carpoolingData.Date = "Завтра";
                _carpoolingState = CarpoolingState.AwaitingCarpoolingTime;
                await ShowCarpoolingFillTime(message.Chat.Id, cancellationToken);
                break;
            case not null when _carpoolingState == CarpoolingState.AwaitingCarpoolingTime:
                _carpoolingData.Time = message.Text;
                _carpoolingState = CarpoolingState.AwaitingCarpoolingFrom;
                await ShowCarpoolingFillFrom(message.Chat.Id, cancellationToken);
                break;
            case not null when _carpoolingState == CarpoolingState.AwaitingCarpoolingFrom:
                _carpoolingData.From = message.Text;
                _carpoolingState = CarpoolingState.AwaitingCarpoolingTo;
                await ShowCarpoolingFillTo(message.Chat.Id, cancellationToken);
                break;
            case not null when _carpoolingState == CarpoolingState.AwaitingCarpoolingTo:
                _carpoolingData.To = message.Text;
                _carpoolingState = CarpoolingState.AwaitingCarpoolingPhone;
                await ShowCarpoolingFillPhone(message.Chat.Id, cancellationToken);
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
                await ShowMarketplaceMenu(message.Chat.Id, cancellationToken);
                break;
        }
    }

    private async Task ShowStartMenu(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton(ButtonTitles.MainMenu) },
            new[] { new KeyboardButton(ButtonTitles.Help) }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "👋 Добро пожаловать!\n\nЯ ваш бот-помощник,",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowMainMenu(long chatId, CancellationToken ct)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] {new KeyboardButton(ButtonTitles.NewPost)},
            new[] {new KeyboardButton(ButtonTitles.Help)}
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
    private async Task ShowCarpoolingFillTime(long chatId, CancellationToken ct)
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
    private async Task ShowCarpoolingFillFrom(long chatId, CancellationToken ct)
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
    private async Task ShowCarpoolingFillTo(long chatId, CancellationToken ct)
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
    private async Task ShowCarpoolingFillPhone(long chatId, CancellationToken ct)
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
    private async Task ShowMarketplaceMenu(long chatId, CancellationToken ct)
    {
        await botClient.SendMessage(
            chatId: chatId,
            text: "🛒 <b>Раздел Рынок</b>\n\nЗдесь вы можете купить/продать товары...",
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
    }

    private async Task ShowHelpMenu(long chatId, CancellationToken ct)
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
            text: "📌 Доступные команды:\n\n" +
                 "/start - Начать работу\n" +
                 "/menu - Главное меню\n" +
                 "/help - Помощь",
            replyMarkup: keyboard,
            cancellationToken: ct
        );
    }
}
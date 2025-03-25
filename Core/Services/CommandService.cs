using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services;

public class CommandService(ITelegramBotClient botClient) : ICommandService
{
    private static class ButtonTitles
    {
        public const string MainMenu = "🏠 Главное меню";
        public const string Help = "ℹ️ Помощь";
        public const string NewPost = "📋 Разместить объявление";
        public const string Cancel = "❌ Отмена";
        public const string Carpooling = "🚗 Попутчики";
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
                await ShowMainMenu(message.Chat.Id, cancellationToken);
                break;
            case "/help":
            case ButtonTitles.Help:
                await ShowHelpMenu(message.Chat.Id, cancellationToken);
                break;
            case ButtonTitles.NewPost:
                await ShowChooseCategory(message.Chat.Id, cancellationToken);
                break;
            case ButtonTitles.Carpooling:
                await ShowCarpoolingMenu(message.Chat.Id, cancellationToken);
                break;
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
            text: "👋 Добро пожаловать!\n\nЯ ваш бот-помощник.",
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
        await botClient.SendMessage(
            chatId: chatId,
            text: "🚗 <b>Раздел Попутчики</b>\n\nЗдесь вы можете найти попутчиков...",
            parseMode: ParseMode.Html,
            cancellationToken: ct
        );
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
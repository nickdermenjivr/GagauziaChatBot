using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GagauziaChatBot.Core.Services;

public class CommandService(ITelegramBotClient botClient) : ICommandService
{
    
    public async Task HandleCommand(Message message, CancellationToken cancellationToken)
    {
        if (message.Text == null) return;

        switch (message.Text)
        {
            case "/start":
                await ShowStartMenu(message.Chat.Id, cancellationToken);
                break;

            case "/menu":
            case "📋 Главное меню":
                await ShowMainMenu(message.Chat.Id, cancellationToken);
                break;

            case "/help":
            case "ℹ️ Помощь":
                await ShowHelpMenu(message.Chat.Id, cancellationToken);
                break;
        }
    }

    private async Task ShowStartMenu(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            [new KeyboardButton("📋 Главное меню")],
            new[] { new KeyboardButton("ℹ️ Помощь") }
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
            cancellationToken: cancellationToken
        );
    }

    private async Task ShowMainMenu(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            [new KeyboardButton("📋 Каталог"), new KeyboardButton("🛒 Корзина")],
            [new KeyboardButton("📞 Контакты"), new KeyboardButton("⚙️ Настройки")],
            new[] { new KeyboardButton("ℹ️ Помощь") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = false
        };

        await botClient.SendMessage(
            chatId: chatId,
            text: "<b>🏠 Главное меню</b>\n\nВыберите раздел:",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }
    private async Task ShowHelpMenu(long chatId, CancellationToken cancellationToken)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("📋 Главное меню") }
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
            cancellationToken: cancellationToken
        );
    }
}
using GagauziaChatBot.Core.Models;
using GagauziaChatBot.Core.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GagauziaChatBot;

internal static class Program
{
    private static async Task Main()
    {
        var settings = new BotSettings();
        var botClient = new TelegramBotClient(settings.BotToken);
        var commandService = new CommandService(botClient);
        var botService = new BotService(botClient, commandService);

        var cts = new CancellationTokenSource();

        await RegisterBotCommands(botClient, cts.Token);
        botService.StartReceiving(cts.Token);

        var me = await botClient.GetMe(cancellationToken: cts.Token);
        Console.WriteLine($"Бот @{me.Username} запущен!");

        await Task.Delay(-1, cts.Token);
    }

    private static async Task RegisterBotCommands(ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var commands = new List<BotCommand>
        {
            new() { Command = "menu", Description = "Главное меню" },
        };

        await botClient.SetMyCommands(
            commands: commands,
            scope: null,
            languageCode: null,
            cancellationToken: cancellationToken
        );
    }
}
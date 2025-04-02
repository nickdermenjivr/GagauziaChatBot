using GagauziaChatBot.Core.Configuration;
using GagauziaChatBot.Core.Services;
using GagauziaChatBot.Core.Services.CommandsService;
using GagauziaChatBot.Core.Services.NewsService;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GagauziaChatBot;

internal class Program 
{
    private static async Task Main()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var botConfig = configuration.GetSection("BotSettings").Get<BotConfig>();
        if (string.IsNullOrEmpty(botConfig?.BotToken))
        {
            Console.WriteLine("Ошибка: Токен бота не настроен");
            return;
        }

        var cts = new CancellationTokenSource();
        
        var botClient = new TelegramBotClient(botConfig.BotToken);
        var commandService = new CommandService(botClient);
        var newsService = new NewsService(botClient);
        var botService = new BotService(botClient, cts.Token, commandService, newsService);

        await RegisterBotCommands(botClient, cts.Token);
        botService.StartReceiving();
        //botService.StartNewsPostingJob();

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
            cancellationToken: cancellationToken
        );
    }
}
using GagauziaChatBot.Core.Configuration;
using GagauziaChatBot.Core.Services;
using GagauziaChatBot.Core.Services.CommandsService;
using GagauziaChatBot.Core.Services.NewsService;
using GagauziaChatBot.Core.Services.NewsService.Parsers;
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

        var botClient = new TelegramBotClient(botConfig.BotToken);
        var commandService = new CommandService(botClient);
        var botService = new BotService(botClient, commandService);

        var cts = new CancellationTokenSource();
        
        const string rssUrl = "https://nokta.md/feed";
        var interval = TimeSpan.FromSeconds(20);
        
        var httpClient = new HttpClient();
        var parser = new RssNewsParser(httpClient, rssUrl);
        var newsTask = new NewsBackgroundTask(botClient, parser, TelegramConstants.GagauziaChatId, interval);

        await RegisterBotCommands(botClient, cts.Token);
        botService.StartReceiving(cts.Token);

        var me = await botClient.GetMe(cancellationToken: cts.Token);
        Console.WriteLine($"Бот @{me.Username} запущен!");
        
        await newsTask.RunAsync(cts.Token);

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
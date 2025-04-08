using GagauziaChatBot.Core.Services;
using GagauziaChatBot.Core.Services.CommandsService;
using GagauziaChatBot.Core.Services.DiscountsService;
using GagauziaChatBot.Core.Services.NewsService;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GagauziaChatBot;

internal static class Program 
{
    private static async Task Main()
    {
        var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
        //var botToken = "8181148069:AAHGwZXVK1rLdQ45g5D8_KCf2BPqZ1Q_IkE"; //Test bot
        if (string.IsNullOrEmpty(botToken))
        {
            Console.WriteLine("❌ Ошибка: Токен бота не настроен");
            return;
        }

        var cts = new CancellationTokenSource();
        var botClient = new TelegramBotClient(botToken);
        await botClient.DeleteWebhook(cancellationToken: cts.Token);
        var commandService = new CommandService(botClient);
        var newsService = new NewsService(botClient);
        var discountsService = new DiscountsService(botClient);
        var botService = new BotService(botClient, cts.Token, commandService, newsService, discountsService);

        await RegisterBotCommands(botClient, cts.Token);
        botService.StartReceiving();
        botService.StartDiscountsPostingJob();
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
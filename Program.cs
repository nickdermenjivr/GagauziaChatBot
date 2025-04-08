using GagauziaChatBot.Core.Services;
using GagauziaChatBot.Core.Services.CommandsService;
using GagauziaChatBot.Core.Services.DiscountsService;
using GagauziaChatBot.Core.Services.NewsService;
using Microsoft.Playwright;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GagauziaChatBot;

internal static class Program 
{
    private static async Task Main()
    {
        await TestPlaywright();
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
        //botService.StartDiscountsPostingJob();
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

    private static async Task TestPlaywright()
    {
        // Выводим сообщение, чтобы убедиться, что метод вызывается
        Console.WriteLine("Запуск Playwright...");

        try
        {
            var playwright = await Playwright.CreateAsync();
            Console.WriteLine("Playwright создан.");

            var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            Console.WriteLine("Браузер запущен.");

            var page = await browser.NewPageAsync();
            Console.WriteLine("Создана новая страница.");

            await page.GotoAsync("https://example.com");
            Console.WriteLine("Перешли на страницу https://example.com");

            // Делаем скриншот страницы для дебага
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screenshot.png" });
            Console.WriteLine("Скриншот сделан.");

            await browser.CloseAsync();
            Console.WriteLine("Браузер закрыт.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка: {ex.Message}");
        }
    }
}
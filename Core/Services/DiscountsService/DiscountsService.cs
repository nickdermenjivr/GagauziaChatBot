using Telegram.Bot;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public class DiscountsService : IDiscountsService
{
    private readonly Dictionary<string, (string MainUrl, string StoreName)> _discountSources = new()
    {
        {"linella", ("https://mooldo.com/ru/shops/linella", "Линелла") },
        {"local", ("https://mooldo.com/ru/shops/local-discounter", "Локал") },
        {"kaufland", ("https://mooldo.com/ru/shops/kaufland", "Кауфлэнд") },
        {"alcomarket", ("https://mooldo.com/ru/shops/alcomarket", "Алкомаркет") },
        {"cip", ("https://mooldo.com/ru/shops/cip-market", "Чип") },
        {"dulcinella", ("https://mooldo.com/ru/shops/dulcinella", "Дулчинелла") },
        {"bomba", ("https://mooldo.com/ru/shops/bomba", "Бомба") },
        {"cleber", ("https://mooldo.com/ru/shops/cleber-md", "Клебер") },
    };

    private readonly List<DiscountsBackgroundTask> _tasks = [];

    private readonly DiscountCache _discountCache = new();
    private readonly TimeSpan _startTime = new(6, 0, 0);
    private readonly TimeSpan _endTime = new(20, 0, 0);
    private readonly TimeSpan _updateInterval = TimeSpan.FromDays(1);
    private readonly TimeSpan _taskDelayInterval = TimeSpan.FromMinutes(30);  // Интервал между задачами

    public DiscountsService(ITelegramBotClient botClient)
    {
        foreach (var (_, (mainUrl, store)) in _discountSources)
        {
            var task = new DiscountsBackgroundTask(
                botClient: botClient,
                pageUrl: mainUrl,
                storeName: store,
                discountCache: _discountCache,
                maxImages: 10,
                interval: _updateInterval,
                startTime: _startTime,
                endTime: _endTime
            );

            _tasks.Add(task);
        }
    }

    public async Task PublishCatalogAsync(CancellationToken cancellationToken)
    {
        foreach (var task in _tasks)
        {
            // Запускаем задачу поочередно
            _ = task.RunAsync(cancellationToken);

            // Ждем перед запуском следующей задачи
            await Task.Delay(_taskDelayInterval, cancellationToken);
        }
    }
}
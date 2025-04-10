using Telegram.Bot;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public class DiscountsService : IDiscountsService
{
    private readonly Dictionary<string, (string Url, string StoreName)> _discountSources = new()
    {
        { "linella", ("https://mooldo.com/ru/catalog/linella/4229/katalog-skidok", "Линелла") },
        { "local", ("https://mooldo.com/ru/catalog/local-discounter/4232/katalog-skidok", "Локал") },
        {"chip", ("https://mooldo.com/ru/catalog/cip-market/4224/katalog-skidok", "Чип")},
        {"bomba", ("https://mooldo.com/ru/catalog/bomba/4199/vesna-v-bomba-nacinaetsya-s-vygodnyx-predlozenii-i-novinok", "Бомба")},
        {"kaufland", ("https://mooldo.com/ru/catalog/kaufland/4306/katalog-skidok", "Кауфлэнд")},
    };

    private readonly List<DiscountsBackgroundTask> _tasks = [];
    private readonly TimeSpan _startTime = new(6, 0, 0);
    private readonly TimeSpan _endTime = new(20, 0, 0);
    private readonly TimeSpan _updateInterval = TimeSpan.FromDays(1);
    private readonly TimeSpan _taskDelayInterval = TimeSpan.FromMinutes(15);  // Интервал между задачами

    public DiscountsService(ITelegramBotClient botClient)
    {
        foreach (var (_, (url, store)) in _discountSources)
        {
            var task = new DiscountsBackgroundTask(
                botClient: botClient,
                pageUrl: url,
                storeName: store,
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
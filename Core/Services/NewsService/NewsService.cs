using GagauziaChatBot.Core.Configuration;
using GagauziaChatBot.Core.Services.NewsService.Parsers;
using Telegram.Bot;

namespace GagauziaChatBot.Core.Services.NewsService;

public class NewsService(ITelegramBotClient botClient) : INewsService
{
    private const int UpdateIntervalSeconds = 1200;
    private readonly Dictionary<string, (RssNewsParser Parser, NewsCache Cache, NewsBackgroundTask Task)> _newsSources = new();
    private readonly Dictionary<string, string> _rssFeeds = new()
    {
        { "Nokta", "https://nokta.md/feed" },
        { "NewsMaker", "https://newsmaker.md/ru/feed" },
        { "IpnNews", "https://www.ipn.md/ru/feed"},
        { "RiaNews", "https://ria.ru/export/rss2/archive/index.xml"}
    };

    public async Task StartNewsPostingAsync(CancellationToken cancellationToken)
    {
        InitializeComponents();

        var tasks = _newsSources.Values.Select(source => source.Task.RunAsync(cancellationToken));
        await Task.WhenAll(tasks);
    }

    private void InitializeComponents()
    {
        var httpClient = CreateHttpClient();

        foreach (var (sourceName, rssUrl) in _rssFeeds)
        {
            var parser = new RssNewsParser(httpClient, rssUrl);
            var cache = new NewsCache();
            var task = new NewsBackgroundTask(botClient, parser, cache, TelegramConstants.GagauziaChatId, TelegramConstants.NewsThreadId, TimeSpan.FromSeconds(UpdateIntervalSeconds));
            
            _newsSources[sourceName] = (parser, cache, task);
        }
    }

    private HttpClient CreateHttpClient()
    {
        return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }
}
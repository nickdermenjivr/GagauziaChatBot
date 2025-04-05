using GagauziaChatBot.Core.Configuration;
using GagauziaChatBot.Core.Services.NewsService.Parsers;
using Telegram.Bot;

namespace GagauziaChatBot.Core.Services.NewsService;

public class NewsService(ITelegramBotClient botClient) : INewsService
{
    private const int UpdateIntervalSeconds = 4000;
    private readonly Dictionary<string, (RssNewsParser Parser, NewsCache Cache, NewsBackgroundTask Task)> _newsSources = new();
    private readonly Dictionary<string, string> _rssFeeds = new()
    {
        { "rambler_crazy", "https://weekend.rambler.ru/rss/crazy-world/latest/?limit=100" },
        { "rambler_tech", "https://news.rambler.ru/rss/tech/latest/?limit=100" },
        //{ "120su", "https://120.su/feed/" },
    };
    
    private readonly TimeSpan _startTime = new (6, 0, 0);
    private readonly TimeSpan _endTime = new (20, 0, 0);

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
            var task = new NewsBackgroundTask(botClient, parser, cache, TelegramConstants.GagauziaChatId, TelegramConstants.NewsThreadId, TimeSpan.FromSeconds(UpdateIntervalSeconds), _startTime, _endTime);
            
            _newsSources[sourceName] = (parser, cache, task);
        }
    }

    private HttpClient CreateHttpClient()
    {
        return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }
}
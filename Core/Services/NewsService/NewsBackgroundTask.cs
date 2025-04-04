using GagauziaChatBot.Core.Services.NewsService.Parsers;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace GagauziaChatBot.Core.Services.NewsService;

public class NewsBackgroundTask(
    ITelegramBotClient botClient,
    RssNewsParser parser,
    NewsCache cache,
    long targetChatId,
    int targetThreadId,
    TimeSpan interval,
    TimeSpan startTime,
    TimeSpan endTime)
{
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var currentTime = DateTime.Now;
            
                if (currentTime.TimeOfDay >= startTime && currentTime.TimeOfDay < endTime)
                {
                    var news = await parser.ParseLatestAsync(ct);
                    if (news != null && !cache.Contains(news.Url))
                    {
                        await PostNewsItem(news, ct);
                        cache.Add(news.Url);
                    }
                }
                else
                {
                    Console.WriteLine("Сейчас не время для публикации новостей (допустимое время: 8:00-23:00)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            TimeSpan waitTime;
            var now = DateTime.Now;
            var tomorrowStart = now.Date.AddDays(now.TimeOfDay >= endTime ? 1 : 0).Add(startTime);
        
            if (now.TimeOfDay >= endTime || now.TimeOfDay < startTime)
            {
                waitTime = tomorrowStart - now;
                Console.WriteLine($"Ожидание до 8:00 ({waitTime.Hours} ч {waitTime.Minutes} мин)");
            }
            else
            {
                waitTime = TimeSpan.FromMilliseconds(interval.Seconds);
            }
        
            await Task.Delay(waitTime, ct);
        }
    }

    private async Task PostNewsItem(NewsItem item, CancellationToken ct)
    {
        try
        {
            var message = $"<b>{EscapeHtml(item.Title)}</b>\n\n" +
                          $"{EscapeHtml(item.Description)}\n\n" +
                          $"<a href=\"{item.Url}\">🔗 Читать полностью</a>";
            await botClient.SendMessage(
                chatId: targetChatId,
                messageThreadId:targetThreadId,
                text: message,
                parseMode: ParseMode.Html,
                linkPreviewOptions: false,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка отправки новости: {ex.Message}");
        }
    }

    private static string EscapeHtml(string text) => System.Net.WebUtility.HtmlEncode(text);
}
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
    TimeSpan interval)
{
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var news = await parser.ParseLatestAsync(ct);
                if (cache.Contains(news!.Url)) return;
                await PostNewsItem(news, ct);
                cache.Add(news.Url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            await Task.Delay(interval, ct);
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
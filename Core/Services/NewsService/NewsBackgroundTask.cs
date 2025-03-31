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
                var news = await parser.ParseAsync(ct);
                var newNews = news.Where(n => !cache.Contains(n.Url)).ToList();
                
                foreach (var item in newNews)
                {
                    await PostNewsItem(item, ct);
                    cache.Add(item.Url);
                    await Task.Delay(1000, ct); // Пауза между сообщениями
                }
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
                          $"<a href=\"{item.Url}\">{item.Url}</a>";
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
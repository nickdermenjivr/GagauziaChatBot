using GagauziaChatBot.Core.Services.NewsService.Parsers;
using Telegram.Bot;

namespace GagauziaChatBot.Core.Services.NewsService;

public class NewsBackgroundTask(
    ITelegramBotClient botClient,
    RssNewsParser parser,
    long targetChatId,
    TimeSpan interval)
{
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var news = await parser.ParseAsync(ct);
                foreach (var item in news)
                {
                    var message = $"<b>{EscapeHtml(item.Title)}</b>\n\n" +
                                  $"{EscapeHtml(item.Description)}\n\n" +
                                  $"<a href=\"{EscapeHtml(item.Url)}\">Читать полностью</a>";
                    await botClient.SendMessage(
                        chatId: targetChatId,
                        text: message,
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                        cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            await Task.Delay(interval, ct);
        }
    }
    private static string EscapeHtml(string text)
    {
        return System.Net.WebUtility.HtmlEncode(text);
    }
}
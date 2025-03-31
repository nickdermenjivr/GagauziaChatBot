using System.ServiceModel.Syndication;
using System.Xml;
using System.Net;
using System.Text.RegularExpressions;

namespace GagauziaChatBot.Core.Services.NewsService.Parsers;

public class RssNewsParser(HttpClient httpClient, string rssUrl)
{
    public async Task<List<NewsItem>> ParseAsync(CancellationToken ct)
    {
        try
        {
            var response = await httpClient.GetStreamAsync(rssUrl, ct);
            using var reader = XmlReader.Create(response, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore, // Игнорируем DTD для безопасности
                MaxCharactersFromEntities = 1024 // Лимит размера сущностей
            });
            
            var feed = SyndicationFeed.Load(reader);
            
            return feed.Items.Select(item => new NewsItem(
                Title: CleanText(item.Title?.Text),
                Description: CleanText(item.Summary?.Text),
                Url: item.Links.FirstOrDefault()?.Uri?.ToString() ?? string.Empty
            )).ToList();
        }
        catch (Exception ex)
        {
            // Логирование ошибки (в реальном проекте используйте ILogger)
            Console.WriteLine($"Ошибка парсинга RSS: {ex.Message}");
            return new List<NewsItem>();
        }
    }

    private static string CleanText(string? htmlText)
    {
        if (string.IsNullOrEmpty(htmlText)) 
            return string.Empty;

        // 1. Удаляем все HTML-теги
        var withoutTags = Regex.Replace(htmlText, "<[^>]*>", string.Empty);
        
        // 2. Заменяем HTML-сущности на обычные символы
        var decoded = WebUtility.HtmlDecode(withoutTags);
        
        // 3. Удаляем лишние пробелы и переносы строк
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }
}

public record NewsItem(string Title, string Description, string Url);
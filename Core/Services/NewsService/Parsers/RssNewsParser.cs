using System.ServiceModel.Syndication;
using System.Xml;
using System.Net;
using System.Text.RegularExpressions;

namespace GagauziaChatBot.Core.Services.NewsService.Parsers;

public class RssNewsParser(HttpClient httpClient, string rssUrl)
{
    public async Task<NewsItem?> ParseLatestAsync(CancellationToken ct)
    {
        try
        {
            var response = await httpClient.GetStreamAsync(rssUrl, ct);
            using var reader = XmlReader.Create(response, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                MaxCharactersFromEntities = 1024
            });

            var feed = SyndicationFeed.Load(reader);
            var latestItem = feed.Items.FirstOrDefault();
            if (latestItem is null) return null;

            var url = latestItem.Links.FirstOrDefault()?.Uri?.ToString();
        
            // Дополнительная проверка на guid с isPermaLink="true"
            var guid = latestItem.Id;
            if (Uri.TryCreate(guid, UriKind.Absolute, out var guidUri))
            {
                url ??= guidUri.ToString();
            }

            // Очистка URL от возможных добавочных элементов, если нужно
            url = NormalizeUrl(url);

            return new NewsItem(
                Title: CleanText(latestItem.Title?.Text),
                Description: CleanText(latestItem.Summary?.Text),
                Url: url
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга RSS: {ex.Message}");
            return null;
        }
    }

    private static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        // Убираем возможные index.php/ если это повторяющаяся ошибка
        return url.Replace("index.php/", "");
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
using HtmlAgilityPack;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public static class DiscountsParser
{
    public static async Task<List<(List<string> imageUrls, string promoText)>> ExtractPostsFromMainPage(
        string mainUrl,
        int maxImagesPerCatalog)
    {
        var posts = new List<(List<string>, string)>();

        try
        {
            var httpClient = new HttpClient();

            // 1. Загружаем HTML главной страницы
            var mainHtml = await httpClient.GetStringAsync(mainUrl);
            var mainDoc = new HtmlDocument();
            mainDoc.LoadHtml(mainHtml);

            // 2. Ищем указанный блок
            const string blockXPath = "//div[contains(@class,'grid-cols-2') and contains(@class,'gap-x-2')]";
            var blockNode = mainDoc.DocumentNode.SelectSingleNode(blockXPath);

            // 3. Ищем все ссылки внутри блока
            var linkNodes = blockNode.SelectNodes(".//a[@href]");
            if (linkNodes.Count == 0)
            {
                Console.WriteLine("Ссылки не найдены внутри блока.");
                return posts;
            }

            foreach (var linkNode in linkNodes)
            {
                var href = linkNode.GetAttributeValue("href", "").Trim();
                if (string.IsNullOrEmpty(href)) continue;

                var catalogUrl = MakeAbsoluteUrl(mainUrl, href);
                var (images, promo) = await ExtractImageUrlsAndPromoText(catalogUrl, maxImagesPerCatalog);
                posts.Add((images, promo));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга: {ex.Message}");
        }

        return posts;
    }

    private static async Task<(List<string>, string)> ExtractImageUrlsAndPromoText(string url, int maxImages)
    {
        var imageUrls = new List<string>();
        var promoText = string.Empty;

        try
        {
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'gallery')]//img")
                .Take(maxImages)
                .ToList();

            imageUrls.AddRange(from imgNode in imageNodes select imgNode.GetAttributeValue("src", "") into src where !string.IsNullOrEmpty(src) select MakeAbsoluteUrl(url, src));

            var promoNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'text-center')]//h5");
            promoText = promoNode.InnerText.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при парсинге каталога: {ex.Message}");
        }

        return (imageUrls, promoText);
    }

    private static string MakeAbsoluteUrl(string baseUrl, string relativeUrl)
    {
        return relativeUrl.StartsWith("http")
            ? relativeUrl
            : new Uri(new Uri(baseUrl), relativeUrl).AbsoluteUri;
    }
}

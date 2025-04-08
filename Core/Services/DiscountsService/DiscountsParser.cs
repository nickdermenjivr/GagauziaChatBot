using HtmlAgilityPack;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public static class DiscountsParser
{
    public static async Task<(List<string>, string)> ExtractImageUrlsAndPromoText(string url, int maxImages)
    {
        var imageUrls = new List<string>();
        var promoText = string.Empty;

        try
        {
            // 1. Загружаем HTML страницы
            var httpClient = new HttpClient();
            var html = await httpClient.GetStringAsync(url);
            
            // 2. Парсим HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 3. Извлекаем изображения
            var imageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'gallery')]//img")
                .Take(maxImages)
                .ToList();

            var index = 0;
            for (; index < imageNodes.Count; index++)
            {
                var imgNode = imageNodes[index];
                var src = imgNode.GetAttributeValue("src", "");
                if (!string.IsNullOrEmpty(src))
                {
                    imageUrls.Add(MakeAbsoluteUrl(url, src));
                }
            }

            // 4. Извлекаем текст акции
            var promoNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'text-center')]//h5");
            promoText = promoNode.InnerText.Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка парсинга: {ex.Message}");
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
using Microsoft.Playwright;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public static class DiscountsParser
{
    public static async Task<(List<string>, string)> ExtractImageUrlsAndPromoText(string url, int maxImages)
    {
        var imageUrls = new List<string>();
        var promoText = "";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var figures = await page.Locator("div.gallery figure").AllAsync();
        foreach (var fig in figures.Take(maxImages))
        {
            var src = await fig.Locator("img").First.GetAttributeAsync("src");
            if (!string.IsNullOrEmpty(src))
                imageUrls.Add(src.StartsWith("http") ? src : new Uri(new Uri(url), src).ToString());
        }

        var h5 = page.Locator("div.text-center.my-6 h5.mb-0");
        if (await h5.CountAsync() > 0)
            promoText = await h5.First.InnerTextAsync();

        return (imageUrls, promoText);
    }
}
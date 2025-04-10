using GagauziaChatBot.Core.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public class DiscountsBackgroundTask(
    ITelegramBotClient botClient,
    string pageUrl,
    string storeName,
    DiscountCache discountCache,
    int maxImages,
    TimeSpan interval,
    TimeSpan startTime,
    TimeSpan endTime)
{
    private readonly HttpClient _httpClient = new();
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "TelegramCatalogs");

    public async Task RunAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_tempFolder);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now.TimeOfDay;

                if (IsWithinPublicationTime(now))
                {
                    await PublishCatalogsAsync(ct);
                }
                else
                {
                    Console.WriteLine($"{storeName}: вне времени публикации.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{storeName}] Ошибка: {ex.Message}");
            }

            var delay = CalculateNextDelay();
            await Task.Delay(delay, ct);
        }
    }

    private bool IsWithinPublicationTime(TimeSpan currentTime)
    {
        return startTime < endTime
            ? currentTime >= startTime && currentTime < endTime
            : currentTime >= startTime || currentTime < endTime;
    }

    private TimeSpan CalculateNextDelay()
    {
        var now = DateTime.Now;
        var today = DateTime.Today;

        if (IsWithinPublicationTime(now.TimeOfDay))
            return interval;

        return now.TimeOfDay < startTime
            ? today.Add(startTime) - now
            : today.AddDays(1).Add(startTime) - now;
    }

    private async Task PublishCatalogsAsync(CancellationToken ct)
    {
        try
        {
            var posts = await DiscountsParser.ExtractPostsFromMainPage(pageUrl, maxImages);

            foreach (var (imageUrls, promoText) in posts)
            {
                if (discountCache.Contains(storeName, promoText))
                {
                    Console.WriteLine($"{storeName}: Скидка уже опубликована для периода: {promoText}");
                    continue;
                }

                var downloadedImages = await DownloadImages(imageUrls, ct);
                try
                {
                    await SendToTelegram(downloadedImages, storeName, promoText, ct);
                    discountCache.Add(storeName, promoText);
                }
                finally
                {
                    CleanupTempFiles(downloadedImages);
                }
                await Task.Delay(30000, ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{storeName}] Ошибка при публикации каталогов: {ex.Message}");
        }
    }

    private async Task<List<string>> DownloadImages(List<string> imageUrls, CancellationToken ct)
    {
        var result = new List<string>();

        foreach (var (url, _) in imageUrls.Select((u, i) => (u, i)))
        {
            var filePath = Path.Combine(_tempFolder, $"discount_{Guid.NewGuid()}.jpg");

            try
            {
                var response = await _httpClient.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                await using var fs = new FileStream(filePath, FileMode.Create);
                await response.Content.CopyToAsync(fs, ct);

                result.Add(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка скачивания {url}: {ex.Message}");
            }
        }

        return result;
    }

    private async Task SendToTelegram(List<string> imagePaths, string storeNameLocal, string promoPeriod, CancellationToken ct)
    {
        var mediaGroup = new List<IAlbumInputMedia>();
        var streams = new List<Stream>();

        try
        {
            for (var i = 0; i < imagePaths.Count; i++)
            {
                var stream = File.OpenRead(imagePaths[i]);
                streams.Add(stream);

                var media = new InputMediaPhoto(InputFile.FromStream(stream, Path.GetFileName(imagePaths[i])));

                if (i == 0)
                {
                    media.Caption = $"🛒 <b>{storeNameLocal}</b>\n📅 <i>{promoPeriod}</i>";
                    media.ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html;
                }

                mediaGroup.Add(media);
            }

            if (mediaGroup.Count > 0)
            {
                await botClient.SendMediaGroup(
                    chatId: TelegramConstants.GagauziaChatId,
                    messageThreadId: TelegramConstants.DiscountsThreadId,
                    media: mediaGroup,
                    cancellationToken: ct);
            }
        }
        finally
        {
            foreach (var s in streams)
                await s.DisposeAsync();
        }
    }

    private void CleanupTempFiles(List<string> files)
    {
        foreach (var file in files)
        {
            try { File.Delete(file); }
            catch { Console.WriteLine($"Не удалось удалить {file}"); }
        }
    }
}

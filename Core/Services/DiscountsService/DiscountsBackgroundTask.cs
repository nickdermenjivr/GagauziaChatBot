using GagauziaChatBot.Core.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public class DiscountsBackgroundTask(
    ITelegramBotClient botClient,
    string pageUrl,
    string storeName,
    int maxImages,
    TimeSpan interval,
    TimeSpan startTime,
    TimeSpan endTime)
{
    private readonly DiscountCache _discountCache = new(); // Инициализация кэша скидок
        // Добавлен кэш скидок

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
                    await PublishCatalogAsync(ct);
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
        if (startTime < endTime)
        {
            return currentTime >= startTime && currentTime < endTime;
        }
        else
        {
            return currentTime >= startTime || currentTime < endTime;
        }
    }

    private TimeSpan CalculateNextDelay()
    {
        var now = DateTime.Now;
        var today = DateTime.Today;

        if (IsWithinPublicationTime(now.TimeOfDay))
        {
            return interval;
        }

        if (now.TimeOfDay >= endTime && startTime < endTime)
        {
            return today.AddDays(1).Add(startTime) - now;
        }

        if (now.TimeOfDay < startTime)
        {
            return today.Add(startTime) - now;
        }

        return today.AddDays(1).Add(startTime) - now;
    }

    private async Task PublishCatalogAsync(CancellationToken ct)
    {
        var downloadedImages = new List<string>();

        try
        {
            var (imageUrls, promoText) = await DiscountsParser.ExtractImageUrlsAndPromoText(pageUrl, maxImages);
        
            // Используем promoText как период акции
            var promoPeriod = promoText; 

            // Проверяем, была ли уже опубликована скидка для этого магазина и периода
            if (_discountCache.Contains(storeName, promoPeriod))
            {
                Console.WriteLine($"{storeName}: Скидка уже была опубликована для этого периода.");
                return; // Пропускаем публикацию
            }

            downloadedImages = await DownloadImages(imageUrls, ct);
            await SendToTelegram(downloadedImages, storeName, promoPeriod, ct);

            // Добавляем скидку в кэш после публикации
            _discountCache.Add(storeName, promoPeriod);
        }
        finally
        {
            CleanupTempFiles(downloadedImages);
        }
    }

    private async Task<List<string>> DownloadImages(List<string> imageUrls, CancellationToken ct)
    {
        var result = new List<string>();

        foreach (var (url, i) in imageUrls.Select((u, i) => (u, i)))
        {
            var filePath = Path.Combine(_tempFolder, $"discount_{i}.jpg");

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

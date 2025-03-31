using System.Text.Json;

namespace GagauziaChatBot.Core.Services.NewsService;

public class NewsCache
{
    private const string CacheFilePath = "news_cache.json";
    private const int MaxCacheSize = 100; // Максимальное количество хранимых новостей
    private readonly Queue<string> _cachedUrls = new();

    public NewsCache()
    {
        LoadCache();
    }

    public bool Contains(string url) => _cachedUrls.Contains(url);

    public void Add(string url)
    {
        if (_cachedUrls.Count >= MaxCacheSize)
        {
            _cachedUrls.Dequeue(); // Удаляем самую старую запись
        }
        
        _cachedUrls.Enqueue(url);
        SaveCache();
    }

    private void LoadCache()
    {
        if (!File.Exists(CacheFilePath)) return;
        
        try
        {
            var json = File.ReadAllText(CacheFilePath);
            var urls = JsonSerializer.Deserialize<List<string>>(json);
            if (urls != null)
            {
                foreach (var url in urls)
                {
                    _cachedUrls.Enqueue(url);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки кэша: {ex.Message}");
        }
    }

    private void SaveCache()
    {
        try
        {
            var urls = _cachedUrls.ToList();
            var json = JsonSerializer.Serialize(urls);
            File.WriteAllText(CacheFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения кэша: {ex.Message}");
        }
    }
}
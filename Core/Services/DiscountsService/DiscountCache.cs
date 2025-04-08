using System.Text.Json;

namespace GagauziaChatBot.Core.Services.DiscountsService;

public class DiscountCache
{
    private const string CacheFilePath = "discount_cache.json";
    private const int MaxCacheSize = 100; // Максимальное количество хранимых скидок
    private readonly Queue<DiscountItem> _cachedDiscounts = new();

    public DiscountCache()
    {
        LoadCache();
    }

    // Проверка, есть ли уже скидка с таким названием магазина и периодом акции
    public bool Contains(string storeName, string promoPeriod)
    {
        return _cachedDiscounts.Any(d => d.StoreName == storeName && d.PromoPeriod == promoPeriod);
    }

    // Добавление новой скидки в кэш
    public void Add(string storeName, string promoPeriod)
    {
        if (_cachedDiscounts.Count >= MaxCacheSize)
        {
            _cachedDiscounts.Dequeue(); // Удаляем самую старую запись
        }

        _cachedDiscounts.Enqueue(new DiscountItem
        {
            StoreName = storeName,
            PromoPeriod = promoPeriod
        });

        SaveCache();
    }

    // Загрузка данных из файла кэша
    private void LoadCache()
    {
        if (!File.Exists(CacheFilePath)) return;

        try
        {
            var json = File.ReadAllText(CacheFilePath);
            var discounts = JsonSerializer.Deserialize<List<DiscountItem>>(json);
            if (discounts != null)
            {
                foreach (var discount in discounts)
                {
                    _cachedDiscounts.Enqueue(discount);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки кэша скидок: {ex.Message}");
        }
    }

    // Сохранение данных в файл кэша
    private void SaveCache()
    {
        try
        {
            var discounts = _cachedDiscounts.ToList();
            var json = JsonSerializer.Serialize(discounts);
            File.WriteAllText(CacheFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения кэша скидок: {ex.Message}");
        }
    }

    // Класс для представления скидки
    private class DiscountItem
    {
        public string? StoreName { get; init; }
        public string? PromoPeriod { get; init; }
    }
}

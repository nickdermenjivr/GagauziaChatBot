namespace GagauziaChatBot.Core.Configuration;

public static class TelegramConstants
{
    public static class ButtonTitles
    {
        public const string MainMenu = "🏠 Главное меню";
        public const string NewPost = "📋 Разместить объявление";
        public const string Post = "✅ Опубликовать";
        public const string Cancel = "❌ Отмена";
        public const string SkipPhotos = "⏭ Продолжить";
        
        // Carpooling
        public const string Carpooling = "🚗 Попутчики";
        public const string CarpoolingToday = "🏃 Сегодня";
        public const string CarpoolingTomorrow = "🚶 Завтра";
        
        // Marketplace
        public const string Marketplace = "🛒 Рынок";
    }

    public const long GagauziaChatId = -1002696920941;
    public const int CarpoolingThreadId = 15;
    public const int MarketplaceThreadId = 18;
    public const int NewsThreadId = 13;
    public static int? MainThreadId = null;
}
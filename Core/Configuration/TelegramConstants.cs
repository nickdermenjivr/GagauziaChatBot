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
        public const string CreateNew = "🆕 Создать новое";
        public const string Repost = "🔄 Переопубликовать";
        
        // Carpooling
        public const string Carpooling = "🚗 Попутчики";
        public const string CarpoolingToday = "🏃 Сегодня";
        public const string CarpoolingTomorrow = "🚶 Завтра";
        
        // Marketplace
        public const string Marketplace = "🛒 Рынок";
        
        // PrivateServices
        public const string PrivateServices = "💼 Частные услуги";
    }

    public const long GagauziaChatId = -1002696920941; // Gagauzia Chat
    //public const long GagauziaChatId = -1002625779840; // Test Chat
    public const int CarpoolingThreadId = 15;
    public const int MarketplaceThreadId = 18;
    public const int DiscountsThreadId = 817;
    public const int NewsThreadId = 13; // Gagauzia Chat
    //public const int NewsThreadId = 2; // Test Chat
    public const int PrivateServicesThreadId = 331;
    public static int? MainThreadId = null;
}
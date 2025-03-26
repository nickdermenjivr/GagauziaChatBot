namespace GagauziaChatBot.Core.Models.Posts;

public class MarketplacePost
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<string>? PhotoIds { get; set; }
    public string? Contact { get; set; }
    public string? Username { get; set; }
    
    public string ToFormattedString() => $@"🛒 <b>{Title}</b>

{Description}

{(PhotoIds != null && PhotoIds.Count != 0 ? "📸 Фото прилагаются" : "🖼 Без фото")}
☎ Телефон: +373{Contact}
💌👉 @{Username}";
}
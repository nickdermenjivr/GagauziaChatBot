namespace GagauziaChatBot.Core.Models.Posts;

public class CarpoolingPost
{
    public string? Date { get; set; }
    public string? Time { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Phone { get; set; }
    public string? Username { get; set; }
    
    public string ToFormattedString() => $@"<b>🚗 Здравствуйте, дорогие попутчики! 🚗</b>

<b>📅 Когда:</b> {Date}
<b>⏰ Во сколько:</b> {Time}
<b>📍 Откуда:</b> {From}
<b>🏁 Куда:</b> {To}
<b>📲 Контакты:</b> +373{Phone}
<b>💌👉 @{Username}</b>

<i>✨ Счастливого пути! ✨</i>";
}
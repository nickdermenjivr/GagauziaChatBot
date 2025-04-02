namespace GagauziaChatBot.Core.Services.NewsService;

public interface INewsService
{
    Task StartNewsPostingAsync(CancellationToken cancellationToken);
}
namespace GagauziaChatBot.Core.Services.DiscountsService;

public interface IDiscountsService
{
    Task PublishCatalogAsync(CancellationToken cancellationToken);
}
namespace eCommerce.OrdersService.Api.Contracts.IntegrationEvents.Products;

public record ProductNameUpdatedEvent(
    Guid ProductID,
    string? NewName
);

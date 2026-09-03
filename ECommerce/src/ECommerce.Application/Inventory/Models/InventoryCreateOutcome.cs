namespace ECommerce.Application.Inventory.Models;

public enum InventoryCreateOutcome
{
    Created = 1,
    DuplicateWarehouseListing = 2,
    NotAuthorized = 3
}

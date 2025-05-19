namespace InventorySystem;

public interface IInventoryRole
{
	bool AllowDisarming(ReferenceHub detainer);

	bool AllowUndisarming(ReferenceHub releaser);
}

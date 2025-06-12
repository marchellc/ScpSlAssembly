namespace InventorySystem.Items.Firearms.Modules;

public readonly struct DisplayAmmoValues
{
	public readonly int Magazines;

	public readonly int Chambered;

	public int Total => this.Magazines + this.Chambered;

	public DisplayAmmoValues(int magazines = 0, int chambered = 0)
	{
		this.Magazines = magazines;
		this.Chambered = chambered;
	}
}

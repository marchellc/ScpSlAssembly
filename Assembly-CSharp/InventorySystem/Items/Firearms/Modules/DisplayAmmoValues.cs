using System;

namespace InventorySystem.Items.Firearms.Modules
{
	public readonly struct DisplayAmmoValues
	{
		public int Total
		{
			get
			{
				return this.Magazines + this.Chambered;
			}
		}

		public DisplayAmmoValues(int magazines = 0, int chambered = 0)
		{
			this.Magazines = magazines;
			this.Chambered = chambered;
		}

		public readonly int Magazines;

		public readonly int Chambered;
	}
}

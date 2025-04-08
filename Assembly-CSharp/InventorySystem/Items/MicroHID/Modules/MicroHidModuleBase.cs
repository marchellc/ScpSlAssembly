using System;
using InventorySystem.Items.Autosync;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class MicroHidModuleBase : SubcomponentBase
	{
		public MicroHIDItem MicroHid { get; private set; }

		private protected MicroHIDViewmodel Viewmodel { protected get; private set; }

		protected override void OnInit()
		{
			base.OnInit();
			this.MicroHid = base.Item as MicroHIDItem;
			this.Viewmodel = this.MicroHid.ViewModel as MicroHIDViewmodel;
		}
	}
}

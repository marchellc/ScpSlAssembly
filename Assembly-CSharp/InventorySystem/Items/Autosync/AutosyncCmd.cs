using System;
using Mirror;

namespace InventorySystem.Items.Autosync
{
	public class AutosyncCmd : AutosyncWriterBase
	{
		public AutosyncCmd(ItemIdentifier item, out NetworkWriter writer)
			: base(item, out writer)
		{
		}

		public AutosyncCmd(ItemIdentifier item)
		{
			NetworkWriter networkWriter;
			base..ctor(item, out networkWriter);
		}

		protected override void HandleSending(AutosyncMessage msg)
		{
			if (!NetworkClient.ready || !NetworkClient.active)
			{
				return;
			}
			NetworkClient.Send<AutosyncMessage>(msg, 0);
		}
	}
}

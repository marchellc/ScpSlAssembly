using System;
using Mirror;

namespace InventorySystem.Items.Usables.Scp1344
{
	public readonly struct Scp1344StatusMessage : NetworkMessage
	{
		public Scp1344StatusMessage(ushort serial, Scp1344Status newState)
		{
			this.Serial = serial;
			this.NewState = newState;
		}

		public readonly ushort Serial;

		public readonly Scp1344Status NewState;
	}
}

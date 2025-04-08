using System;
using Mirror;

namespace InventorySystem.Items.Usables.Scp1344
{
	public readonly struct Scp1344DetectionMessage : NetworkMessage
	{
		public Scp1344DetectionMessage(uint detectedNetId)
		{
			this.DetectedNetId = detectedNetId;
		}

		public readonly uint DetectedNetId;
	}
}

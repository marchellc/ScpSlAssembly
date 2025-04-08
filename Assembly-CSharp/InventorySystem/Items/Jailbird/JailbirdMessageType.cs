using System;

namespace InventorySystem.Items.Jailbird
{
	public enum JailbirdMessageType
	{
		UpdateState,
		Broken,
		Holstered,
		AttackTriggered,
		AttackPerformed,
		ChargeLoadTriggered,
		ChargeFailed,
		ChargeStarted,
		Inspect
	}
}

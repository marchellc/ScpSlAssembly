using System;
using Mirror;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class InputSyncModule : MicroHidModuleBase
	{
		public bool Primary
		{
			get
			{
				return (this._lastReceived & InputSyncModule.SyncData.Primary) > InputSyncModule.SyncData.None;
			}
		}

		public bool Secondary
		{
			get
			{
				return (this._lastReceived & InputSyncModule.SyncData.Secondary) > InputSyncModule.SyncData.None;
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this._lastReceived = (InputSyncModule.SyncData)reader.ReadByte();
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this._lastSent = null;
			this._lastReceived = InputSyncModule.SyncData.None;
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.IsLocalPlayer)
			{
				return;
			}
			InputSyncModule.SyncData syncData = InputSyncModule.SyncData.None;
			if (base.GetAction(ActionName.Shoot))
			{
				syncData |= InputSyncModule.SyncData.Primary;
			}
			if (base.GetAction(ActionName.Zoom))
			{
				syncData |= InputSyncModule.SyncData.Secondary;
			}
			InputSyncModule.SyncData syncData2 = syncData;
			InputSyncModule.SyncData? lastSent = this._lastSent;
			if ((syncData2 == lastSent.GetValueOrDefault()) & (lastSent != null))
			{
				return;
			}
			this._lastSent = new InputSyncModule.SyncData?(syncData);
			this.SendCmd(new Action<NetworkWriter>(this.SerializeCmd));
		}

		private void SerializeCmd(NetworkWriter writer)
		{
			writer.WriteByte((byte)this._lastSent.Value);
		}

		private InputSyncModule.SyncData? _lastSent;

		private InputSyncModule.SyncData _lastReceived;

		[Flags]
		private enum SyncData : byte
		{
			None = 0,
			Primary = 1,
			Secondary = 2
		}
	}
}

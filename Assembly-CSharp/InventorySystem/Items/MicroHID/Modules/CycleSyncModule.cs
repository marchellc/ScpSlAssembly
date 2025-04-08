using System;
using System.Collections.Generic;
using Mirror;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class CycleSyncModule : MicroHidModuleBase
	{
		public static CycleController GetCycleController(ushort serial)
		{
			foreach (CycleSyncModule.SyncedCycleController syncedCycleController in CycleSyncModule.SyncControllers)
			{
				if (syncedCycleController.Controller.Serial == serial)
				{
					return syncedCycleController.Controller;
				}
			}
			CycleSyncModule.SyncedCycleController syncedCycleController2 = new CycleSyncModule.SyncedCycleController(serial);
			CycleSyncModule.SyncControllers.Add(syncedCycleController2);
			return syncedCycleController2.Controller;
		}

		public static MicroHidPhase GetPhase(ushort serial)
		{
			return CycleSyncModule.GetCycleController(serial).Phase;
		}

		public static MicroHidFiringMode GetFiringMode(ushort serial)
		{
			return CycleSyncModule.GetCycleController(serial).LastFiringMode;
		}

		public static void ForEachController(Action<CycleController> action)
		{
			foreach (CycleSyncModule.SyncedCycleController syncedCycleController in CycleSyncModule.SyncControllers)
			{
				action(syncedCycleController.Controller);
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			CycleSyncModule.SyncControllers.Clear();
		}

		internal override void TemplateUpdate()
		{
			base.TemplateUpdate();
			if (!NetworkServer.active)
			{
				return;
			}
			using (List<CycleSyncModule.SyncedCycleController>.Enumerator enumerator = CycleSyncModule.SyncControllers.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.NeedsResync)
					{
						this.SendRpc(new Action<NetworkWriter>(this.ServerWriteAllDelta), true);
						break;
					}
				}
			}
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
		{
			base.ServerOnPlayerConnected(hub, firstSubcomponent);
			this.SendRpc(hub, new Action<NetworkWriter>(this.ServerWriteAllActive));
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			if (NetworkServer.active)
			{
				return;
			}
			while (reader.Remaining > 0)
			{
				CycleController cycleController = CycleSyncModule.GetCycleController(reader.ReadUShort());
				cycleController.Phase = (MicroHidPhase)reader.ReadByte();
				if (cycleController.Phase != MicroHidPhase.Standby)
				{
					cycleController.LastFiringMode = (MicroHidFiringMode)reader.ReadByte();
				}
			}
		}

		private void Update()
		{
			if (!base.IsServer)
			{
				return;
			}
			if (this._instCycleController == null)
			{
				this._instCycleController = CycleSyncModule.GetCycleController(base.ItemSerial);
			}
			this._instCycleController.ServerUpdateHeldItem(base.MicroHid);
		}

		private void ServerWriteAllDelta(NetworkWriter writer)
		{
			foreach (CycleSyncModule.SyncedCycleController syncedCycleController in CycleSyncModule.SyncControllers)
			{
				if (syncedCycleController.NeedsResync)
				{
					syncedCycleController.WriteSelf(writer);
					syncedCycleController.LastSentPhase = new MicroHidPhase?(syncedCycleController.Controller.Phase);
				}
			}
		}

		private void ServerWriteAllActive(NetworkWriter writer)
		{
			foreach (CycleSyncModule.SyncedCycleController syncedCycleController in CycleSyncModule.SyncControllers)
			{
				if (syncedCycleController.Controller.Phase != MicroHidPhase.Standby)
				{
					syncedCycleController.WriteSelf(writer);
				}
			}
		}

		private static readonly List<CycleSyncModule.SyncedCycleController> SyncControllers = new List<CycleSyncModule.SyncedCycleController>();

		private CycleController _instCycleController;

		private class SyncedCycleController
		{
			public bool NeedsResync
			{
				get
				{
					MicroHidPhase? lastSentPhase = this.LastSentPhase;
					MicroHidPhase phase = this.Controller.Phase;
					return !((lastSentPhase.GetValueOrDefault() == phase) & (lastSentPhase != null));
				}
			}

			public SyncedCycleController(ushort serial)
			{
				this.Controller = new CycleController(serial);
				this.LastSentPhase = null;
			}

			public void WriteSelf(NetworkWriter writer)
			{
				writer.WriteUShort(this.Controller.Serial);
				writer.WriteByte((byte)this.Controller.Phase);
				if (this.Controller.Phase != MicroHidPhase.Standby)
				{
					writer.WriteByte((byte)this.Controller.LastFiringMode);
				}
			}

			public readonly CycleController Controller;

			public MicroHidPhase? LastSentPhase;
		}
	}
}

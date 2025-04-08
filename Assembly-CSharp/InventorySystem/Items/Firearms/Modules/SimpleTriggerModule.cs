using System;
using System.Collections.Generic;
using Mirror;

namespace InventorySystem.Items.Firearms.Modules
{
	public class SimpleTriggerModule : ModuleBase, ITriggerControllerModule
	{
		public ModuleBase[] IgnoredBusyModules { get; private set; }

		public double LastTriggerPress
		{
			get
			{
				return this.SelfData.PressTime;
			}
		}

		public double LastTriggerRelease
		{
			get
			{
				return this.SelfData.ReleaseTime;
			}
		}

		public bool TriggerHeld
		{
			get
			{
				return (base.IsLocalPlayer ? this.KeyHeld : SimpleTriggerModule.GetData(base.ItemSerial).IsHeld) && !base.PrimaryActionBlocked;
			}
		}

		private SimpleTriggerModule.ReceivedData SelfData
		{
			get
			{
				if (!base.IsLocalPlayer)
				{
					return SimpleTriggerModule.GetData(base.ItemSerial);
				}
				return this._clientData;
			}
		}

		private bool KeyHeld
		{
			get
			{
				return base.GetAction(ActionName.Shoot) && !this.PreventHoldingTrigger;
			}
		}

		private bool PreventHoldingTrigger
		{
			get
			{
				foreach (ModuleBase moduleBase in base.Firearm.Modules)
				{
					ITriggerPressPreventerModule triggerPressPreventerModule = moduleBase as ITriggerPressPreventerModule;
					if (triggerPressPreventerModule != null && triggerPressPreventerModule.ClientBlockTrigger)
					{
						return true;
					}
					IBusyIndicatorModule busyIndicatorModule = moduleBase as IBusyIndicatorModule;
					if (busyIndicatorModule != null && busyIndicatorModule.IsBusy && !this.IgnoredBusyModules.Contains(moduleBase))
					{
						return true;
					}
				}
				return false;
			}
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (!base.IsLocalPlayer)
			{
				return;
			}
			bool triggerStatus = this.TriggerHeld;
			if (this._clientData.IsHeld == triggerStatus)
			{
				return;
			}
			this._clientData.Set(triggerStatus);
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteBool(triggerStatus);
			});
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			SimpleTriggerModule.SyncData.Clear();
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			bool receivedState = reader.ReadBool();
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteBool(receivedState);
			}, true);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			SimpleTriggerModule.ReceivedData data = SimpleTriggerModule.GetData(serial);
			bool flag = reader.ReadBool();
			if (flag == data.IsHeld)
			{
				return;
			}
			data.Set(flag);
		}

		internal static SimpleTriggerModule.ReceivedData GetData(ushort serial)
		{
			return SimpleTriggerModule.SyncData.GetOrAdd(serial, () => new SimpleTriggerModule.ReceivedData());
		}

		private static readonly Dictionary<ushort, SimpleTriggerModule.ReceivedData> SyncData = new Dictionary<ushort, SimpleTriggerModule.ReceivedData>();

		private readonly SimpleTriggerModule.ReceivedData _clientData = new SimpleTriggerModule.ReceivedData();

		internal class ReceivedData
		{
			public bool IsHeld
			{
				get
				{
					return this.PressTime > this.ReleaseTime;
				}
			}

			public double PressTime { get; private set; }

			public double ReleaseTime { get; private set; }

			public void Set(bool isHeld)
			{
				if (isHeld)
				{
					this.PressTime = NetworkTime.time;
					return;
				}
				this.ReleaseTime = NetworkTime.time;
			}
		}
	}
}

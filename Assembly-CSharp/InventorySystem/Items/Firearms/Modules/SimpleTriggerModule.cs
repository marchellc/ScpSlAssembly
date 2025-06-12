using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class SimpleTriggerModule : ModuleBase, ITriggerControllerModule
{
	internal class ReceivedData
	{
		public bool IsHeld => this.PressTime > this.ReleaseTime;

		public double PressTime { get; private set; }

		public double ReleaseTime { get; private set; }

		public void Set(bool isHeld)
		{
			if (isHeld)
			{
				this.PressTime = NetworkTime.time;
			}
			else
			{
				this.ReleaseTime = NetworkTime.time;
			}
		}
	}

	private static readonly Dictionary<ushort, ReceivedData> SyncData = new Dictionary<ushort, ReceivedData>();

	private readonly ReceivedData _clientData = new ReceivedData();

	[field: SerializeField]
	public ModuleBase[] IgnoredBusyModules { get; private set; }

	public double LastTriggerPress => this.SelfData.PressTime;

	public double LastTriggerRelease => this.SelfData.ReleaseTime;

	public bool TriggerHeld
	{
		get
		{
			if (base.IsControllable ? this.KeyHeld : SimpleTriggerModule.GetData(base.ItemSerial).IsHeld)
			{
				return !base.PrimaryActionBlocked;
			}
			return false;
		}
	}

	private ReceivedData SelfData
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
			if (base.GetAction(ActionName.Shoot))
			{
				return !this.PreventHoldingTrigger;
			}
			return false;
		}
	}

	private bool PreventHoldingTrigger
	{
		get
		{
			ModuleBase[] modules = base.Firearm.Modules;
			foreach (ModuleBase moduleBase in modules)
			{
				if (moduleBase is ITriggerPressPreventerModule { ClientBlockTrigger: not false })
				{
					return true;
				}
				if (moduleBase is IBusyIndicatorModule { IsBusy: not false } && !this.IgnoredBusyModules.Contains(moduleBase))
				{
					return true;
				}
			}
			return false;
		}
	}

	private void ServerSetTrigger(bool isHeld)
	{
		this.SendRpc(delegate(NetworkWriter x)
		{
			x.WriteBool(isHeld);
		});
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!base.IsLocalPlayer)
		{
			return;
		}
		bool triggerStatus = this.TriggerHeld;
		if (this._clientData.IsHeld != triggerStatus)
		{
			this._clientData.Set(triggerStatus);
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteBool(triggerStatus);
			});
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		SimpleTriggerModule.SyncData.Clear();
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this.ServerSetTrigger(reader.ReadBool());
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		ReceivedData data = SimpleTriggerModule.GetData(serial);
		bool flag = reader.ReadBool();
		if (flag != data.IsHeld)
		{
			data.Set(flag);
		}
	}

	internal static ReceivedData GetData(ushort serial)
	{
		return SimpleTriggerModule.SyncData.GetOrAdd(serial, () => new ReceivedData());
	}
}

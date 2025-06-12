using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.AutoIcons;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Pickups;
using Mirror;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class KeycardItem : AutosyncItem, IItemDescription, IItemNametag, IDoorPermissionProvider, ICustomRADisplay
{
	protected enum MsgType : byte
	{
		Custom,
		OnKeycardUsed,
		Inspect,
		NewPlayerFullResync
	}

	public static readonly Dictionary<ushort, double> StartInspectTimes = new Dictionary<ushort, double>();

	[SerializeField]
	private ItemDescriptionType _descriptionType;

	[SerializeField]
	private bool _allowInspect;

	private readonly ClientRequestTimer _inspectRequestTimer = new ClientRequestTimer();

	private static readonly ActionName[] CancelInspectButtons = new ActionName[2]
	{
		ActionName.Shoot,
		ActionName.Zoom
	};

	private bool IsIdle
	{
		get
		{
			if (this.HasViewmodel)
			{
				if (base.ViewModel is KeycardViewmodel keycardViewmodel)
				{
					return keycardViewmodel.IsIdle;
				}
				return false;
			}
			return false;
		}
	}

	[field: SerializeField]
	public bool OpenDoorsOnThrow { get; private set; }

	[field: SerializeField]
	public KeycardGfx KeycardGfx { get; private set; }

	[field: SerializeField]
	public DetailBase[] Details { get; private set; }

	[field: SerializeField]
	public bool Customizable { get; private set; }

	public override float Weight => 0.01f + this.KeycardGfx.ExtraWeight;

	public override ItemDescriptionType DescriptionType => this._descriptionType;

	public string Name
	{
		get
		{
			DetailBase[] details = this.Details;
			for (int i = 0; i < details.Length; i++)
			{
				if (details[i] is IItemNametag itemNametag)
				{
					return itemNametag.Name;
				}
			}
			return base.ItemTypeId.GetName();
		}
	}

	public string Description
	{
		get
		{
			DetailBase[] details = this.Details;
			for (int i = 0; i < details.Length; i++)
			{
				if (details[i] is IItemDescription itemDescription)
				{
					return itemDescription.Description;
				}
			}
			return base.ItemTypeId.GetDescription();
		}
	}

	public string DisplayName => null;

	public bool CanBeDisplayed => !this.Customizable;

	public PermissionUsed PermissionsUsedCallback { get; private set; }

	public static event Action<ushort, bool> OnKeycardUsed;

	public virtual DoorPermissionFlags GetPermissions(IDoorPermissionRequester requester)
	{
		DetailBase[] details = this.Details;
		for (int i = 0; i < details.Length; i++)
		{
			if (details[i] is IDoorPermissionProvider doorPermissionProvider)
			{
				return doorPermissionProvider.GetPermissions(requester);
			}
		}
		return DoorPermissionFlags.None;
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		if (NetworkServer.active)
		{
			this.PermissionsUsedCallback = OnUsed;
			KeycardDetailSynchronizer.ServerProcessItem(this);
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		if (NetworkServer.active)
		{
			base.ServerSendPublicRpc(delegate(NetworkWriter x)
			{
				KeycardItem.WriteInspect(x, state: false);
			});
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!base.IsControllable || this._inspectRequestTimer.Busy)
		{
			return;
		}
		if (!KeycardItem.StartInspectTimes.ContainsKey(base.ItemSerial))
		{
			if (base.GetActionDown(ActionName.InspectItem) && this.IsIdle)
			{
				base.ClientSendCmd(delegate(NetworkWriter x)
				{
					KeycardItem.WriteInspect(x, state: true);
				});
				this._inspectRequestTimer.Trigger();
			}
			return;
		}
		bool flag = this.IsIdle;
		ActionName[] cancelInspectButtons = KeycardItem.CancelInspectButtons;
		foreach (ActionName action in cancelInspectButtons)
		{
			flag |= base.GetActionDown(action);
		}
		if (flag)
		{
			base.ClientSendCmd(delegate(NetworkWriter x)
			{
				KeycardItem.WriteInspect(x, state: false);
			});
			this._inspectRequestTimer.Trigger();
		}
	}

	internal override void OnTemplateReloaded(bool wasEverLoaded)
	{
		base.OnTemplateReloaded(wasEverLoaded);
		if (base.TryGetComponent<AutoIconApplier>(out var component))
		{
			component.UpdateIcon();
		}
		if (wasEverLoaded)
		{
			return;
		}
		CustomNetworkManager.OnClientReady += OnStaticDataReset;
		ReferenceHub.OnPlayerAdded += delegate(ReferenceHub hub)
		{
			if (NetworkServer.active)
			{
				this.ServerOnNewPlayerConnected(hub);
			}
		};
	}

	protected virtual void OnUsed(IDoorPermissionRequester requester, bool success)
	{
		base.ServerSendPublicRpc(delegate(NetworkWriter x)
		{
			x.WriteSubheader(MsgType.OnKeycardUsed);
			x.WriteBool(success);
		});
	}

	protected virtual void ServerOnNewPlayerConnected(ReferenceHub hub)
	{
		if (KeycardItem.StartInspectTimes.Count == 0)
		{
			return;
		}
		base.ServerSendTargetRpc(hub, delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MsgType.NewPlayerFullResync);
			foreach (KeyValuePair<ushort, double> startInspectTime in KeycardItem.StartInspectTimes)
			{
				writer.WriteUShort(startInspectTime.Key);
				writer.WriteDouble(startInspectTime.Value);
			}
		});
	}

	protected virtual void OnStaticDataReset()
	{
		KeycardItem.StartInspectTimes.Clear();
	}

	public sealed override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((MsgType)reader.ReadByte())
		{
		case MsgType.Custom:
			this.ClientProcessCustomRpcTemplate(reader, serial);
			break;
		case MsgType.OnKeycardUsed:
			KeycardItem.OnKeycardUsed?.Invoke(serial, reader.ReadBool());
			KeycardItem.StartInspectTimes.Remove(serial);
			break;
		case MsgType.Inspect:
			if (reader.ReadBool() && this._allowInspect)
			{
				KeycardItem.StartInspectTimes[serial] = NetworkTime.time;
			}
			else
			{
				KeycardItem.StartInspectTimes.Remove(serial);
			}
			break;
		case MsgType.NewPlayerFullResync:
			KeycardItem.StartInspectTimes.Clear();
			while (reader.Remaining > 0)
			{
				ushort key = reader.ReadUShort();
				double value = reader.ReadDouble();
				KeycardItem.StartInspectTimes[key] = value;
			}
			break;
		}
	}

	public sealed override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		if (reader.ReadByte() == 0)
		{
			this.ClientProcessCustomRpcInstance(reader);
		}
	}

	public sealed override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		switch ((MsgType)reader.ReadByte())
		{
		case MsgType.Custom:
			this.ServerProcessCustomCmd(reader);
			break;
		case MsgType.Inspect:
		{
			bool num = reader.ReadBool();
			bool flag = KeycardItem.StartInspectTimes.ContainsKey(base.ItemSerial);
			if (!num)
			{
				base.ServerSendPublicRpc(delegate(NetworkWriter x)
				{
					KeycardItem.WriteInspect(x, state: false);
				});
			}
			else if (!flag)
			{
				base.ServerSendPublicRpc(delegate(NetworkWriter x)
				{
					KeycardItem.WriteInspect(x, state: true);
				});
			}
			break;
		}
		}
	}

	protected virtual void ClientProcessCustomRpcTemplate(NetworkReader reader, ushort serial)
	{
	}

	protected virtual void ClientProcessCustomRpcInstance(NetworkReader reader)
	{
	}

	protected virtual void ServerProcessCustomCmd(NetworkReader reader)
	{
	}

	private static void WriteInspect(NetworkWriter writer, bool state)
	{
		writer.WriteSubheader(MsgType.Inspect);
		writer.WriteBool(state);
	}
}

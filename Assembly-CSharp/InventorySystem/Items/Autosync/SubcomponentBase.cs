using System;
using System.Linq;
using InventorySystem.Items.Pickups;
using Mirror;
using Scp914;
using UnityEngine;

namespace InventorySystem.Items.Autosync;

public abstract class SubcomponentBase : MonoBehaviour, IAutosyncReceiver, IIdentifierProvider
{
	public virtual bool AllowCmdsWhileHolstered => false;

	[field: SerializeField]
	[field: HideInInspector]
	public int UniqueComponentId { get; private set; }

	public bool Initialized { get; private set; }

	public ModularAutosyncItem Item { get; private set; }

	public byte SyncId { get; private set; }

	protected bool IsLocalPlayer => this.Item.IsLocalPlayer;

	protected bool IsSpectator => this.Item.IsSpectator;

	protected bool IsControllable => this.Item.IsControllable;

	protected ushort ItemSerial => this.Item.ItemSerial;

	protected bool IsServer => this.Item.IsServer;

	protected bool HasViewmodel => this.Item.HasViewmodel;

	protected bool PrimaryActionBlocked => this.Item.PrimaryActionBlocked;

	protected bool ItemUsageBlocked => this.Item.ItemUsageBlocked;

	public ItemIdentifier ItemId => this.Item.ItemId;

	protected bool GetActionDown(ActionName action)
	{
		return this.Item.GetActionDown(action);
	}

	protected bool GetAction(ActionName action)
	{
		return this.Item.GetAction(action);
	}

	protected virtual void SendRpc(Action<NetworkWriter> writerFunc = null, bool toAll = true)
	{
		NetworkWriter writer;
		if (toAll)
		{
			using (new AutosyncRpc(this.ItemId, out writer))
			{
				writer.WriteByte(this.SyncId);
				writerFunc?.Invoke(writer);
				return;
			}
		}
		this.SendRpc(this.Item.Owner, writerFunc);
	}

	protected virtual void SendRpc(Func<ReferenceHub, bool> condition, Action<NetworkWriter> writerFunc = null)
	{
		NetworkWriter writer;
		using (new AutosyncRpc(this.ItemId, condition, out writer))
		{
			writer.WriteByte(this.SyncId);
			writerFunc?.Invoke(writer);
		}
	}

	protected virtual void SendRpc(ReferenceHub targetPlayer, Action<NetworkWriter> writerFunc = null)
	{
		NetworkWriter writer;
		using (new AutosyncRpc(this.ItemId, targetPlayer, out writer))
		{
			writer.WriteByte(this.SyncId);
			writerFunc?.Invoke(writer);
		}
	}

	protected virtual void SendCmd(Action<NetworkWriter> writerFunc = null)
	{
		NetworkWriter writer;
		using (new AutosyncCmd(this.ItemId, out writer))
		{
			writer.WriteByte(this.SyncId);
			writerFunc?.Invoke(writer);
		}
	}

	protected virtual void OnValidate()
	{
		if (this.UniqueComponentId == 0 && base.transform.TryGetComponentInParent<ModularAutosyncItem>(out var comp))
		{
			SubcomponentBase[] componentsInChildren = comp.GetComponentsInChildren<SubcomponentBase>();
			int newId;
			for (newId = base.GetInstanceID(); newId == 0 || componentsInChildren.Any((SubcomponentBase x) => x.UniqueComponentId == newId); newId++)
			{
			}
			this.UniqueComponentId = newId;
		}
	}

	internal void Init(ModularAutosyncItem item, byte syncIndex)
	{
		this.Item = item;
		this.SyncId = syncIndex;
		this.Initialized = true;
		this.OnInit();
	}

	internal virtual void SpectatorInit()
	{
	}

	internal virtual void ServerOnPlayerConnected(ReferenceHub hub, bool firstSubcomponent)
	{
	}

	internal virtual void ServerProcessMapgenDistribution(ItemPickupBase pickup)
	{
	}

	internal virtual void ServerProcessScp914Creation(ushort serial, Scp914KnobSetting knobSetting, Scp914Result scp914Result, ItemType itemType)
	{
	}

	internal virtual void OnClientReady()
	{
	}

	internal virtual void TemplateUpdate()
	{
	}

	internal virtual void ServerOnPickedUp(ItemPickupBase ipb)
	{
	}

	internal virtual void OnAdded()
	{
	}

	internal virtual void OnEquipped()
	{
	}

	internal virtual void EquipUpdate()
	{
	}

	internal virtual void AlwaysUpdate()
	{
	}

	internal virtual void OnHolstered()
	{
	}

	internal virtual void OnRemoved(ItemPickupBase pickup)
	{
	}

	internal virtual void OnTemplateReloaded(ModularAutosyncItem template, bool wasEverLoaded)
	{
	}

	public virtual void ServerProcessCmd(NetworkReader reader)
	{
	}

	public virtual void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
	}

	public virtual void ClientProcessRpcInstance(NetworkReader reader)
	{
	}

	protected virtual void OnInit()
	{
	}
}

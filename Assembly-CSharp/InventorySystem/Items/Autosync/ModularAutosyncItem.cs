using System;
using System.Collections.Generic;
using InventorySystem.Drawers;
using InventorySystem.GUI.Descriptions;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using UnityEngine;

namespace InventorySystem.Items.Autosync;

public abstract class ModularAutosyncItem : AutosyncItem, IExternalMobilityControllerItem, IZoomModifyingItem, ILightEmittingItem, IAmmoDropPreventer, IHumeShieldProvider, ICustomDescriptionItem, IItemAlertDrawer, IItemDrawer
{
	private AutosyncModifiersCombiner _modifiersCombiner;

	private readonly Dictionary<int, SubcomponentBase> _subcomponentsByIdCache = new Dictionary<int, SubcomponentBase>();

	private readonly ClientRequestTimer _holsterRequestTimer = new ClientRequestTimer();

	private static readonly HashSet<Type> NewPlayerSyncModuleTypes = new HashSet<Type>();

	private static readonly List<ModularAutosyncItem> AllTemplates = new List<ModularAutosyncItem>();

	public const byte MainSyncHeader = byte.MaxValue;

	public object DesignatedMobilityControllerClass => this._modifiersCombiner;

	public bool IsHolstering => this._holsterRequestTimer.Busy;

	[field: SerializeField]
	public SubcomponentBase[] AllSubcomponents { get; protected set; }

	public AutosyncInstantiationStatus InstantiationStatus { get; set; }

	public bool HasOwner => this.InstantiationStatus == AutosyncInstantiationStatus.InventoryInstance;

	public bool PrimaryActionBlocked
	{
		get
		{
			if (this.HasOwner)
			{
				return base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction);
			}
			return false;
		}
	}

	public bool ItemUsageBlocked
	{
		get
		{
			if (this.HasOwner)
			{
				return base.Owner.HasBlock(BlockedInteraction.ItemUsage);
			}
			return false;
		}
	}

	public bool IsServer { get; private set; }

	internal override bool IsLocalPlayer
	{
		get
		{
			if (this.HasOwner)
			{
				return base.IsLocalPlayer;
			}
			return false;
		}
	}

	internal override bool IsDummy
	{
		get
		{
			if (this.HasOwner)
			{
				return base.IsDummy;
			}
			return false;
		}
	}

	public float ZoomAmount => this._modifiersCombiner.ZoomAmount;

	public float SensitivityScale => this._modifiersCombiner.SensitivityScale;

	public bool IsEmittingLight => this._modifiersCombiner.IsEmittingLight;

	public bool ForceBarVisible => this._modifiersCombiner.ForceBarVisible;

	public float HsMax => this._modifiersCombiner.HsMax;

	public float HsRegeneration => this._modifiersCombiner.HsRegeneration;

	public Color? HsWarningColor => this._modifiersCombiner.HsWarningColor;

	public virtual CustomDescriptionGui CustomGuiPrefab => this._modifiersCombiner.CustomGuiPrefab;

	public virtual string[] CustomDescriptionContent => this._modifiersCombiner.CustomDescriptionContent;

	public virtual AlertContent Alert => this._modifiersCombiner.Alert;

	public virtual void InitializeSubcomponents()
	{
		this._modifiersCombiner = new AutosyncModifiersCombiner(this);
		for (byte b = 0; b < this.AllSubcomponents.Length; b++)
		{
			SubcomponentBase subcomponentBase = this.AllSubcomponents[b];
			try
			{
				subcomponentBase.Init(this, b);
			}
			catch (Exception exception)
			{
				if (subcomponentBase == null)
				{
					Debug.LogError("Null subcomponent on " + base.ItemTypeId.ToString() + " with index " + b);
				}
				else
				{
					Debug.LogError("Subcomponent " + subcomponentBase.name + " failed to init!");
				}
				Debug.LogException(exception);
			}
		}
	}

	public sealed override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this.DecodeAndProcessMessage(reader, delegate(IAutosyncReceiver x)
		{
			x.ServerProcessCmd(reader);
		}, delegate
		{
			this.ServerProcessMainCmd(reader);
		}, checkCmd: true);
	}

	public sealed override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		this.DecodeAndProcessMessage(reader, delegate(IAutosyncReceiver x)
		{
			x.ClientProcessRpcInstance(reader);
		}, delegate
		{
			this.ClientProcessMainRpcInstance(reader);
		});
	}

	public sealed override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		this.DecodeAndProcessMessage(reader, delegate(IAutosyncReceiver x)
		{
			x.ClientProcessRpcTemplate(reader, serial);
		}, delegate
		{
			this.ClientProcessMainRpcTemplate(reader, serial);
		});
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		this.IsServer = NetworkServer.active;
		this.InstantiationStatus = AutosyncInstantiationStatus.InventoryInstance;
		this.InitializeSubcomponents();
		SubcomponentBase[] allSubcomponents;
		if (this.IsServer && pickup != null)
		{
			allSubcomponents = this.AllSubcomponents;
			for (int i = 0; i < allSubcomponents.Length; i++)
			{
				allSubcomponents[i].ServerOnPickedUp(pickup);
			}
		}
		allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].OnAdded();
		}
	}

	public override void OnEquipped()
	{
		base.OnEquipped();
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].OnEquipped();
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		this._holsterRequestTimer.Reset();
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].OnHolstered();
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].EquipUpdate();
		}
	}

	public override void AlwaysUpdate()
	{
		base.AlwaysUpdate();
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].AlwaysUpdate();
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].OnRemoved(pickup);
		}
	}

	public override void OnHolsterRequestSent()
	{
		base.OnHolsterRequestSent();
		this._holsterRequestTimer.Trigger();
	}

	internal override void OnTemplateReloaded(bool wasEverLoaded)
	{
		base.OnTemplateReloaded(wasEverLoaded);
		this.InstantiationStatus = AutosyncInstantiationStatus.Template;
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].OnTemplateReloaded(this, wasEverLoaded);
		}
		if (!wasEverLoaded)
		{
			this.InitializeSubcomponents();
			CustomNetworkManager.OnClientReady += OnClientReady;
			StaticUnityMethods.OnUpdate += OnTemplateUpdate;
			ModularAutosyncItem.AllTemplates.Add(this);
		}
	}

	protected virtual void OnClientReady()
	{
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].OnClientReady();
		}
	}

	protected virtual void OnTemplateUpdate()
	{
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			allSubcomponents[i].TemplateUpdate();
		}
	}

	protected virtual void ServerProcessMainCmd(NetworkReader reader)
	{
	}

	protected virtual void ClientProcessMainRpcInstance(NetworkReader reader)
	{
	}

	protected virtual void ClientProcessMainRpcTemplate(NetworkReader reader, ushort serial)
	{
	}

	public bool TryGetSubcomponentFromId(int id, out SubcomponentBase subcomponent)
	{
		if (this._subcomponentsByIdCache.TryGetValue(id, out subcomponent))
		{
			return true;
		}
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		foreach (SubcomponentBase subcomponentBase in allSubcomponents)
		{
			if (subcomponentBase.UniqueComponentId == id)
			{
				subcomponent = subcomponentBase;
				this._subcomponentsByIdCache[id] = subcomponentBase;
				return true;
			}
		}
		return false;
	}

	public bool TryGetSubcomponent<T>(out T ret)
	{
		SubcomponentBase[] allSubcomponents = this.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			if (allSubcomponents[i] is T val)
			{
				ret = val;
				return true;
			}
		}
		ret = default(T);
		return false;
	}

	public bool ValidateAmmoDrop(ItemType id)
	{
		return this._modifiersCombiner.ValidateAmmoDrop(id);
	}

	private void DecodeAndProcessMessage(NetworkReader reader, Action<IAutosyncReceiver> interpreter, Action main, bool checkCmd = false)
	{
		byte b = reader.ReadByte();
		SubcomponentBase element;
		if (b == byte.MaxValue)
		{
			main();
		}
		else if (this.AllSubcomponents.TryGet(b, out element) && (!checkCmd || base.IsEquipped || element.AllowCmdsWhileHolstered))
		{
			interpreter(element);
		}
	}

	private static void OnHubAdded(ReferenceHub hub)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		ModularAutosyncItem.NewPlayerSyncModuleTypes.Clear();
		foreach (ModularAutosyncItem allTemplate in ModularAutosyncItem.AllTemplates)
		{
			SubcomponentBase[] allSubcomponents = allTemplate.AllSubcomponents;
			foreach (SubcomponentBase obj in allSubcomponents)
			{
				Type type = obj.GetType();
				bool firstSubcomponent = ModularAutosyncItem.NewPlayerSyncModuleTypes.Add(type);
				obj.ServerOnPlayerConnected(hub, firstSubcomponent);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		ReferenceHub.OnPlayerAdded += OnHubAdded;
	}
}

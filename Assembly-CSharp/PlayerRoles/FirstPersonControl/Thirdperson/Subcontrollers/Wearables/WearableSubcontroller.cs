using GameObjectPools;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

public class WearableSubcontroller : SubcontrollerBehaviour, IPoolResettable
{
	private WearableElements? _syncWearable;

	[field: SerializeField]
	public DisplayableWearableBase[] DefinedWearables { get; private set; }

	public WearableElements SyncWearable
	{
		get
		{
			return this._syncWearable.GetValueOrDefault();
		}
		private set
		{
			if (this._syncWearable != value)
			{
				this._syncWearable = value;
				DisplayableWearableBase[] definedWearables = this.DefinedWearables;
				for (int i = 0; i < definedWearables.Length; i++)
				{
					definedWearables[i].OnFlagsUpdated();
				}
			}
		}
	}

	public void ResetObject()
	{
		this.SyncWearable = WearableElements.None;
		this.SetWearables(WearableElements.None);
	}

	public override void OnReassigned()
	{
		base.OnReassigned();
		this._syncWearable = null;
		if (WearableSync.TryGetData(base.OwnerHub, out var data))
		{
			this.ClientReceiveWearables(data.Flags, data.GetPayloadReader());
		}
		else
		{
			this.SyncWearable = WearableElements.None;
		}
		if (base.HasCuller && !base.Culler.IsCulled)
		{
			this.SetWearables(this.SyncWearable);
		}
	}

	public override void Init(AnimatedCharacterModel model, int index)
	{
		base.Init(model, index);
		model.OnFadeChanged += OnFadeChanged;
		model.OnVisibilityChanged += OnVisibilityChanged;
		if (base.HasCuller)
		{
			base.Culler.OnCullChanged += OnCulllChanged;
		}
		DisplayableWearableBase[] definedWearables = this.DefinedWearables;
		foreach (DisplayableWearableBase displayableWearableBase in definedWearables)
		{
			displayableWearableBase.Initialize(this);
			int id = (int)displayableWearableBase.Id;
			if ((id & (id - 1)) != 0 || id == 0)
			{
				Debug.LogError("Invalid ID for wearable '" + displayableWearableBase.name + "' on model '" + model.name + "'.");
			}
		}
	}

	public void WriteWearableSyncvars(NetworkWriter writer, WearableElements mask)
	{
		DisplayableWearableBase[] definedWearables = this.DefinedWearables;
		foreach (DisplayableWearableBase displayableWearableBase in definedWearables)
		{
			if ((displayableWearableBase.Id & mask) != WearableElements.None)
			{
				displayableWearableBase.WriteSyncvars(writer);
			}
		}
	}

	public override void ProcessRpc(NetworkReader reader)
	{
		base.ProcessRpc(reader);
		byte wearable = reader.ReadByte();
		if (this.TryGetWearable<DisplayableWearableBase>((WearableElements)wearable, out var ret))
		{
			ret.ProcessRpc(reader);
		}
	}

	private void OnVisibilityChanged()
	{
		this.SetWearables(this.SyncWearable);
	}

	private void OnFadeChanged()
	{
		float fade = base.Model.Fade;
		DisplayableWearableBase[] definedWearables = this.DefinedWearables;
		for (int i = 0; i < definedWearables.Length; i++)
		{
			definedWearables[i].SetFade(fade);
		}
	}

	private void OnCulllChanged()
	{
		if (base.Culler.IsCulled)
		{
			this.SetWearables(WearableElements.None);
		}
		else
		{
			this.SetWearables(this.SyncWearable);
		}
	}

	private void SetWearables(WearableElements elements)
	{
		DisplayableWearableBase[] definedWearables = this.DefinedWearables;
		foreach (DisplayableWearableBase obj in definedWearables)
		{
			obj.SetVisible((obj.Id & elements) != WearableElements.None && base.Model.IsVisible);
		}
	}

	public void ClientReceiveWearables(WearableElements sync, NetworkReader payload)
	{
		this.SyncWearable = sync;
		this.SetWearables(sync);
		DisplayableWearableBase[] definedWearables = this.DefinedWearables;
		foreach (DisplayableWearableBase displayableWearableBase in definedWearables)
		{
			if (displayableWearableBase.IsWorn)
			{
				displayableWearableBase.ApplySyncvars(payload);
			}
		}
	}

	public bool TryGetWearable<T>(WearableElements wearable, out T ret)
	{
		DisplayableWearableBase[] definedWearables = this.DefinedWearables;
		foreach (DisplayableWearableBase displayableWearableBase in definedWearables)
		{
			if (displayableWearableBase.Id == wearable && displayableWearableBase is T val)
			{
				ret = val;
				return true;
			}
		}
		ret = default(T);
		return false;
	}
}

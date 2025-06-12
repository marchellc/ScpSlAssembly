using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

public abstract class DisplayableWearableBase : MonoBehaviour
{
	private Action<NetworkWriter> _lastWriter;

	private bool _prevWorn;

	[field: SerializeField]
	public WearableElements Id { get; private set; }

	public bool IsVisible { get; private set; }

	public bool IsWorn => (this.Subcontroller.SyncWearable & this.Id) != 0;

	public AnimatedCharacterModel Model => this.Subcontroller.Model;

	public Animator Animator => this.Model.Animator;

	public WearableSubcontroller Subcontroller { get; private set; }

	public virtual void Initialize(WearableSubcontroller subcontroller)
	{
		this.Subcontroller = subcontroller;
	}

	public virtual void OnFlagsUpdated()
	{
		if (this._prevWorn != this.IsWorn)
		{
			this._prevWorn = !this._prevWorn;
			this.OnWornStatusChanged();
		}
	}

	public virtual void OnWornStatusChanged()
	{
	}

	public virtual void UpdateVisibility()
	{
	}

	public virtual void WriteSyncvars(NetworkWriter writer)
	{
	}

	public virtual void ApplySyncvars(NetworkReader reader)
	{
	}

	public void SetVisible(bool newVisible)
	{
		if (newVisible != this.IsVisible)
		{
			this.IsVisible = newVisible;
			this.UpdateVisibility();
		}
	}

	public abstract void SetFade(float fade);

	public virtual void ProcessRpc(NetworkReader reader)
	{
	}

	public void SendRpc(Action<NetworkWriter> extraData = null)
	{
		this._lastWriter = extraData;
		SubcontrollerRpcHandler.ServerSendRpc(this.Subcontroller, WriteRpc);
	}

	private void WriteRpc(NetworkWriter writer)
	{
		writer.WriteByte((byte)this.Id);
		this._lastWriter?.Invoke(writer);
	}
}

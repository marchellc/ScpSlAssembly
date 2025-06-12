using InventorySystem.Items.Thirdperson;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.OverlayAnims;

public abstract class OverlayAnimationsBase
{
	private static readonly int HashReplayInstant = Animator.StringToHash("FlavorOverlayTriggerInstant");

	private static readonly int HashReplaySoft = Animator.StringToHash("FlavorOverlayTriggerSoft");

	public bool IsPlaying { get; private set; }

	protected byte SyncIndex { get; private set; }

	protected OverlayAnimationsSubcontroller Controller { get; private set; }

	public AnimatedCharacterModel Model { get; private set; }

	public abstract bool WantsToPlay { get; }

	public abstract bool Bypassable { get; }

	public abstract AnimationClip Clip { get; }

	public abstract float GetLayerWeight(AnimItemLayer3p layer);

	public virtual void UpdateActive()
	{
	}

	public virtual void OnStarted()
	{
		this.IsPlaying = true;
	}

	public virtual void OnStopped()
	{
		this.IsPlaying = false;
	}

	public virtual void OnReassigned()
	{
	}

	public virtual void OnReset()
	{
	}

	public virtual void ProcessRpc(NetworkReader reader)
	{
	}

	public virtual void Init(OverlayAnimationsSubcontroller ctrl, int index)
	{
		this.Controller = ctrl;
		this.SyncIndex = (byte)index;
		this.Model = ctrl.Model;
	}

	public void SendRpc()
	{
		SubcontrollerRpcHandler.ServerSendRpc(this.Controller, WriteRpcHeader);
	}

	public void Replay(bool instant)
	{
		if (this.IsPlaying)
		{
			this.Model.Animator.SetTrigger(instant ? OverlayAnimationsBase.HashReplayInstant : OverlayAnimationsBase.HashReplaySoft);
		}
	}

	private void WriteRpcHeader(NetworkWriter writer)
	{
		writer.WriteByte(this.SyncIndex);
	}
}

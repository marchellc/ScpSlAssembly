using System.Diagnostics;
using AudioPooling;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Map;

public abstract class Scp079ToggleMenuAbilityBase<T> : Scp079KeyAbilityBase where T : Scp079ToggleMenuAbilityBase<T>
{
	[SerializeField]
	private float _cooldown;

	[SerializeField]
	private AudioClip _soundOpen;

	[SerializeField]
	private AudioClip _soundClose;

	private string _openTxt;

	private string _closeTxt;

	private static bool _trackedInstanceSet;

	private static Scp079ToggleMenuAbilityBase<T> _trackedInstance;

	private static readonly Stopwatch CooldownSw = Stopwatch.StartNew();

	public override bool IsReady => Scp079ToggleMenuAbilityBase<T>.CooldownSw.Elapsed.TotalSeconds > (double)this._cooldown;

	public override string AbilityName
	{
		get
		{
			if (!this.SyncState)
			{
				return this._openTxt;
			}
			return this._closeTxt;
		}
	}

	public override string FailMessage => null;

	protected bool SyncState { get; set; }

	protected abstract Scp079HudTranslation OpenTranslation { get; }

	protected abstract Scp079HudTranslation CloseTranslation { get; }

	public static bool IsOpen
	{
		get
		{
			if (Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet)
			{
				return Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState;
			}
			return false;
		}
		internal set
		{
			if (Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet && Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState != value)
			{
				Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState = value;
				Scp079ToggleMenuAbilityBase<T>.CooldownSw.Restart();
				if (Scp079ToggleMenuAbilityBase<T>._trackedInstance.Role.IsLocalPlayer)
				{
					Scp079ToggleMenuAbilityBase<T>._trackedInstance.ClientSendCmd();
				}
			}
		}
	}

	public static bool Visible
	{
		get
		{
			if (Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet)
			{
				if (!Scp079ToggleMenuAbilityBase<T>._trackedInstance.SyncState)
				{
					return !Scp079ToggleMenuAbilityBase<T>._trackedInstance.IsReady;
				}
				return true;
			}
			return false;
		}
	}

	private void OnSpectatorTargetChanged()
	{
		if (base.CastRole.IsSpectated || base.CastRole.IsLocalPlayer)
		{
			Scp079ToggleMenuAbilityBase<T>._trackedInstance = this;
			Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = true;
		}
		else
		{
			Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = false;
		}
	}

	private void PlaySound()
	{
		AudioSourcePoolManager.Play2D(this.SyncState ? this._soundOpen : this._soundClose);
	}

	protected override void Trigger()
	{
		if (base.CurrentCamSync.CurClientSwitchState == Scp079CurrentCameraSync.ClientSwitchState.None)
		{
			Scp079ToggleMenuAbilityBase<T>.IsOpen = !Scp079ToggleMenuAbilityBase<T>.IsOpen;
			this.PlaySound();
		}
	}

	protected override void Start()
	{
		base.Start();
		this._openTxt = Translations.Get(this.OpenTranslation);
		this._closeTxt = Translations.Get(this.CloseTranslation);
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteBool(this.SyncState);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this.SyncState = reader.ReadBool();
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBool(this.SyncState);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		bool syncState = this.SyncState;
		this.SyncState = reader.ReadBool();
		if (syncState != this.SyncState && base.Owner.IsLocallySpectated() && base.CastRole.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out var subroutine) && subroutine.CurClientSwitchState == Scp079CurrentCameraSync.ClientSwitchState.None)
		{
			this.PlaySound();
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		ReferenceHub.OnPlayerAdded += base.ServerSendRpc;
		SpectatorTargetTracker.OnTargetChanged += OnSpectatorTargetChanged;
		if (base.Role.IsLocalPlayer)
		{
			Scp079ToggleMenuAbilityBase<T>._trackedInstance = this;
			Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = true;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		ReferenceHub.OnPlayerAdded -= base.ServerSendRpc;
		SpectatorTargetTracker.OnTargetChanged -= OnSpectatorTargetChanged;
		this.SyncState = false;
		if (Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet && !(this != Scp079ToggleMenuAbilityBase<T>._trackedInstance))
		{
			Scp079ToggleMenuAbilityBase<T>._trackedInstanceSet = false;
		}
	}
}

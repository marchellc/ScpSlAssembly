using CursorManagement;
using GameObjectPools;
using LabApi.Events.Arguments.Scp106Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public class Scp106SinkholeController : SubroutineBase, ICursorOverride, IPoolResettable, IPoolSpawnable
{
	public delegate void SubmergeStateChanged(Scp106Role scp106, bool newTargetSubmerged);

	private const float AudioFadeIntensity = 8f;

	private const float AudioFadeAbs = 0.07f;

	private const float EmergeCooldownDuration = 5f;

	private float _toggleTime;

	private int _vigorAbilitiesCount;

	private int _lastActiveVigorAbility;

	private Scp106VigorAbilityBase[] _vigorAbilities;

	private readonly InconsistentAbilityCooldown _submergeCooldown = new InconsistentAbilityCooldown();

	[SerializeField]
	private AudioClip _emergeSound;

	[SerializeField]
	private AudioClip _submergeSound;

	[SerializeField]
	private AudioSource _toggleAudioSource;

	private float CurTime => Time.timeSinceLevelLoad;

	public CursorOverrideMode CursorOverride => CursorOverrideMode.NoOverride;

	public bool LockMovement
	{
		get
		{
			if (base.Role.IsLocalPlayer)
			{
				return this.IsDuringAnimation;
			}
			return false;
		}
	}

	public float ElapsedToggle => this.CurTime - this._toggleTime;

	public bool IsDuringAnimation => this.ElapsedToggle < this.TargetTransitionDuration;

	public IAbilityCooldown ReadonlyCooldown => this._submergeCooldown;

	public bool IsHidden
	{
		get
		{
			if (this.TargetSubmerged)
			{
				return !this.IsDuringAnimation;
			}
			return false;
		}
	}

	public bool TargetSubmerged { get; private set; }

	public float TargetTransitionDuration { get; private set; }

	public float SubmergeProgress
	{
		get
		{
			float num = this.ElapsedToggle / this.TargetTransitionDuration;
			if (!this.TargetSubmerged)
			{
				num = 1f - num;
			}
			return Mathf.Clamp01(num);
		}
	}

	public static event SubmergeStateChanged OnSubmergeStateChange;

	public void ModifyCooldown(double modifyAmount)
	{
		this._submergeCooldown.NextUse += modifyAmount;
		base.ServerSendRpc(toAll: true);
	}

	public void SpawnObject()
	{
		CursorManager.Register(this);
	}

	public void ResetObject()
	{
		CursorManager.Unregister(this);
		this._submergeCooldown.Clear();
		this.TargetSubmerged = false;
		this._toggleTime = 0f;
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBool(this.TargetSubmerged);
		writer.WriteFloat(this.TargetTransitionDuration);
		this._submergeCooldown.WriteCooldown(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		bool targetSubmerged = this.TargetSubmerged;
		this.TargetSubmerged = reader.ReadBool();
		this.TargetTransitionDuration = reader.ReadFloat();
		this._submergeCooldown.ReadCooldown(reader);
		if (targetSubmerged != this.TargetSubmerged)
		{
			this._toggleTime = this.CurTime;
			this._toggleAudioSource.PlayOneShot(this.TargetSubmerged ? this._submergeSound : this._emergeSound);
			Scp106SinkholeController.OnSubmergeStateChange?.Invoke(base.Role as Scp106Role, this.TargetSubmerged);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		SubroutineBase[] allSubroutines = (base.Role as Scp106Role).SubroutineModule.AllSubroutines;
		this._vigorAbilities = new Scp106VigorAbilityBase[allSubroutines.Length];
		this._vigorAbilitiesCount = 0;
		for (int i = 0; i < allSubroutines.Length; i++)
		{
			if (allSubroutines[i] is Scp106VigorAbilityBase scp106VigorAbilityBase)
			{
				this._vigorAbilities[this._vigorAbilitiesCount++] = scp106VigorAbilityBase;
			}
		}
	}

	private void ServerSetSubmerged(bool targetSubmerged, float animTime)
	{
		if (!NetworkServer.active || this.TargetSubmerged == targetSubmerged)
		{
			return;
		}
		ReferenceHub hub;
		bool flag = base.Role.TryGetOwner(out hub);
		if (flag)
		{
			Scp106ChangingSubmersionStatusEventArgs e = new Scp106ChangingSubmersionStatusEventArgs(hub, targetSubmerged);
			Scp106Events.OnChangingSubmersionStatus(e);
			if (!e.IsAllowed)
			{
				return;
			}
		}
		if (!targetSubmerged)
		{
			this._submergeCooldown.Trigger(5.0);
		}
		this.TargetSubmerged = targetSubmerged;
		this.TargetTransitionDuration = animTime;
		this._toggleTime = this.CurTime;
		Scp106SinkholeController.OnSubmergeStateChange?.Invoke(base.Role as Scp106Role, targetSubmerged);
		base.ServerSendRpc(toAll: true);
		if (flag)
		{
			Scp106Events.OnChangedSubmersionStatus(new Scp106ChangedSubmersionStatusEventArgs(hub, targetSubmerged));
		}
	}

	private void Update()
	{
		this._toggleAudioSource.volume = 8f * (1f - this.SubmergeProgress) - 0.07f;
		if (!NetworkServer.active)
		{
			return;
		}
		for (int i = 0; i < this._vigorAbilitiesCount; i++)
		{
			Scp106VigorAbilityBase scp106VigorAbilityBase = this._vigorAbilities[i];
			if (scp106VigorAbilityBase.ServerWantsSubmerged)
			{
				this._lastActiveVigorAbility = i;
				this.ServerSetSubmerged(targetSubmerged: true, scp106VigorAbilityBase.SubmergeTime);
				return;
			}
		}
		this.ServerSetSubmerged(targetSubmerged: false, this._vigorAbilities[this._lastActiveVigorAbility].EmergeTime);
	}
}

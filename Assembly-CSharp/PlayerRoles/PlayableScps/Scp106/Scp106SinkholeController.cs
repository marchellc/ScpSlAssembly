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
				return IsDuringAnimation;
			}
			return false;
		}
	}

	public float ElapsedToggle => CurTime - _toggleTime;

	public bool IsDuringAnimation => ElapsedToggle < TargetTransitionDuration;

	public IAbilityCooldown ReadonlyCooldown => _submergeCooldown;

	public bool IsHidden
	{
		get
		{
			if (TargetSubmerged)
			{
				return !IsDuringAnimation;
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
			float num = ElapsedToggle / TargetTransitionDuration;
			if (!TargetSubmerged)
			{
				num = 1f - num;
			}
			return Mathf.Clamp01(num);
		}
	}

	public static event SubmergeStateChanged OnSubmergeStateChange;

	public void ModifyCooldown(double modifyAmount)
	{
		_submergeCooldown.NextUse += modifyAmount;
		ServerSendRpc(toAll: true);
	}

	public void SpawnObject()
	{
		CursorManager.Register(this);
	}

	public void ResetObject()
	{
		CursorManager.Unregister(this);
		_submergeCooldown.Clear();
		TargetSubmerged = false;
		_toggleTime = 0f;
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBool(TargetSubmerged);
		writer.WriteFloat(TargetTransitionDuration);
		_submergeCooldown.WriteCooldown(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		bool targetSubmerged = TargetSubmerged;
		TargetSubmerged = reader.ReadBool();
		TargetTransitionDuration = reader.ReadFloat();
		_submergeCooldown.ReadCooldown(reader);
		if (targetSubmerged != TargetSubmerged)
		{
			_toggleTime = CurTime;
			_toggleAudioSource.PlayOneShot(TargetSubmerged ? _submergeSound : _emergeSound);
			Scp106SinkholeController.OnSubmergeStateChange?.Invoke(base.Role as Scp106Role, TargetSubmerged);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		SubroutineBase[] allSubroutines = (base.Role as Scp106Role).SubroutineModule.AllSubroutines;
		_vigorAbilities = new Scp106VigorAbilityBase[allSubroutines.Length];
		_vigorAbilitiesCount = 0;
		for (int i = 0; i < allSubroutines.Length; i++)
		{
			if (allSubroutines[i] is Scp106VigorAbilityBase scp106VigorAbilityBase)
			{
				_vigorAbilities[_vigorAbilitiesCount++] = scp106VigorAbilityBase;
			}
		}
	}

	private void ServerSetSubmerged(bool targetSubmerged, float animTime)
	{
		if (!NetworkServer.active || TargetSubmerged == targetSubmerged)
		{
			return;
		}
		ReferenceHub hub;
		bool flag = base.Role.TryGetOwner(out hub);
		if (flag)
		{
			Scp106ChangingSubmersionStatusEventArgs scp106ChangingSubmersionStatusEventArgs = new Scp106ChangingSubmersionStatusEventArgs(hub, targetSubmerged);
			Scp106Events.OnChangingSubmersionStatus(scp106ChangingSubmersionStatusEventArgs);
			if (!scp106ChangingSubmersionStatusEventArgs.IsAllowed)
			{
				return;
			}
		}
		if (!targetSubmerged)
		{
			_submergeCooldown.Trigger(5.0);
		}
		TargetSubmerged = targetSubmerged;
		TargetTransitionDuration = animTime;
		_toggleTime = CurTime;
		Scp106SinkholeController.OnSubmergeStateChange?.Invoke(base.Role as Scp106Role, targetSubmerged);
		ServerSendRpc(toAll: true);
		if (flag)
		{
			Scp106Events.OnChangedSubmersionStatus(new Scp106ChangedSubmersionStatusEventArgs(hub, targetSubmerged));
		}
	}

	private void Update()
	{
		_toggleAudioSource.volume = 8f * (1f - SubmergeProgress) - 0.07f;
		if (!NetworkServer.active)
		{
			return;
		}
		for (int i = 0; i < _vigorAbilitiesCount; i++)
		{
			Scp106VigorAbilityBase scp106VigorAbilityBase = _vigorAbilities[i];
			if (scp106VigorAbilityBase.ServerWantsSubmerged)
			{
				_lastActiveVigorAbility = i;
				ServerSetSubmerged(targetSubmerged: true, scp106VigorAbilityBase.SubmergeTime);
				return;
			}
		}
		ServerSetSubmerged(targetSubmerged: false, _vigorAbilities[_lastActiveVigorAbility].EmergeTime);
	}
}

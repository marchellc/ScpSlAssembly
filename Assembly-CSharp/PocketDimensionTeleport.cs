using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;
using UnityEngine;

public class PocketDimensionTeleport : NetworkBehaviour
{
	public delegate void PlayerEscapePocketDimension(ReferenceHub hub);

	public enum PDTeleportType
	{
		Killer,
		Exit
	}

	public static readonly HashSet<PocketDimensionTeleport> AllInstances = new HashSet<PocketDimensionTeleport>();

	public const float DisabledDuration = 10f;

	public static bool DebugBool;

	public static bool RefreshExit;

	private const float MinAliveDuration = 1f;

	private static bool _anyModified;

	private PDTeleportType _type;

	public static event Action<PocketDimensionTeleport> OnAdded;

	public static event Action<PocketDimensionTeleport> OnRemoved;

	public static event Action OnInstancesUpdated;

	public static event PlayerEscapePocketDimension OnPlayerEscapePocketDimension;

	public void SetType(PDTeleportType t)
	{
		this._type = t;
	}

	public PDTeleportType GetTeleportType()
	{
		return this._type;
	}

	public static void Exit(PocketDimensionTeleport instance, ReferenceHub hub)
	{
		if (hub.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			PlayerLeavingPocketDimensionEventArgs e = new PlayerLeavingPocketDimensionEventArgs(hub, instance, isSuccessful: true);
			PlayerEvents.OnLeavingPocketDimension(e);
			if (e.IsAllowed)
			{
				fpcRole.FpcModule.ServerOverridePosition(Scp106PocketExitFinder.GetBestExitPosition(fpcRole));
				hub.playerEffectsController.EnableEffect<Disabled>(10f, addDuration: true);
				hub.playerEffectsController.EnableEffect<Traumatized>();
				hub.playerEffectsController.DisableEffect<PocketCorroding>();
				hub.playerEffectsController.DisableEffect<Corroding>();
				PocketDimensionTeleport.OnPlayerEscapePocketDimension?.Invoke(hub);
				PocketDimensionGenerator.RandomizeTeleports();
				PlayerEvents.OnLeftPocketDimension(new PlayerLeftPocketDimensionEventArgs(hub, instance, isSuccessful: true));
			}
		}
	}

	public static void Kill(PocketDimensionTeleport instance, ReferenceHub hub)
	{
		PlayerLeavingPocketDimensionEventArgs e = new PlayerLeavingPocketDimensionEventArgs(hub, instance, isSuccessful: false);
		PlayerEvents.OnLeavingPocketDimension(e);
		if (e.IsAllowed)
		{
			hub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.PocketDecay));
			PlayerEvents.OnLeftPocketDimension(new PlayerLeftPocketDimensionEventArgs(hub, instance, isSuccessful: false));
		}
	}

	private void Awake()
	{
		PocketDimensionTeleport._anyModified = true;
		PocketDimensionTeleport.AllInstances.Add(this);
	}

	private void Start()
	{
		PocketDimensionTeleport.OnAdded?.Invoke(this);
	}

	private void OnDestroy()
	{
		PocketDimensionTeleport._anyModified = true;
		PocketDimensionTeleport.AllInstances.Remove(this);
		PocketDimensionTeleport.OnRemoved?.Invoke(this);
	}

	private void Update()
	{
		if (PocketDimensionTeleport._anyModified)
		{
			PocketDimensionTeleport.OnInstancesUpdated?.Invoke();
			PocketDimensionTeleport._anyModified = false;
		}
	}

	[ServerCallback]
	private void OnTriggerEnter(Collider other)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		NetworkIdentity component = other.GetComponent<NetworkIdentity>();
		if (!(component == null) && ReferenceHub.TryGetHubNetID(component.netId, out var hub) && !(hub.roleManager.CurrentRole.ActiveTime < 1f))
		{
			if ((this._type == PDTeleportType.Killer || AlphaWarheadController.Detonated) && !PocketDimensionTeleport.DebugBool)
			{
				PocketDimensionTeleport.Kill(this, hub);
			}
			else if (this._type == PDTeleportType.Exit || PocketDimensionTeleport.DebugBool)
			{
				PocketDimensionTeleport.Exit(this, hub);
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}

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
	public static event Action<PocketDimensionTeleport> OnAdded;

	public static event Action<PocketDimensionTeleport> OnRemoved;

	public static event Action OnInstancesUpdated;

	public static event PocketDimensionTeleport.PlayerEscapePocketDimension OnPlayerEscapePocketDimension;

	public void SetType(PocketDimensionTeleport.PDTeleportType t)
	{
		this._type = t;
	}

	public PocketDimensionTeleport.PDTeleportType GetTeleportType()
	{
		return this._type;
	}

	public static void Exit(PocketDimensionTeleport instance, ReferenceHub hub)
	{
		IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
		if (fpcRole == null)
		{
			return;
		}
		PlayerLeavingPocketDimensionEventArgs playerLeavingPocketDimensionEventArgs = new PlayerLeavingPocketDimensionEventArgs(hub, instance, true);
		PlayerEvents.OnLeavingPocketDimension(playerLeavingPocketDimensionEventArgs);
		if (!playerLeavingPocketDimensionEventArgs.IsAllowed)
		{
			return;
		}
		fpcRole.FpcModule.ServerOverridePosition(Scp106PocketExitFinder.GetBestExitPosition(fpcRole));
		hub.playerEffectsController.EnableEffect<Disabled>(10f, true);
		hub.playerEffectsController.EnableEffect<Traumatized>(0f, false);
		hub.playerEffectsController.DisableEffect<PocketCorroding>();
		hub.playerEffectsController.DisableEffect<Corroding>();
		PocketDimensionTeleport.PlayerEscapePocketDimension onPlayerEscapePocketDimension = PocketDimensionTeleport.OnPlayerEscapePocketDimension;
		if (onPlayerEscapePocketDimension != null)
		{
			onPlayerEscapePocketDimension(hub);
		}
		PocketDimensionGenerator.RandomizeTeleports();
		PlayerEvents.OnLeftPocketDimension(new PlayerLeftPocketDimensionEventArgs(hub, instance, true));
	}

	public static void Kill(PocketDimensionTeleport instance, ReferenceHub hub)
	{
		PlayerLeavingPocketDimensionEventArgs playerLeavingPocketDimensionEventArgs = new PlayerLeavingPocketDimensionEventArgs(hub, instance, false);
		PlayerEvents.OnLeavingPocketDimension(playerLeavingPocketDimensionEventArgs);
		if (!playerLeavingPocketDimensionEventArgs.IsAllowed)
		{
			return;
		}
		hub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.PocketDecay, null));
		PlayerEvents.OnLeftPocketDimension(new PlayerLeftPocketDimensionEventArgs(hub, instance, false));
	}

	private void Awake()
	{
		PocketDimensionTeleport._anyModified = true;
		PocketDimensionTeleport.AllInstances.Add(this);
	}

	private void Start()
	{
		Action<PocketDimensionTeleport> onAdded = PocketDimensionTeleport.OnAdded;
		if (onAdded == null)
		{
			return;
		}
		onAdded(this);
	}

	private void OnDestroy()
	{
		PocketDimensionTeleport._anyModified = true;
		PocketDimensionTeleport.AllInstances.Remove(this);
		Action<PocketDimensionTeleport> onRemoved = PocketDimensionTeleport.OnRemoved;
		if (onRemoved == null)
		{
			return;
		}
		onRemoved(this);
	}

	private void Update()
	{
		if (!PocketDimensionTeleport._anyModified)
		{
			return;
		}
		Action onInstancesUpdated = PocketDimensionTeleport.OnInstancesUpdated;
		if (onInstancesUpdated != null)
		{
			onInstancesUpdated();
		}
		PocketDimensionTeleport._anyModified = false;
	}

	[ServerCallback]
	private void OnTriggerEnter(Collider other)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		NetworkIdentity component = other.GetComponent<NetworkIdentity>();
		if (component == null)
		{
			return;
		}
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHubNetID(component.netId, out referenceHub))
		{
			return;
		}
		if (referenceHub.roleManager.CurrentRole.ActiveTime < 1f)
		{
			return;
		}
		if ((this._type == PocketDimensionTeleport.PDTeleportType.Killer || AlphaWarheadController.Detonated) && !PocketDimensionTeleport.DebugBool)
		{
			PocketDimensionTeleport.Kill(this, referenceHub);
			return;
		}
		if (this._type == PocketDimensionTeleport.PDTeleportType.Exit || PocketDimensionTeleport.DebugBool)
		{
			PocketDimensionTeleport.Exit(this, referenceHub);
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	public static readonly HashSet<PocketDimensionTeleport> AllInstances = new HashSet<PocketDimensionTeleport>();

	public const float DisabledDuration = 10f;

	public static bool DebugBool;

	public static bool RefreshExit;

	private const float MinAliveDuration = 1f;

	private static bool _anyModified;

	private PocketDimensionTeleport.PDTeleportType _type;

	public delegate void PlayerEscapePocketDimension(ReferenceHub hub);

	public enum PDTeleportType
	{
		Killer,
		Exit
	}
}

using System.Collections.Generic;
using System.Diagnostics;
using AudioPooling;
using InventorySystem.Items.Pickups;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Usables.Scp1576;

public class Scp1576Item : UsableItem
{
	public const float TransmissionDuration = 30f;

	public const float UseCooldown = 120f;

	public const float HornReturnSpeed = 0.4f;

	public const float HornReturnDelay = 1.1f;

	public const float SqrAudibleRange = 110f;

	public const float WarningDuration = 2f;

	public static HashSet<ReferenceHub> ValidatedTransmitters = new HashSet<ReferenceHub>();

	public static HashSet<ReferenceHub> ValidatedReceivers = new HashSet<ReferenceHub>();

	private static readonly List<Vector3> ActiveNonAllocPositions = new List<Vector3>(8);

	private static bool _locallyUsed;

	private static bool _eventAssigned;

	[SerializeField]
	private AudioClip _warningStart;

	[SerializeField]
	private AudioClip _warningStop;

	private float _serverHornPos;

	private bool _startWarningTriggered;

	private readonly Stopwatch _useStopwatch = new Stopwatch();

	public static bool LocallyUsed
	{
		get
		{
			return Scp1576Item._locallyUsed;
		}
		internal set
		{
			Scp1576Item._locallyUsed = value;
			if (value != Scp1576Item._eventAssigned)
			{
				if (value)
				{
					StaticUnityMethods.OnUpdate += ContinueCheckingLocalUse;
					Scp1576Item._eventAssigned = true;
				}
				else
				{
					StaticUnityMethods.OnUpdate -= ContinueCheckingLocalUse;
					Scp1576Item._eventAssigned = false;
				}
			}
		}
	}

	public override bool AllowHolster => true;

	[field: SerializeField]
	public Scp1576Playback PlaybackTemplate { get; private set; }

	public override void ServerOnUsingCompleted()
	{
		Scp1576Item.ValidatedTransmitters.Add(base.Owner);
	}

	public override void OnUsingStarted()
	{
		base.OnUsingStarted();
		this._useStopwatch.Restart();
		this._startWarningTriggered = true;
	}

	public override void OnUsingCancelled()
	{
		base.OnUsingCancelled();
		this._useStopwatch.Reset();
	}

	public override bool ServerValidateStartRequest(PlayerHandler handler)
	{
		if (!this._useStopwatch.IsRunning)
		{
			return base.ServerValidateStartRequest(handler);
		}
		return false;
	}

	public override bool ServerValidateCancelRequest(PlayerHandler handler)
	{
		if (handler.CurrentUsable.ItemSerial == base.ItemSerial || !this._useStopwatch.IsRunning)
		{
			return base.ServerValidateCancelRequest(handler);
		}
		this.ServerStopTransmitting();
		return false;
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		if (pickup is Scp1576Pickup scp1576Pickup && !(scp1576Pickup == null))
		{
			this._serverHornPos = scp1576Pickup.HornPos;
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (pickup is Scp1576Pickup scp1576Pickup && !(scp1576Pickup == null))
		{
			scp1576Pickup.HornPos = this._serverHornPos;
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!NetworkServer.active)
		{
			return;
		}
		float num = 30f + base.UseTime;
		double totalSeconds = this._useStopwatch.Elapsed.TotalSeconds;
		if (totalSeconds < 1.100000023841858)
		{
			return;
		}
		if (totalSeconds < (double)base.UseTime)
		{
			if (this._startWarningTriggered && (double)base.UseTime - totalSeconds < 2.0)
			{
				this._startWarningTriggered = false;
				Scp1576SpectatorWarningHandler.TriggerStart(this);
			}
			this._serverHornPos = Mathf.Max(this._serverHornPos - Time.deltaTime * 0.4f, 0f);
		}
		else if (totalSeconds < (double)num)
		{
			this._serverHornPos = Mathf.Clamp01((float)(totalSeconds - (double)base.UseTime) / num);
		}
		else
		{
			this._serverHornPos = 1f;
			this.ServerStopTransmitting();
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		if (this._useStopwatch.IsRunning)
		{
			this.ServerStopTransmitting();
		}
	}

	internal override void OnTemplateReloaded(bool wasEverLoaded)
	{
		base.OnTemplateReloaded(wasEverLoaded);
		if (!wasEverLoaded)
		{
			Scp1576SpectatorWarningHandler.OnStop += delegate
			{
				Scp1576Item.PlayWarningSound(this._warningStop);
			};
			Scp1576SpectatorWarningHandler.OnStart += delegate
			{
				Scp1576Item.PlayWarningSound(this._warningStart);
			};
		}
	}

	private void ServerStopTransmitting()
	{
		this._useStopwatch.Reset();
		Scp1576Item.ValidatedTransmitters.Remove(base.Owner);
		base.ServerSetGlobalItemCooldown(120f);
		Scp1576SpectatorWarningHandler.TriggerStop(this);
		new StatusMessage(StatusMessage.StatusType.Cancel, base.ItemSerial).SendToAuthenticated();
		base.Owner.connectionToClient.Send(new ItemCooldownMessage(base.ItemSerial, 120f));
	}

	private static void PlayWarningSound(AudioClip sound)
	{
		if (SpectatorTargetTracker.TrackerSet)
		{
			Transform parent = SpectatorTargetTracker.Singleton.transform;
			AudioSourcePoolManager.Play2DWithParent(sound, parent, 1f, MixerChannel.VoiceChat);
		}
	}

	private static void ContinueCheckingLocalUse()
	{
		if (!ReferenceHub.TryGetLocalHub(out var hub) || !(hub.inventory.CurInstance is Scp1576Item scp1576Item) || !(scp1576Item != null))
		{
			Scp1576Item.LocallyUsed = false;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnUpdate += RevalidateReceivers;
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			if (NetworkServer.active)
			{
				Scp1576Item.ValidatedReceivers.Remove(hub);
				Scp1576Item.ValidatedTransmitters.Remove(hub);
			}
		};
		Inventory.OnServerStarted += Scp1576Item.ValidatedTransmitters.Clear;
	}

	private static void RevalidateReceivers()
	{
		if (!StaticUnityMethods.IsPlaying || !NetworkServer.active)
		{
			return;
		}
		Scp1576Item.ValidatedReceivers.Clear();
		Scp1576Item.ActiveNonAllocPositions.Clear();
		foreach (ReferenceHub validatedTransmitter in Scp1576Item.ValidatedTransmitters)
		{
			if (validatedTransmitter.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				Scp1576Item.ActiveNonAllocPositions.Add(fpcRole.FpcModule.Position);
			}
		}
		int count = Scp1576Item.ActiveNonAllocPositions.Count;
		if (count == 0)
		{
			return;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!(allHub.roleManager.CurrentRole is IFpcRole fpcRole2))
			{
				if (allHub.IsAlive())
				{
					Scp1576Item.ValidatedReceivers.Add(allHub);
				}
				continue;
			}
			Vector3 position = fpcRole2.FpcModule.Position;
			for (int i = 0; i < count; i++)
			{
				if (!((position - Scp1576Item.ActiveNonAllocPositions[i]).sqrMagnitude > 110f))
				{
					Scp1576Item.ValidatedReceivers.Add(allHub);
					break;
				}
			}
		}
	}
}

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
			return _locallyUsed;
		}
		internal set
		{
			_locallyUsed = value;
			if (value != _eventAssigned)
			{
				if (value)
				{
					StaticUnityMethods.OnUpdate += ContinueCheckingLocalUse;
					_eventAssigned = true;
				}
				else
				{
					StaticUnityMethods.OnUpdate -= ContinueCheckingLocalUse;
					_eventAssigned = false;
				}
			}
		}
	}

	public override bool AllowHolster => true;

	[field: SerializeField]
	public Scp1576Playback PlaybackTemplate { get; private set; }

	public override void ServerOnUsingCompleted()
	{
		ValidatedTransmitters.Add(base.Owner);
	}

	public override void OnUsingStarted()
	{
		base.OnUsingStarted();
		_useStopwatch.Restart();
		_startWarningTriggered = true;
	}

	public override void OnUsingCancelled()
	{
		base.OnUsingCancelled();
		_useStopwatch.Reset();
	}

	public override bool ServerValidateStartRequest(PlayerHandler handler)
	{
		if (!_useStopwatch.IsRunning)
		{
			return base.ServerValidateStartRequest(handler);
		}
		return false;
	}

	public override bool ServerValidateCancelRequest(PlayerHandler handler)
	{
		if (handler.CurrentUsable.ItemSerial == base.ItemSerial || !_useStopwatch.IsRunning)
		{
			return base.ServerValidateCancelRequest(handler);
		}
		ServerStopTransmitting();
		return false;
	}

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		if (pickup is Scp1576Pickup scp1576Pickup && !(scp1576Pickup == null))
		{
			_serverHornPos = scp1576Pickup.HornPos;
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (pickup is Scp1576Pickup scp1576Pickup && !(scp1576Pickup == null))
		{
			scp1576Pickup.HornPos = _serverHornPos;
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!NetworkServer.active)
		{
			return;
		}
		float num = 30f + UseTime;
		double totalSeconds = _useStopwatch.Elapsed.TotalSeconds;
		if (totalSeconds < 1.100000023841858)
		{
			return;
		}
		if (totalSeconds < (double)UseTime)
		{
			if (_startWarningTriggered && (double)UseTime - totalSeconds < 2.0)
			{
				_startWarningTriggered = false;
				Scp1576SpectatorWarningHandler.TriggerStart(this);
			}
			_serverHornPos = Mathf.Max(_serverHornPos - Time.deltaTime * 0.4f, 0f);
		}
		else if (totalSeconds < (double)num)
		{
			_serverHornPos = Mathf.Clamp01((float)(totalSeconds - (double)UseTime) / num);
		}
		else
		{
			_serverHornPos = 1f;
			ServerStopTransmitting();
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		if (_useStopwatch.IsRunning)
		{
			ServerStopTransmitting();
		}
	}

	internal override void OnTemplateReloaded(bool wasEverLoaded)
	{
		base.OnTemplateReloaded(wasEverLoaded);
		if (!wasEverLoaded)
		{
			Scp1576SpectatorWarningHandler.OnStop += delegate
			{
				PlayWarningSound(_warningStop);
			};
			Scp1576SpectatorWarningHandler.OnStart += delegate
			{
				PlayWarningSound(_warningStart);
			};
		}
	}

	private void ServerStopTransmitting()
	{
		_useStopwatch.Reset();
		ValidatedTransmitters.Remove(base.Owner);
		ServerSetGlobalItemCooldown(120f);
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
			LocallyUsed = false;
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
				ValidatedReceivers.Remove(hub);
				ValidatedTransmitters.Remove(hub);
			}
		};
		Inventory.OnServerStarted += ValidatedTransmitters.Clear;
	}

	private static void RevalidateReceivers()
	{
		if (!StaticUnityMethods.IsPlaying || !NetworkServer.active)
		{
			return;
		}
		ValidatedReceivers.Clear();
		ActiveNonAllocPositions.Clear();
		foreach (ReferenceHub validatedTransmitter in ValidatedTransmitters)
		{
			if (validatedTransmitter.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				ActiveNonAllocPositions.Add(fpcRole.FpcModule.Position);
			}
		}
		int count = ActiveNonAllocPositions.Count;
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
					ValidatedReceivers.Add(allHub);
				}
				continue;
			}
			Vector3 position = fpcRole2.FpcModule.Position;
			for (int i = 0; i < count; i++)
			{
				if (!((position - ActiveNonAllocPositions[i]).sqrMagnitude > 110f))
				{
					ValidatedReceivers.Add(allHub);
					break;
				}
			}
		}
	}
}

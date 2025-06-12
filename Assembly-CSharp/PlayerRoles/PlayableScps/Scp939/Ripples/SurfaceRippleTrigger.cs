using System.Collections.Generic;
using CustomPlayerEffects;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Ripples;

public class SurfaceRippleTrigger : RippleTriggerBase
{
	private struct LastRippleInformation
	{
		public bool IsNatural;

		private double _time;

		public static LastRippleInformation Default => new LastRippleInformation
		{
			IsNatural = true,
			_time = NetworkTime.time
		};

		public static LastRippleInformation SurfaceDefault => new LastRippleInformation
		{
			IsNatural = false,
			_time = NetworkTime.time
		};

		public float Elapsed => (float)(NetworkTime.time - this._time);
	}

	private const float TimeBetweenSurfaceRipples = 10f;

	private const float NaturalRippleCooldown = 20f;

	private readonly Dictionary<uint, LastRippleInformation> _lastRipples = new Dictionary<uint, LastRippleInformation>();

	private ReferenceHub _syncPlayer;

	private RelativePosition _syncPos;

	public override void SpawnObject()
	{
		base.SpawnObject();
		this._lastRipples.Clear();
		if (base.Role.IsLocalPlayer)
		{
			RippleTriggerBase.OnPlayedRippleLocally += OnPlayerPlayedRipple;
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._lastRipples.Clear();
		if (!base.Role.IsLocalPlayer)
		{
			RippleTriggerBase.OnPlayedRippleLocally -= OnPlayerPlayedRipple;
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteReferenceHub(this._syncPlayer);
		writer.WriteRelativePosition(this._syncPos);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (reader.TryReadReferenceHub(out this._syncPlayer) && HitboxIdentity.IsEnemy(base.Owner, this._syncPlayer) && this._syncPlayer.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase && !(fpcStandardRoleBase.FpcModule.CharacterModelInstance.Fade > 0f))
		{
			this._syncPos = reader.ReadRelativePosition();
			base.Player.Play(this._syncPos.Position, fpcStandardRoleBase.RoleColor);
		}
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteReferenceHub(this._syncPlayer);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (reader.TryReadReferenceHub(out this._syncPlayer))
		{
			this.ProcessRipple(this._syncPlayer);
		}
	}

	public void ProcessRipple(ReferenceHub hub)
	{
		if (this._lastRipples.ContainsKey(hub.netId))
		{
			this._lastRipples[hub.netId] = LastRippleInformation.Default;
		}
		else
		{
			this._lastRipples.Add(hub.netId, LastRippleInformation.Default);
		}
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!HitboxIdentity.IsEnemy(base.Owner, allHub) || !(allHub.roleManager.CurrentRole is IFpcRole fpcRole) || (allHub.playerEffectsController.TryGetEffect<Invisible>(out var playerEffect) && playerEffect.IsEnabled))
			{
				continue;
			}
			Vector3 position = fpcRole.FpcModule.Position;
			if (position.GetZone() != FacilityZone.Surface)
			{
				continue;
			}
			if (!this._lastRipples.TryGetValue(allHub.netId, out var value))
			{
				this._lastRipples.Add(allHub.netId, LastRippleInformation.SurfaceDefault);
				continue;
			}
			if (value.IsNatural)
			{
				if (value.Elapsed < 20f)
				{
					goto IL_00d6;
				}
			}
			else if (value.Elapsed < 10f)
			{
				goto IL_00d6;
			}
			bool flag = false;
			goto IL_00de;
			IL_00de:
			if (!flag)
			{
				this._lastRipples[allHub.netId] = LastRippleInformation.SurfaceDefault;
				this._syncPos = new RelativePosition(position);
				this._syncPlayer = allHub;
				base.ServerSendRpcToObservers();
			}
			continue;
			IL_00d6:
			flag = true;
			goto IL_00de;
		}
	}

	private void OnPlayerPlayedRipple(ReferenceHub player)
	{
		this._syncPlayer = player;
		base.ClientSendCmd();
	}
}

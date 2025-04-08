using System;
using CustomPlayerEffects;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Hazards
{
	public class SinkholeEnvironmentalHazard : EnvironmentalHazard
	{
		public override bool OnEnter(ReferenceHub player)
		{
			if (!this.IsActive || player.IsSCP(true))
			{
				return false;
			}
			if (!base.OnEnter(player))
			{
				return false;
			}
			player.playerEffectsController.EnableEffect<Sinkhole>(1f, false);
			PlayerEvents.OnEnteredHazard(new PlayerEnteredHazardEventArgs(player, this));
			return true;
		}

		public override void OnStay(ReferenceHub player)
		{
			player.playerEffectsController.EnableEffect<Sinkhole>(1f, false);
		}

		public override bool OnExit(ReferenceHub player)
		{
			if (!base.OnExit(player))
			{
				return false;
			}
			player.playerEffectsController.EnableEffect<Sinkhole>(1f, false);
			PlayerEvents.OnLeftHazard(new PlayerLeftHazardEventArgs(player, this));
			return true;
		}

		protected override void Awake()
		{
			base.Awake();
			if (!NetworkServer.active)
			{
				return;
			}
			this.IsActive = false;
		}

		protected override void Start()
		{
			if (!NetworkServer.active || this.IsActive || ConfigFile.ServerConfig.GetFloat("sinkhole_spawn_chance", 0f) >= (float)global::UnityEngine.Random.Range(1, 100))
			{
				base.Start();
				this.ClientApplyDecalSize();
				return;
			}
			if (base.netId == 0U)
			{
				global::UnityEngine.Object.Destroy(base.gameObject);
				return;
			}
			NetworkServer.Destroy(base.gameObject);
		}

		protected override void ClientApplyDecalSize()
		{
		}

		public override bool Weaved()
		{
			return true;
		}
	}
}

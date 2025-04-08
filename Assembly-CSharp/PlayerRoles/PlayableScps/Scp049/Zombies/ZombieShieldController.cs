using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HumeShield;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieShieldController : DynamicHumeShieldController
	{
		public override float HsMax
		{
			get
			{
				return 100f;
			}
		}

		public override float HsRegeneration
		{
			get
			{
				if (!ZombieShieldController.CallSubroutines.Any((Scp049CallAbility x) => x.IsMarkerShown && this.CheckDistanceTo(x.CastRole)))
				{
					return 0f;
				}
				return base.HsRegeneration;
			}
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._fpc = (base.Owner.roleManager.CurrentRole as IFpcRole).FpcModule;
		}

		private bool CheckDistanceTo(Scp049Role role)
		{
			return (role.FpcModule.Position - this._fpc.Position).sqrMagnitude <= 100f;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += ZombieShieldController.CallSubroutines.Clear;
			PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				Scp049CallAbility scp049CallAbility;
				if (ZombieShieldController.TryGetCallSubroutine(oldRole, out scp049CallAbility))
				{
					ZombieShieldController.CallSubroutines.Remove(scp049CallAbility);
				}
				Scp049CallAbility scp049CallAbility2;
				if (ZombieShieldController.TryGetCallSubroutine(newRole, out scp049CallAbility2))
				{
					ZombieShieldController.CallSubroutines.Add(scp049CallAbility2);
				}
			};
		}

		private static bool TryGetCallSubroutine(PlayerRoleBase prb, out Scp049CallAbility sr)
		{
			Scp049Role scp049Role = prb as Scp049Role;
			if (scp049Role != null)
			{
				return scp049Role.SubroutineModule.TryGetSubroutine<Scp049CallAbility>(out sr);
			}
			sr = null;
			return false;
		}

		public const float MaxShield = 100f;

		public const float MaxActivateDistanceSqr = 100f;

		private static readonly HashSet<Scp049CallAbility> CallSubroutines = new HashSet<Scp049CallAbility>();

		private FirstPersonMovementModule _fpc;
	}
}

using System;
using PlayerStatsSystem;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class SqueakInteraction : PopupInterobject, IDestructible
	{
		public uint NetworkId
		{
			get
			{
				return this._spawner.netId;
			}
		}

		public Vector3 CenterOfMass
		{
			get
			{
				return Vector3.zero;
			}
		}

		public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHitPos)
		{
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null || attackerDamageHandler.Attacker.Hub == null)
			{
				return false;
			}
			this._spawner.TargetHitMouse(attackerDamageHandler.Attacker.Hub.networkIdentity.connectionToClient);
			return true;
		}

		protected override void OnClientStateChange()
		{
		}

		protected override void OnClientUpdate(float enableRatio)
		{
		}

		private void Awake()
		{
			this._spawner = base.GetComponentInParent<SqueakSpawner>();
		}

		private SqueakSpawner _spawner;
	}
}

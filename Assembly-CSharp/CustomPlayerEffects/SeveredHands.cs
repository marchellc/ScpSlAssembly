using System;
using InventorySystem;
using InventorySystem.Items;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class SeveredHands : TickingEffectBase, IInteractionBlocker
	{
		public override bool AllowEnabling
		{
			get
			{
				return true;
			}
		}

		public bool CanBeCleared
		{
			get
			{
				return !base.IsEnabled;
			}
		}

		public BlockedInteraction BlockedInteractions
		{
			get
			{
				return BlockedInteraction.All;
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			base.Hub.interCoordinator.AddBlocker(this);
			this.ChangeHandsState(true);
		}

		protected override void Disabled()
		{
			base.Disabled();
			this.ChangeHandsState(false);
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.Hub.inventory.ServerDropItem(base.Hub.inventory.CurItem.SerialNumber);
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this._tickDamage, DeathTranslations.SeveredHands, null));
		}

		private void ChangeHandsState(bool handsCut)
		{
			HumanRole humanRole = base.Hub.roleManager.CurrentRole as HumanRole;
			if (humanRole == null)
			{
				return;
			}
			AnimatedCharacterModel animatedCharacterModel = humanRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
			if (animatedCharacterModel == null)
			{
				return;
			}
			if (animatedCharacterModel.HasParameter(SeveredHands.HashSeveredHands))
			{
				animatedCharacterModel.Animator.SetBool(SeveredHands.HashSeveredHands, handsCut);
			}
		}

		private const BlockedInteraction Interactions = BlockedInteraction.All;

		private static readonly int HashSeveredHands = Animator.StringToHash("SeveredHands");

		[SerializeField]
		private float _tickDamage;
	}
}

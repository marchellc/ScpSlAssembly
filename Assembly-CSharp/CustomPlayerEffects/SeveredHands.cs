using InventorySystem;
using InventorySystem.Items;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class SeveredHands : TickingEffectBase, IInteractionBlocker
{
	private const BlockedInteraction Interactions = BlockedInteraction.All;

	private static readonly int HashSeveredHands = Animator.StringToHash("SeveredHands");

	[SerializeField]
	private float _tickDamage;

	public override bool AllowEnabling => true;

	public bool CanBeCleared => !base.IsEnabled;

	public BlockedInteraction BlockedInteractions => BlockedInteraction.All;

	protected override void Enabled()
	{
		base.Enabled();
		base.Hub.interCoordinator.AddBlocker(this);
		ChangeHandsState(handsCut: true);
	}

	protected override void Disabled()
	{
		base.Disabled();
		ChangeHandsState(handsCut: false);
	}

	protected override void OnTick()
	{
		if (NetworkServer.active)
		{
			base.Hub.inventory.ServerDropItem(base.Hub.inventory.CurItem.SerialNumber);
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(_tickDamage, DeathTranslations.SeveredHands));
		}
	}

	private void ChangeHandsState(bool handsCut)
	{
		if (base.Hub.roleManager.CurrentRole is HumanRole humanRole && humanRole.FpcModule.CharacterModelInstance is AnimatedCharacterModel animatedCharacterModel && animatedCharacterModel.HasParameter(HashSeveredHands))
		{
			animatedCharacterModel.Animator.SetBool(HashSeveredHands, handsCut);
		}
	}
}

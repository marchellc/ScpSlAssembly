using MapGeneration.StaticHelpers;
using PlayerStatsSystem;
using UnityEngine;

namespace Interactables.Interobjects;

public class SqueakInteraction : PopupInterobject, IDestructible, IBlockStaticBatching
{
	private SqueakSpawner _spawner;

	public uint NetworkId => this._spawner.netId;

	public Vector3 CenterOfMass => Vector3.zero;

	public bool Damage(float damage, DamageHandlerBase handler, Vector3 exactHitPos)
	{
		if (!(handler is AttackerDamageHandler attackerDamageHandler) || attackerDamageHandler.Attacker.Hub == null)
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
}

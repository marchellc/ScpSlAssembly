using System;
using CustomPlayerEffects;
using InventorySystem.Disarming;
using InventorySystem.Items;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace Interactables.Verification
{
	public class StandardDistanceVerification : IVerificationRule
	{
		public StandardDistanceVerification(float maxDistance = 2.42f, bool allowHandcuffedInteraction = false, bool cancelScp268 = true)
		{
			this._maxDistance = maxDistance;
			this._allowHandcuffed = allowHandcuffedInteraction;
			this._cancel268 = cancelScp268;
		}

		public bool ClientCanInteract(InteractableCollider collider, RaycastHit hit)
		{
			return hit.distance < this._maxDistance;
		}

		public bool ServerCanInteract(ReferenceHub hub, InteractableCollider collider)
		{
			if (!this._allowHandcuffed && !PlayerInteract.CanDisarmedInteract && hub.inventory.IsDisarmed())
			{
				return false;
			}
			if (hub.interCoordinator.AnyBlocker(BlockedInteraction.GeneralInteractions))
			{
				return false;
			}
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			Transform transform = collider.transform;
			if (Vector3.Distance(fpcRole.FpcModule.Position, transform.position + transform.TransformDirection(collider.VerificationOffset)) > this._maxDistance * 1.4f)
			{
				return false;
			}
			if (this._cancel268)
			{
				hub.playerEffectsController.DisableEffect<Invisible>();
			}
			return true;
		}

		private const float InteractLagCompensation = 1.4f;

		public const float DefaultMaxDistance = 2.42f;

		public static StandardDistanceVerification Default = new StandardDistanceVerification(2.42f, false, true);

		private readonly float _maxDistance;

		private readonly bool _allowHandcuffed;

		private readonly bool _cancel268;
	}
}

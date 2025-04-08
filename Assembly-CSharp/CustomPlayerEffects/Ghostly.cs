using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp106;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Ghostly : StatusEffectBase, IFpcCollisionModifier
	{
		public LayerMask DetectionMask
		{
			get
			{
				return Scp106MovementModule.PassableDetectionMask;
			}
		}

		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public void ProcessColliders(ArraySegment<Collider> detections)
		{
			foreach (Collider collider in detections)
			{
				collider.enabled = Scp106MovementModule.GetSlowdownFromCollider(collider) == 0f;
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			FpcCollisionProcessor.AddModifier(this, fpcRole);
		}

		protected override void Disabled()
		{
			base.Disabled();
			FpcCollisionProcessor.RemoveModifier(this);
		}
	}
}

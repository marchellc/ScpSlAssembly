using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	public static class FpcCollisionProcessor
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			FirstPersonMovementModule.OnPositionUpdated += FpcCollisionProcessor.RestoreColliders;
			PlayerRoleManager.OnRoleChanged += FpcCollisionProcessor.OnRoleChanged;
		}

		private static void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			IFpcRole fpcRole = prevRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			FpcCollisionProcessor.RemoveAllModifiers(fpcRole);
		}

		private static void RestoreColliders()
		{
			foreach (Collider collider in FpcCollisionProcessor.AffectedColliders)
			{
				if (!(collider == null))
				{
					collider.enabled = true;
				}
			}
			FpcCollisionProcessor.AffectedColliders.Clear();
		}

		private static void ProcessPair(FpcCollisionProcessor.RoleModifierPair pair, Vector3 moveDir)
		{
			CharacterController charController = pair.Role.FpcModule.CharController;
			Vector3 position = pair.Role.FpcModule.Position;
			Vector3 vector = Vector3.up * charController.height / 2f;
			LayerMask detectionMask = pair.Modifier.DetectionMask;
			float num = charController.radius + charController.skinWidth;
			int num2 = Physics.OverlapCapsuleNonAlloc(position + vector, position - vector, num, FpcCollisionProcessor.CollidersNonAlloc, detectionMask);
			int num3 = num2;
			float magnitude = moveDir.magnitude;
			if (magnitude > 0f)
			{
				moveDir /= magnitude;
				int num4 = Physics.CapsuleCastNonAlloc(position + vector, position - vector, num, moveDir, FpcCollisionProcessor.HitsNonAlloc, magnitude, detectionMask);
				for (int i = 0; i < num4; i++)
				{
					FpcCollisionProcessor.CollidersNonAlloc[num2 + i] = FpcCollisionProcessor.HitsNonAlloc[i].collider;
				}
				num3 += num4;
			}
			pair.Modifier.ProcessColliders(new ArraySegment<Collider>(FpcCollisionProcessor.CollidersNonAlloc, 0, num3));
			for (int j = 0; j < num3; j++)
			{
				Collider collider = FpcCollisionProcessor.CollidersNonAlloc[j];
				if (!collider.enabled)
				{
					FpcCollisionProcessor.AffectedColliders.Add(collider);
				}
			}
		}

		public static void RemoveAllModifiers(IFpcRole role)
		{
			for (int i = 0; i < FpcCollisionProcessor.ActiveModifiers.Count; i++)
			{
				FpcCollisionProcessor.RoleModifierPair roleModifierPair = FpcCollisionProcessor.ActiveModifiers[i];
				if (!(roleModifierPair.Role.FpcModule != role.FpcModule))
				{
					roleModifierPair.Unlink();
					FpcCollisionProcessor.ActiveModifiers.RemoveAt(i--);
				}
			}
		}

		public static void RemoveModifier(IFpcCollisionModifier modifier)
		{
			for (int i = 0; i < FpcCollisionProcessor.ActiveModifiers.Count; i++)
			{
				FpcCollisionProcessor.RoleModifierPair roleModifierPair = FpcCollisionProcessor.ActiveModifiers[i];
				if (FpcCollisionProcessor.ActiveModifiers[i].Modifier == modifier)
				{
					roleModifierPair.Unlink();
					FpcCollisionProcessor.ActiveModifiers.RemoveAt(i--);
				}
			}
		}

		public static void AddModifier(IFpcCollisionModifier modifier, IFpcRole fpcRole)
		{
			FpcCollisionProcessor.ActiveModifiers.Add(new FpcCollisionProcessor.RoleModifierPair(fpcRole, modifier));
		}

		private static readonly List<Collider> AffectedColliders = new List<Collider>();

		private static readonly List<FpcCollisionProcessor.RoleModifierPair> ActiveModifiers = new List<FpcCollisionProcessor.RoleModifierPair>();

		private static readonly Collider[] CollidersNonAlloc = new Collider[32];

		private static readonly RaycastHit[] HitsNonAlloc = new RaycastHit[16];

		private class RoleModifierPair
		{
			public RoleModifierPair(IFpcRole role, IFpcCollisionModifier modifier)
			{
				this.Role = role;
				this.Modifier = modifier;
				this._motor = this.Role.FpcModule.Motor;
				this._motor.OnBeforeMove += this.OnBeforeMove;
			}

			public void Unlink()
			{
				if (this._motor == null)
				{
					return;
				}
				this._motor.OnBeforeMove -= this.OnBeforeMove;
			}

			private void OnBeforeMove(Vector3 moveDir)
			{
				FpcCollisionProcessor.ProcessPair(this, moveDir);
			}

			public readonly IFpcRole Role;

			public readonly IFpcCollisionModifier Modifier;

			private readonly FpcMotor _motor;
		}
	}
}

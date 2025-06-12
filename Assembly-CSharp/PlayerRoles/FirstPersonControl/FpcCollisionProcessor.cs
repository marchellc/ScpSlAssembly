using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl;

public static class FpcCollisionProcessor
{
	private class RoleModifierPair
	{
		public readonly IFpcRole Role;

		public readonly IFpcCollisionModifier Modifier;

		private readonly FpcMotor _motor;

		public RoleModifierPair(IFpcRole role, IFpcCollisionModifier modifier)
		{
			this.Role = role;
			this.Modifier = modifier;
			this._motor = this.Role.FpcModule.Motor;
			this._motor.OnBeforeMove += OnBeforeMove;
		}

		public void Unlink()
		{
			if (this._motor != null)
			{
				this._motor.OnBeforeMove -= OnBeforeMove;
			}
		}

		private void OnBeforeMove(Vector3 moveDir)
		{
			FpcCollisionProcessor.ProcessPair(this, moveDir);
		}
	}

	private static readonly List<Collider> AffectedColliders = new List<Collider>();

	private static readonly List<RoleModifierPair> ActiveModifiers = new List<RoleModifierPair>();

	private static readonly Collider[] CollidersNonAlloc = new Collider[32];

	private static readonly RaycastHit[] HitsNonAlloc = new RaycastHit[16];

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		FirstPersonMovementModule.OnPositionUpdated += RestoreColliders;
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private static void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (prevRole is IFpcRole role)
		{
			FpcCollisionProcessor.RemoveAllModifiers(role);
		}
	}

	private static void RestoreColliders()
	{
		foreach (Collider affectedCollider in FpcCollisionProcessor.AffectedColliders)
		{
			if (!(affectedCollider == null))
			{
				affectedCollider.enabled = true;
			}
		}
		FpcCollisionProcessor.AffectedColliders.Clear();
	}

	private static void ProcessPair(RoleModifierPair pair, Vector3 moveDir)
	{
		CharacterController charController = pair.Role.FpcModule.CharController;
		Vector3 position = pair.Role.FpcModule.Position;
		Vector3 vector = Vector3.up * charController.height / 2f;
		LayerMask detectionMask = pair.Modifier.DetectionMask;
		float radius = charController.radius + charController.skinWidth;
		int num = Physics.OverlapCapsuleNonAlloc(position + vector, position - vector, radius, FpcCollisionProcessor.CollidersNonAlloc, detectionMask);
		int num2 = num;
		float magnitude = moveDir.magnitude;
		if (magnitude > 0f)
		{
			moveDir /= magnitude;
			int num3 = Physics.CapsuleCastNonAlloc(position + vector, position - vector, radius, moveDir, FpcCollisionProcessor.HitsNonAlloc, magnitude, detectionMask);
			for (int i = 0; i < num3; i++)
			{
				FpcCollisionProcessor.CollidersNonAlloc[num + i] = FpcCollisionProcessor.HitsNonAlloc[i].collider;
			}
			num2 += num3;
		}
		pair.Modifier.ProcessColliders(new ArraySegment<Collider>(FpcCollisionProcessor.CollidersNonAlloc, 0, num2));
		for (int j = 0; j < num2; j++)
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
			RoleModifierPair roleModifierPair = FpcCollisionProcessor.ActiveModifiers[i];
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
			RoleModifierPair roleModifierPair = FpcCollisionProcessor.ActiveModifiers[i];
			if (FpcCollisionProcessor.ActiveModifiers[i].Modifier == modifier)
			{
				roleModifierPair.Unlink();
				FpcCollisionProcessor.ActiveModifiers.RemoveAt(i--);
			}
		}
	}

	public static void AddModifier(IFpcCollisionModifier modifier, IFpcRole fpcRole)
	{
		FpcCollisionProcessor.ActiveModifiers.Add(new RoleModifierPair(fpcRole, modifier));
	}
}

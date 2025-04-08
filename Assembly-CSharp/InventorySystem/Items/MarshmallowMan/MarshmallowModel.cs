using System;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.MarshmallowMan
{
	public class MarshmallowModel : AnimatedCharacterModel
	{
		protected override void Update()
		{
			base.Update();
			if (base.Pooled)
			{
				return;
			}
			base.Animator.SetBool(MarshmallowModel.HashGrounded, base.FpcModule.Noclip.IsActive || base.FpcModule.IsGrounded);
		}

		private static readonly int HashGrounded = Animator.StringToHash("Grounded");
	}
}

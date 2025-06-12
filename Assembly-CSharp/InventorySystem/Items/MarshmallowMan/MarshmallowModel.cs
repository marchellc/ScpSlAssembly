using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.MarshmallowMan;

public class MarshmallowModel : AnimatedCharacterModel
{
	private static readonly int HashGrounded = Animator.StringToHash("Grounded");

	protected override void Update()
	{
		base.Update();
		if (!base.Pooled)
		{
			base.Animator.SetBool(MarshmallowModel.HashGrounded, base.FpcModule.Noclip.IsActive || base.FpcModule.IsGrounded);
		}
	}
}

using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints
{
	public class RoleSpawnpointVisualizer : MonoBehaviour
	{
		private void OnDrawGizmosSelected()
		{
			Gizmos.color = this._gizmosColor;
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(this._role, out playerRoleBase))
			{
				return;
			}
			IFpcRole fpcRole = playerRoleBase as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			ISpawnpointHandler spawnpointHandler = fpcRole.SpawnpointHandler;
			if (spawnpointHandler == null)
			{
				return;
			}
			for (int i = 0; i < this._numberOfTests; i++)
			{
				Vector3 vector;
				float num;
				if (spawnpointHandler.TryGetSpawnpoint(out vector, out num))
				{
					Gizmos.DrawWireSphere(vector, 0.2f);
				}
			}
		}

		[SerializeField]
		private Color _gizmosColor = Color.white;

		[SerializeField]
		private int _numberOfTests = 64;

		[SerializeField]
		private RoleTypeId _role;
	}
}

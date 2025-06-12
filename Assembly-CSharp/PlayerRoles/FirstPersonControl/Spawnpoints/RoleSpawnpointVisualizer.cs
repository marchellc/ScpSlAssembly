using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints;

public class RoleSpawnpointVisualizer : MonoBehaviour
{
	[SerializeField]
	private Color _gizmosColor = Color.white;

	[SerializeField]
	private int _numberOfTests = 64;

	[SerializeField]
	private RoleTypeId _role;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = this._gizmosColor;
		if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(this._role, out var result) || !(result is IFpcRole { SpawnpointHandler: { } spawnpointHandler }))
		{
			return;
		}
		for (int i = 0; i < this._numberOfTests; i++)
		{
			if (spawnpointHandler.TryGetSpawnpoint(out var position, out var _))
			{
				Gizmos.DrawWireSphere(position, 0.2f);
			}
		}
	}
}

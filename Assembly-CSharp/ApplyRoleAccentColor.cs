using PlayerRoles;
using UnityEngine;
using UnityEngine.UI;

public class ApplyRoleAccentColor : MonoBehaviour
{
	[SerializeField]
	private RoleAccentColor _color;

	private Graphic _graphic;

	private void Awake()
	{
		this._graphic = base.GetComponent<Graphic>();
		this._graphic.color = this._color.Color;
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private void OnDestroy()
	{
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (userHub.isLocalPlayer)
		{
			this._graphic.color = this._color.Color;
		}
	}
}

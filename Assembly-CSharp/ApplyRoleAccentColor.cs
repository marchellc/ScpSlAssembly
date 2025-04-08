using System;
using PlayerRoles;
using UnityEngine;
using UnityEngine.UI;

public class ApplyRoleAccentColor : MonoBehaviour
{
	private void Awake()
	{
		this._graphic = base.GetComponent<Graphic>();
		this._graphic.color = this._color.Color;
		PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
	}

	private void OnDestroy()
	{
		PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (!userHub.isLocalPlayer)
		{
			return;
		}
		this._graphic.color = this._color.Color;
	}

	[SerializeField]
	private RoleAccentColor _color;

	private Graphic _graphic;
}

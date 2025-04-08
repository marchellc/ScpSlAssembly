using System;
using PlayerRoles;
using UnityEngine;

[Serializable]
public class RoleAccentColor
{
	public Color Color
	{
		get
		{
			if (this.RoleColorInfluence == 0f)
			{
				return this.ClampBrigtness(this._savedColor);
			}
			ReferenceHub referenceHub;
			bool flag;
			Color color;
			if (ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				PlayerRoleBase currentRole = referenceHub.roleManager.CurrentRole;
				flag = this._prevType == currentRole.RoleTypeId;
				color = currentRole.RoleColor;
			}
			else
			{
				flag = false;
				color = this._fallbackColor;
			}
			if (this.RoleColorInfluence == 2f)
			{
				return color;
			}
			if (!flag)
			{
				Color color2 = new Color(this._savedColor.r * color.r, this._savedColor.g * color.g, this._savedColor.b * color.b, this._savedColor.a * color.a);
				if (this.RoleColorInfluence == 1f)
				{
					this._prevMixed = color2;
				}
				else if (this.RoleColorInfluence < 1f)
				{
					this._prevMixed = Color.Lerp(this._savedColor, color2, this.RoleColorInfluence);
				}
				else
				{
					this._prevMixed = Color.Lerp(color2, color, this.RoleColorInfluence - 1f);
				}
				this._prevMixed = this.ClampBrigtness(this._prevMixed);
			}
			return this._prevMixed;
		}
		set
		{
			this._savedColor = value;
			this._prevType = (RoleTypeId)(-128);
		}
	}

	private Color ClampBrigtness(Color color)
	{
		float grayscale = color.grayscale;
		if (grayscale < this._minBrightness)
		{
			float num = this._minBrightness - grayscale;
			return new Color(Mathf.Clamp01(color.r + num), Mathf.Clamp01(color.g + num), Mathf.Clamp01(color.b + num), color.a);
		}
		if (grayscale > this._maxBrightness)
		{
			float num2 = this._maxBrightness / grayscale;
			return new Color(Mathf.Clamp01(color.r * num2), Mathf.Clamp01(color.g * num2), Mathf.Clamp01(color.b * num2), color.a);
		}
		return color;
	}

	[SerializeField]
	private Color _savedColor = Color.white;

	[SerializeField]
	private Color _fallbackColor = Color.white;

	[SerializeField]
	[Range(0f, 1f)]
	private float _minBrightness;

	[SerializeField]
	[Range(0f, 1f)]
	private float _maxBrightness = 1f;

	[Range(0f, 2f)]
	public float RoleColorInfluence = 1f;

	private Color _prevMixed = Color.white;

	private RoleTypeId _prevType = (RoleTypeId)(-128);

	private const RoleTypeId NoPrevRole = (RoleTypeId)(-128);
}

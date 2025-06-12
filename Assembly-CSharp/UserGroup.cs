using System;

[Serializable]
public class UserGroup
{
	public string Name;

	public string BadgeColor;

	public string BadgeText;

	public ulong Permissions;

	public bool Cover;

	public bool HiddenByDefault;

	public bool Shared;

	public byte KickPower;

	public byte RequiredKickPower;

	public UserGroup Clone()
	{
		return new UserGroup
		{
			Name = this.Name,
			BadgeColor = this.BadgeColor,
			BadgeText = this.BadgeText,
			Permissions = this.Permissions,
			Cover = this.Cover,
			HiddenByDefault = this.HiddenByDefault,
			Shared = this.Shared,
			KickPower = this.KickPower,
			RequiredKickPower = this.RequiredKickPower
		};
	}
}

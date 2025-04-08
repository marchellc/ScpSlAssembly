using System;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	public static class SpectatableListColors
	{
		public static Color MixAvatarColor(Color roleColor)
		{
			Color color = new Color(0.443f, 0.443f, 0.443f, 1f);
			return Color.Lerp(roleColor.linear, color, 0.63f);
		}

		public static readonly Color BgRegular = new Color(0.161f, 0.161f, 0.161f, 0.761f);

		public static readonly Color BgSelected = new Color(0.161f, 0.161f, 0.161f, 1f);

		public static readonly Color Nickname = new Color(0.518f, 0.518f, 0.518f, 1f);

		public static readonly Color Shield = new Color(0.682f, 0.682f, 0.682f, 1f);
	}
}

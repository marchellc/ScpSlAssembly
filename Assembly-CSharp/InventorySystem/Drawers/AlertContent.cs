using System;
using System.Text;
using NorthwoodLib.Pools;
using UnityEngine;

namespace InventorySystem.Drawers
{
	public readonly struct AlertContent
	{
		public string ParseText(Color roleColor)
		{
			if (!this.Active)
			{
				return string.Empty;
			}
			switch (this.SelectedColorMode)
			{
			case AlertContent.ColorMode.White:
				return this.GetWhiteColorTag() + this.Text + "</color>";
			case AlertContent.ColorMode.Role:
				return this.GetRoleColorTag(roleColor) + this.Text + "</color>";
			case AlertContent.ColorMode.Accented:
				return this.ParseAccented(roleColor);
			default:
				return null;
			}
		}

		private string ParseAccented(Color roleColor)
		{
			string whiteColorTag = this.GetWhiteColorTag();
			string roleColorTag = this.GetRoleColorTag(roleColor);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent(whiteColorTag);
			bool flag = false;
			foreach (char c in this.Text)
			{
				if (c != '$')
				{
					stringBuilder.Append(c);
				}
				else
				{
					stringBuilder.Append("</color>");
					flag = !flag;
					if (flag)
					{
						stringBuilder.Append(roleColorTag);
					}
					else
					{
						stringBuilder.Append(whiteColorTag);
					}
				}
			}
			stringBuilder.Append("</color>");
			return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
		}

		private string GetRoleColorTag(Color roleColor)
		{
			roleColor.a = this.Alpha;
			return "<color=" + roleColor.ToHex() + ">";
		}

		private string GetWhiteColorTag()
		{
			Color color = new Color(1f, 1f, 1f, this.Alpha);
			return "<color=" + color.ToHex() + ">";
		}

		public AlertContent(string content, float alpha = 1f, AlertContent.ColorMode color = AlertContent.ColorMode.Accented)
		{
			this.Text = content;
			this.Alpha = alpha;
			this.SelectedColorMode = color;
			this.Active = !string.IsNullOrEmpty(content) && alpha > 0f;
		}

		public readonly bool Active;

		public readonly string Text;

		public readonly AlertContent.ColorMode SelectedColorMode;

		public readonly float Alpha;

		private const char ToggleAccentSymbol = '$';

		private const string StopColorTag = "</color>";

		public enum ColorMode
		{
			White,
			Role,
			Accented
		}
	}
}

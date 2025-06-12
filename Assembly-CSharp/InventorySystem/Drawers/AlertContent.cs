using System.Text;
using NorthwoodLib.Pools;
using UnityEngine;

namespace InventorySystem.Drawers;

public readonly struct AlertContent
{
	public enum ColorMode
	{
		White,
		Role,
		Accented
	}

	public readonly bool Active;

	public readonly string Text;

	public readonly ColorMode SelectedColorMode;

	public readonly float Alpha;

	private const char ToggleAccentSymbol = '$';

	private const string StopColorTag = "</color>";

	public string ParseText(Color roleColor)
	{
		if (!this.Active)
		{
			return string.Empty;
		}
		return this.SelectedColorMode switch
		{
			ColorMode.Role => this.GetRoleColorTag(roleColor) + this.Text + "</color>", 
			ColorMode.White => this.GetWhiteColorTag() + this.Text + "</color>", 
			ColorMode.Accented => this.ParseAccented(roleColor), 
			_ => null, 
		};
	}

	private string ParseAccented(Color roleColor)
	{
		string whiteColorTag = this.GetWhiteColorTag();
		string roleColorTag = this.GetRoleColorTag(roleColor);
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent(whiteColorTag);
		bool flag = false;
		string text = this.Text;
		foreach (char c in text)
		{
			if (c != '$')
			{
				stringBuilder.Append(c);
				continue;
			}
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

	public AlertContent(string content, float alpha = 1f, ColorMode color = ColorMode.Accented)
	{
		this.Text = content;
		this.Alpha = alpha;
		this.SelectedColorMode = color;
		this.Active = !string.IsNullOrEmpty(content) && alpha > 0f;
	}
}

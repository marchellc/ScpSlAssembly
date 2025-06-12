using System;
using Mirror;
using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class CustomLabelDetail : SyncedDetail, ICustomizableDetail
{
	private static string _customText;

	private static Color32 _customColor;

	public int CustomizablePropertiesAmount => 2;

	public string[] CommandArguments => new string[2] { "Label (use '_' instead of spaces)", "Label text color (hex or name)" };

	public void ParseArguments(ArraySegment<string> args)
	{
		CustomLabelDetail._customText = args.At(0).Replace('_', ' ');
		Misc.TryParseColor(args.At(1), out CustomLabelDetail._customColor);
	}

	public void SetArguments(ArraySegment<object> args)
	{
		CustomLabelDetail._customText = (string)args.At(0);
		CustomLabelDetail._customColor = (Color32)args.At(1);
	}

	private void WriteCustom(NetworkWriter writer)
	{
		writer.WriteString(CustomLabelDetail._customText);
		writer.WriteColor32(CustomLabelDetail._customColor);
	}

	public override void WriteDefault(NetworkWriter writer)
	{
		this.WriteCustom(writer);
	}

	public override void WriteNewItem(KeycardItem item, NetworkWriter writer)
	{
		this.WriteCustom(writer);
	}

	public override void WriteNewPickup(KeycardPickup pickup, NetworkWriter writer)
	{
		this.WriteCustom(writer);
	}

	protected override void ApplyDetail(KeycardGfx target, NetworkReader reader, KeycardItem template)
	{
		string text = reader.ReadString();
		Color color = reader.ReadColor32();
		TMP_Text[] keycardLabels = target.KeycardLabels;
		foreach (TMP_Text obj in keycardLabels)
		{
			obj.text = text;
			obj.color = color;
		}
	}
}

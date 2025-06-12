using System;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class CustomRankDetail : SyncedDetail, ICustomizableDetail
{
	private static int _index;

	[SerializeField]
	private Mesh[] _options;

	public string[] CommandArguments => new string[1] { "Rank detail option (index)" };

	public int CustomizablePropertiesAmount => 1;

	public void ParseArguments(ArraySegment<string> args)
	{
		CustomRankDetail._index = int.Parse(args.At(0));
	}

	public void SetArguments(ArraySegment<object> args)
	{
		CustomRankDetail._index = (int)args.At(0);
	}

	private void WriteCustom(NetworkWriter writer)
	{
		writer.WriteByte((byte)(Mathf.Abs(CustomRankDetail._index) % this._options.Length));
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
		target.RankFilter.sharedMesh = this._options[reader.ReadByte()];
	}
}

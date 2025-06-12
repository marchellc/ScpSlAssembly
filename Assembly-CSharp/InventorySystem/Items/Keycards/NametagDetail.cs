using System;
using MapGeneration;
using Mirror;
using PlayerRoles;
using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class NametagDetail : SyncedDetail, ICustomizableDetail
{
	[Serializable]
	private struct RolePrefixOverride
	{
		public RoleTypeId[] Roles;

		public KeycardWordsCollection Prefix;
	}

	private const string DefaultName = "Name Not Set";

	[SerializeField]
	private RolePrefixOverride[] _roleBasedPrefixes;

	[SerializeField]
	private KeycardWordsCollection _fallbackPrefix;

	[SerializeField]
	private KeycardWordsCollection _fakeNamesPeople;

	[SerializeField]
	private KeycardWordsCollection _fakeNamesScp914;

	private static string _customNametag;

	public int CustomizablePropertiesAmount => 1;

	public string[] CommandArguments => new string[1] { "Card holder name (use '_' instead of spaces)" };

	public void ParseArguments(ArraySegment<string> args)
	{
		NametagDetail._customNametag = args.At(0).Replace('_', ' ');
	}

	public void SetArguments(ArraySegment<object> args)
	{
		NametagDetail._customNametag = (string)args.At(0);
	}

	public override void WriteDefault(NetworkWriter writer)
	{
		this.WriteNameWithPrefix(writer, "Name Not Set", RoleTypeId.None);
	}

	public override void WriteNewItem(KeycardItem item, NetworkWriter writer)
	{
		if (item.Customizable)
		{
			writer.WriteString(NametagDetail._customNametag);
			return;
		}
		ReferenceHub owner = item.Owner;
		this.WriteNameWithPrefix(writer, owner.nicknameSync.DisplayName, owner.GetRoleId());
	}

	public override void WriteNewPickup(KeycardPickup pickup, NetworkWriter writer)
	{
		if (pickup.Position.TryGetRoom(out var room) && room.Name == RoomName.Lcz914)
		{
			writer.WriteString(this._fakeNamesScp914.NextRandomWord());
		}
		else
		{
			this.WriteNameWithPrefix(writer, this._fakeNamesPeople.NextRandomWord(), RoleTypeId.None);
		}
	}

	protected override void ApplyDetail(KeycardGfx target, NetworkReader reader, KeycardItem template)
	{
		string text = reader.ReadString();
		TMP_Text[] nameFields = target.NameFields;
		for (int i = 0; i < nameFields.Length; i++)
		{
			nameFields[i].text = text;
		}
	}

	private void WriteNameWithPrefix(NetworkWriter writer, string name, RoleTypeId role)
	{
		KeycardWordsCollection keycardWordsCollection = this._fallbackPrefix;
		RolePrefixOverride[] roleBasedPrefixes = this._roleBasedPrefixes;
		for (int i = 0; i < roleBasedPrefixes.Length; i++)
		{
			RolePrefixOverride rolePrefixOverride = roleBasedPrefixes[i];
			if (rolePrefixOverride.Roles.Contains(role))
			{
				keycardWordsCollection = rolePrefixOverride.Prefix;
				break;
			}
		}
		if (keycardWordsCollection == null)
		{
			writer.WriteString(name);
		}
		else
		{
			writer.WriteString(keycardWordsCollection.Words.RandomItem() + " " + name);
		}
	}
}

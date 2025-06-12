using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Respawning.NamingRules;

public class NineTailedFoxNamingRule : UnitNamingRule
{
	private const int MinUnitNumber = 1;

	private const int MaxUnitNumber = 19;

	private readonly HashSet<int> _usedCombos = new HashSet<int>();

	private static readonly string[] PossibleCodes = new string[26]
	{
		"ALPHA", "BRAVO", "CHARLIE", "DELTA", "ECHO", "FOXTROT", "GOLF", "HOTEL", "INDIA", "JULIETT",
		"KILO", "LIMA", "MIKE", "NOVEMBER", "OSCAR", "PAPA", "QUEBEC", "ROMEO", "SIERRA", "TANGO",
		"UNIFORM", "VICTOR", "WHISKEY", "XRAY", "YANKEE", "ZULU"
	};

	private int _lastUnitNumber;

	private int _lastUnitNato;

	public override void GenerateNew()
	{
		do
		{
			this._lastUnitNato = Random.Range(0, NineTailedFoxNamingRule.PossibleCodes.Length - 1);
			this._lastUnitNumber = Random.Range(1, 19);
		}
		while (!this._usedCombos.Add(this._lastUnitNato * 255 + this._lastUnitNumber));
		base.LastGeneratedName = this.ReadName(this._lastUnitNato, this._lastUnitNumber);
	}

	public override void WriteName(NetworkWriter writer)
	{
		writer.WriteByte((byte)this._lastUnitNato);
		writer.WriteByte((byte)this._lastUnitNumber);
	}

	public override string ReadName(NetworkReader reader)
	{
		return this.ReadName(reader.ReadByte(), reader.ReadByte());
	}

	public string ReadName(int nato, int num)
	{
		return $"{NineTailedFoxNamingRule.PossibleCodes[nato]}-{num:00}";
	}

	public override string TranslateToCassie(string untranslatedString)
	{
		try
		{
			string[] array = untranslatedString.Split('-');
			return "NATO_" + array[0][0] + " " + array[1];
		}
		catch
		{
			ServerConsole.AddLog("Error, couldn't convert '" + untranslatedString + "' into a CASSIE-readable form.");
			return "ERROR";
		}
	}

	public override int GetRolePower(RoleTypeId role)
	{
		switch (role)
		{
		case RoleTypeId.NtfPrivate:
			return 1;
		case RoleTypeId.NtfSpecialist:
		case RoleTypeId.NtfSergeant:
			return 2;
		case RoleTypeId.NtfCaptain:
			return 3;
		default:
			return 0;
		}
	}
}

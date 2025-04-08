using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace Respawning.NamingRules
{
	public class NineTailedFoxNamingRule : UnitNamingRule
	{
		public override void GenerateNew()
		{
			do
			{
				this._lastUnitNato = global::UnityEngine.Random.Range(0, NineTailedFoxNamingRule.PossibleCodes.Length - 1);
				this._lastUnitNumber = global::UnityEngine.Random.Range(1, 19);
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
			return this.ReadName((int)reader.ReadByte(), (int)reader.ReadByte());
		}

		public string ReadName(int nato, int num)
		{
			return string.Format("{0}-{1:00}", NineTailedFoxNamingRule.PossibleCodes[nato], num);
		}

		public override string TranslateToCassie(string untranslatedString)
		{
			string text;
			try
			{
				string[] array = untranslatedString.Split('-', StringSplitOptions.None);
				text = "NATO_" + array[0][0].ToString() + " " + array[1];
			}
			catch
			{
				ServerConsole.AddLog("Error, couldn't convert '" + untranslatedString + "' into a CASSIE-readable form.", ConsoleColor.Gray, false);
				text = "ERROR";
			}
			return text;
		}

		public override int GetRolePower(RoleTypeId role)
		{
			if (role != RoleTypeId.NtfSpecialist)
			{
				switch (role)
				{
				case RoleTypeId.NtfSergeant:
					break;
				case RoleTypeId.NtfCaptain:
					return 3;
				case RoleTypeId.NtfPrivate:
					return 1;
				default:
					return 0;
				}
			}
			return 2;
		}

		private const int MinUnitNumber = 1;

		private const int MaxUnitNumber = 19;

		private readonly HashSet<int> _usedCombos = new HashSet<int>();

		private static readonly string[] PossibleCodes = new string[]
		{
			"ALPHA", "BRAVO", "CHARLIE", "DELTA", "ECHO", "FOXTROT", "GOLF", "HOTEL", "INDIA", "JULIETT",
			"KILO", "LIMA", "MIKE", "NOVEMBER", "OSCAR", "PAPA", "QUEBEC", "ROMEO", "SIERRA", "TANGO",
			"UNIFORM", "VICTOR", "WHISKEY", "XRAY", "YANKEE", "ZULU"
		};

		private int _lastUnitNumber;

		private int _lastUnitNato;
	}
}

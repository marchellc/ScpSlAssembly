using System;
using System.Collections.Generic;
using PlayerRoles;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Scp127;

[CreateAssetMenu(fileName = "New Voice Line Collection", menuName = "ScriptableObject/Firearms/SCP-127 Voice Line Collection")]
public class Scp127VoiceLineCollection : ScriptableObject
{
	[Serializable]
	public struct RoleBased
	{
		public RoleTypeId[] Roles;

		public bool AllowTeam;

		public bool AllowGeneric;

		public AudioClip[] Lines;
	}

	[Serializable]
	public struct TeamBased
	{
		public Team[] Teams;

		public bool AllowGeneric;

		public AudioClip[] Lines;
	}

	private static readonly List<AudioClip> CompatibleCombiner = new List<AudioClip>();

	private static readonly List<AudioClip> UnplayedCombiner = new List<AudioClip>();

	private readonly HashSet<AudioClip> _alreadyPlayed = new HashSet<AudioClip>();

	public RoleBased[] RoleLines;

	public TeamBased[] TeamLines;

	public AudioClip[] GenericLines;

	public bool AvoidRepetition;

	public bool TryGetRandom(RoleTypeId playerRole, out AudioClip voiceLine)
	{
		Scp127VoiceLineCollection.CompatibleCombiner.Clear();
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		RoleBased[] roleLines = this.RoleLines;
		for (int i = 0; i < roleLines.Length; i++)
		{
			RoleBased roleBased = roleLines[i];
			if (roleBased.Roles.Contains(playerRole))
			{
				flag |= roleBased.AllowTeam;
				flag2 |= roleBased.AllowGeneric;
				flag3 = true;
				Scp127VoiceLineCollection.CompatibleCombiner.AddRange(roleBased.Lines);
			}
		}
		if (flag || !flag3)
		{
			Team team = playerRole.GetTeam();
			TeamBased[] teamLines = this.TeamLines;
			for (int i = 0; i < teamLines.Length; i++)
			{
				TeamBased teamBased = teamLines[i];
				if (teamBased.Teams.Contains(team))
				{
					flag2 |= teamBased.AllowGeneric;
					flag3 = true;
					Scp127VoiceLineCollection.CompatibleCombiner.AddRange(teamBased.Lines);
				}
			}
		}
		if (flag2 || !flag3)
		{
			Scp127VoiceLineCollection.CompatibleCombiner.AddRange(this.GenericLines);
		}
		if (this.AvoidRepetition)
		{
			Scp127VoiceLineCollection.UnplayedCombiner.Clear();
			foreach (AudioClip item in Scp127VoiceLineCollection.CompatibleCombiner)
			{
				if (!this._alreadyPlayed.Contains(item))
				{
					Scp127VoiceLineCollection.UnplayedCombiner.Add(item);
				}
			}
			if (Scp127VoiceLineCollection.UnplayedCombiner.TryGetRandomItem(out voiceLine))
			{
				this._alreadyPlayed.Add(voiceLine);
				return true;
			}
			this._alreadyPlayed.Clear();
		}
		return Scp127VoiceLineCollection.CompatibleCombiner.TryGetRandomItem(out voiceLine);
	}

	public bool Contains(AudioClip clip)
	{
		if (this.GenericLines.Contains(clip))
		{
			return true;
		}
		RoleBased[] roleLines = this.RoleLines;
		for (int i = 0; i < roleLines.Length; i++)
		{
			if (roleLines[i].Lines.Contains(clip))
			{
				return true;
			}
		}
		TeamBased[] teamLines = this.TeamLines;
		for (int i = 0; i < teamLines.Length; i++)
		{
			if (teamLines[i].Lines.Contains(clip))
			{
				return true;
			}
		}
		return false;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerStatsSystem;
using Respawning;
using Respawning.NamingRules;
using Subtitles;
using UnityEngine;
using Utils;
using Utils.Networking;

public class NineTailedFoxAnnouncer : MonoBehaviour
{
	[Serializable]
	public class VoiceLine
	{
		public string apiName;

		public AudioClip clip;

		public float length;

		public string collection;

		public static bool IsYield(string s, out float value)
		{
			if (s.StartsWith("YIELD_", StringComparison.OrdinalIgnoreCase) || s.StartsWith("YD_", StringComparison.OrdinalIgnoreCase))
			{
				string[] array = s.Split('_');
				if (array.Length == 2)
				{
					return CustomParser.TryParseFloat(array[1], out value) != CustomParser.ParseResult.Failed;
				}
				value = 0f;
				return false;
			}
			value = 0f;
			return false;
		}

		public static bool IsJam(string s, out int percent, out int amount)
		{
			bool num = s.StartsWith("JAM_", StringComparison.OrdinalIgnoreCase);
			percent = 0;
			amount = 0;
			if (!num)
			{
				return false;
			}
			string[] array = s.Split('_');
			if (array.Length != 3)
			{
				return false;
			}
			if (CustomParser.TryParseInt(array[1], out var val) == CustomParser.ParseResult.Failed)
			{
				return false;
			}
			percent = val;
			if (CustomParser.TryParseInt(array[2], out val) == CustomParser.ParseResult.Failed)
			{
				return false;
			}
			amount = val;
			return true;
		}

		public static bool IsPitch(string s, out float value)
		{
			bool num = s.StartsWith("PITCH_", StringComparison.OrdinalIgnoreCase) || s.StartsWith("PI_", StringComparison.OrdinalIgnoreCase);
			value = 1f;
			if (!num)
			{
				return false;
			}
			string[] array = s.Split('_');
			if (array.Length < 2)
			{
				return false;
			}
			if (CustomParser.TryParseFloat(array[1], out var val) == CustomParser.ParseResult.Failed)
			{
				return false;
			}
			if (val > 0f)
			{
				value = val;
			}
			return true;
		}

		public static bool IsRegular(string s)
		{
			if (!VoiceLine.IsYield(s, out var value) && !VoiceLine.IsJam(s, out var _, out var _))
			{
				return !VoiceLine.IsPitch(s, out value);
			}
			return false;
		}

		public string GetName()
		{
			return this.apiName;
		}
	}

	[Serializable]
	public struct ScpDeath : IEquatable<ScpDeath>
	{
		public List<RoleTypeId> scpSubjects;

		public string announcement;

		public SubtitlePart[] subtitleParts;

		public bool Equals(ScpDeath other)
		{
			if (this.scpSubjects == other.scpSubjects)
			{
				return string.Equals(this.announcement, other.announcement);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is ScpDeath other)
			{
				return this.Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((this.scpSubjects != null) ? this.scpSubjects.GetHashCode() : 0) * 397) ^ ((this.announcement != null) ? this.announcement.GetHashCode() : 0);
		}

		public static bool operator ==(ScpDeath left, ScpDeath right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ScpDeath left, ScpDeath right)
		{
			return !left.Equals(right);
		}
	}

	private class ItemEqualityComparer : IEqualityComparer<VoiceLine>
	{
		public bool Equals(VoiceLine x, VoiceLine y)
		{
			if (x != null)
			{
				if (x.clip != null)
				{
					return x.clip == y.clip;
				}
				return false;
			}
			return false;
		}

		public int GetHashCode(VoiceLine obj)
		{
			return obj.clip.GetHashCode();
		}
	}

	public VoiceLine[] voiceLines;

	public AudioClip[] backgroundLines;

	public AudioClip suffixPluralStandard;

	public AudioClip suffixPluralException;

	public AudioClip suffixPastStandard;

	public AudioClip suffixPastException;

	public AudioClip suffixContinuous;

	public const string EndOfMessageSignal = "END_OF_MESSAGE";

	public readonly List<VoiceLine> queue = new List<VoiceLine>();

	private List<string> newWords = new List<string>();

	private readonly List<VoiceLine> newLines = new List<VoiceLine>();

	private static readonly List<ScpDeath> scpDeaths = new List<ScpDeath>();

	public AudioSource speakerSource;

	public AudioSource backgroundSource;

	private readonly Regex UniqueKeyRegex = new Regex("(jam_\\d{1,3}_\\d{1,3})|(pitch_[\\d\\.]{1,4})|(\\.g\\d{1,3})|(bell_start)|(bell_end)", RegexOptions.IgnoreCase);

	private readonly Regex WhiteSpaceRegex = new Regex("\\s+");

	public static NineTailedFoxAnnouncer singleton;

	private float scpListTimer;

	public static event Action<VoiceLine> OnLineDequeued;

	public static string ConvertNumber(int num)
	{
		if (num <= 0)
		{
			return " 0 ";
		}
		ushort num2 = 0;
		byte b = 0;
		byte b2 = 0;
		while ((float)num / 1000f >= 1f)
		{
			num2++;
			num -= 1000;
		}
		while ((float)num / 100f >= 1f)
		{
			b++;
			num -= 100;
		}
		if (num >= 20)
		{
			while ((float)num / 10f >= 1f)
			{
				b2++;
				num -= 10;
			}
		}
		string text = string.Empty;
		if (num2 > 0)
		{
			text = text + NineTailedFoxAnnouncer.ConvertNumber(num2) + " thousand ";
		}
		if (b > 0)
		{
			text = text + b + " hundred ";
		}
		if (b + num2 > 0 && (num > 0 || b2 > 0))
		{
			text += " and ";
		}
		if (b2 > 0)
		{
			text = text + b2 + "0 ";
		}
		if (num > 0)
		{
			text = text + num + " ";
		}
		return text;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerStats.OnAnyPlayerDied += AnnounceScpTermination;
	}

	public static void AnnounceScpTermination(ReferenceHub scp, DamageHandlerBase hit)
	{
		NineTailedFoxAnnouncer.singleton.scpListTimer = 0f;
		if (!scp.IsSCP(includeZombies: false))
		{
			return;
		}
		string announcement = hit.CassieDeathAnnouncement.Announcement;
		SubtitlePart[] subtitleParts = hit.CassieDeathAnnouncement.SubtitleParts;
		CassieQueuingScpTerminationEventArgs e = new CassieQueuingScpTerminationEventArgs(scp, announcement, subtitleParts);
		ServerEvents.OnCassieQueuingScpTermination(e);
		if (!e.IsAllowed)
		{
			return;
		}
		announcement = e.Announcement;
		subtitleParts = e.SubtitleParts;
		if (string.IsNullOrEmpty(announcement))
		{
			return;
		}
		foreach (ScpDeath scpDeath in NineTailedFoxAnnouncer.scpDeaths)
		{
			if (!(scpDeath.announcement != announcement))
			{
				scpDeath.scpSubjects.Add(scp.GetRoleId());
				return;
			}
		}
		NineTailedFoxAnnouncer.scpDeaths.Add(new ScpDeath
		{
			scpSubjects = new List<RoleTypeId>(new RoleTypeId[1] { scp.GetRoleId() }),
			announcement = announcement,
			subtitleParts = subtitleParts
		});
		ServerEvents.OnCassieQueuedScpTermination(new CassieQueuedScpTerminationEventArgs(scp, announcement, subtitleParts));
	}

	public float CalculateDuration(string tts, bool rawNumber = false, float speed = 1f)
	{
		float num = 0f;
		string[] array = tts.Split(' ');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			if (VoiceLine.IsJam(text, out var j, out var amount))
			{
				num += 0.13f * (float)amount;
				continue;
			}
			if (VoiceLine.IsRegular(text))
			{
				bool flag = false;
				for (int k = i + 1; k < array.Length && !VoiceLine.IsRegular(array[k]); k++)
				{
					if (VoiceLine.IsYield(array[k], out var value))
					{
						num += value;
						flag = true;
						break;
					}
				}
				if (flag)
				{
					continue;
				}
			}
			if (VoiceLine.IsPitch(text, out var value2))
			{
				speed = value2;
				continue;
			}
			if (int.TryParse(text, out var result) && !rawNumber)
			{
				num += this.CalculateDuration(NineTailedFoxAnnouncer.ConvertNumber(result), rawNumber: true, speed);
				continue;
			}
			bool flag2 = false;
			VoiceLine[] array2 = this.voiceLines;
			for (j = 0; j < array2.Length; j++)
			{
				VoiceLine voiceLine = array2[j];
				if (string.Equals(text, voiceLine.apiName, StringComparison.OrdinalIgnoreCase))
				{
					flag2 = true;
					num += voiceLine.length / speed;
				}
			}
			if (flag2 || text.Length <= 3)
			{
				continue;
			}
			for (byte b = 1; b <= 3; b++)
			{
				array2 = this.voiceLines;
				for (j = 0; j < array2.Length; j++)
				{
					VoiceLine voiceLine2 = array2[j];
					if (string.Equals(text.Remove(text.Length - b), voiceLine2.apiName, StringComparison.OrdinalIgnoreCase))
					{
						num += voiceLine2.length / speed;
					}
				}
			}
		}
		return num;
	}

	public void ServerOnlyAddGlitchyPhrase(string tts, float glitchChance, float jamChance)
	{
		string[] array = tts.Split(' ');
		this.newWords.Clear();
		this.newWords.EnsureCapacity(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			this.newWords.Add(array[i]);
			if (i < array.Length - 1)
			{
				if (UnityEngine.Random.value < glitchChance)
				{
					this.newWords.Add(".G" + UnityEngine.Random.Range(1, 7));
				}
				if (UnityEngine.Random.value < jamChance)
				{
					this.newWords.Add("JAM_" + UnityEngine.Random.Range(0, 70).ToString("000") + "_" + UnityEngine.Random.Range(2, 6));
				}
			}
		}
		tts = "";
		foreach (string newWord in this.newWords)
		{
			tts = tts + newWord + " ";
		}
		RespawnEffectsController.PlayCassieAnnouncement(tts, makeHold: false, makeNoise: true);
	}

	public void ClearQueue()
	{
		this.queue.Clear();
		this.backgroundSource.Stop();
		this.speakerSource.Stop();
	}

	public void AddPhraseToQueue(string tts, bool generateNoise, bool rawNumber = false, bool makeHold = false, bool customAnnouncement = false, string customSubtitles = "")
	{
		string[] array = tts.Split(' ');
		if (!rawNumber)
		{
			float num = this.CalculateDuration(tts);
			int num2 = 0;
			for (int i = 0; i < this.backgroundLines.Length - 1; i++)
			{
				if ((float)i < num)
				{
					num2 = i + 1;
				}
			}
			if (generateNoise)
			{
				this.queue.Add(new VoiceLine
				{
					apiName = "BG_BACKGROUND",
					clip = this.backgroundLines[num2],
					length = 2.5f
				});
			}
		}
		float num3 = 1f;
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!VoiceLine.IsRegular(text))
			{
				this.queue.Add(new VoiceLine
				{
					apiName = text.ToUpper()
				});
				continue;
			}
			if (!rawNumber && float.TryParse(text, out var result))
			{
				this.AddPhraseToQueue(NineTailedFoxAnnouncer.ConvertNumber((int)result), generateNoise: false, rawNumber: true);
				continue;
			}
			bool flag = false;
			VoiceLine[] array3 = this.voiceLines;
			foreach (VoiceLine voiceLine in array3)
			{
				if (string.Equals(text, voiceLine.apiName, StringComparison.OrdinalIgnoreCase))
				{
					this.queue.Add(new VoiceLine
					{
						apiName = voiceLine.apiName,
						clip = voiceLine.clip,
						length = voiceLine.length / num3
					});
					flag = true;
				}
			}
			if (flag)
			{
				continue;
			}
			VoiceLine voiceLine2 = null;
			if (text.Length > 3)
			{
				for (byte b = 1; b <= 4; b++)
				{
					if (text.Length > b)
					{
						array3 = this.voiceLines;
						foreach (VoiceLine voiceLine3 in array3)
						{
							if ((string.Equals(text.Remove(text.Length - b), voiceLine3.apiName, StringComparison.OrdinalIgnoreCase) || (text.EndsWith("ING", StringComparison.OrdinalIgnoreCase) && string.Equals(text.Remove(text.Length - b) + "E", voiceLine3.apiName, StringComparison.OrdinalIgnoreCase))) && voiceLine2 == null)
							{
								voiceLine2 = new VoiceLine
								{
									apiName = voiceLine3.apiName,
									clip = voiceLine3.clip,
									length = voiceLine3.length / num3
								};
							}
						}
					}
				}
			}
			if (voiceLine2 != null)
			{
				AudioClip audioClip = ((!text.EndsWith("TED", StringComparison.OrdinalIgnoreCase) && !text.EndsWith("DED", StringComparison.OrdinalIgnoreCase)) ? (text.EndsWith("D", StringComparison.OrdinalIgnoreCase) ? this.suffixPastStandard : (text.EndsWith("ING", StringComparison.OrdinalIgnoreCase) ? this.suffixContinuous : ((!voiceLine2.apiName.EndsWith("S") && !voiceLine2.apiName.EndsWith("SH") && !voiceLine2.apiName.EndsWith("CH") && !voiceLine2.apiName.EndsWith("X") && !voiceLine2.apiName.EndsWith("Z")) ? this.suffixPluralStandard : this.suffixPluralException))) : this.suffixPastException);
				this.queue.Add(new VoiceLine
				{
					apiName = voiceLine2.apiName,
					clip = voiceLine2.clip,
					length = (voiceLine2.length - (text.EndsWith("ING", StringComparison.OrdinalIgnoreCase) ? 0.1f : 0.06f)) / num3
				});
				this.queue.Add(new VoiceLine
				{
					apiName = "SUFFIX_" + audioClip.name,
					clip = audioClip,
					length = audioClip.length / num3
				});
			}
		}
		if (!rawNumber)
		{
			this.queue.Add(new VoiceLine
			{
				apiName = "PITCH_1"
			});
			for (byte b2 = 0; b2 < ((!makeHold) ? 1 : 3); b2++)
			{
				this.queue.Add(new VoiceLine
				{
					apiName = "END_OF_MESSAGE"
				});
			}
		}
	}

	private IEnumerator Start()
	{
		NineTailedFoxAnnouncer.scpDeaths.Clear();
		NineTailedFoxAnnouncer.singleton = this;
		float speed = 1f;
		int jammed = 0;
		int jamSize = 0;
		WaitForEndOfFrame wait = new WaitForEndOfFrame();
		while (this != null)
		{
			if (this.queue.Count == 0)
			{
				speed = 1f;
				yield return wait;
				continue;
			}
			VoiceLine line = this.queue[0];
			this.queue.RemoveAt(0);
			NineTailedFoxAnnouncer.OnLineDequeued?.Invoke(line);
			if (line.apiName == "END_OF_MESSAGE")
			{
				this.speakerSource.pitch = 1f;
				yield return new WaitForSeconds(4f);
				continue;
			}
			bool flag = line.apiName.StartsWith("BG_") || line.apiName.StartsWith("BELL_");
			bool flag2 = line.apiName.StartsWith("SUFFIX_");
			float absoluteTimeAddition = 0f;
			float relativeTimeAddition = 0f;
			float value;
			int percent;
			int amount;
			if (line.clip != null)
			{
				if (flag)
				{
					this.backgroundSource.PlayOneShot(line.clip);
				}
				else if (flag2)
				{
					this.speakerSource.Stop();
					this.speakerSource.PlayOneShot(line.clip);
				}
				else if (jammed > 0)
				{
					this.speakerSource.Stop();
					float timeToJam = line.length * ((float)jammed / 100f);
					this.speakerSource.clip = line.clip;
					this.speakerSource.time = 0f;
					this.speakerSource.Play();
					yield return new WaitForSeconds(timeToJam);
					float stepSize = 0.13f;
					for (int i = 0; i < jamSize; i++)
					{
						absoluteTimeAddition -= stepSize * 3f;
						this.speakerSource.time = timeToJam;
						yield return new WaitForSeconds(stepSize);
					}
					jammed = 0;
				}
				else
				{
					this.speakerSource.PlayOneShot(line.clip);
				}
			}
			else if (VoiceLine.IsPitch(line.apiName, out value))
			{
				speed = value;
				this.speakerSource.pitch = speed;
			}
			else if (VoiceLine.IsJam(line.apiName, out percent, out amount))
			{
				jammed = percent;
				jamSize = amount;
			}
			if (!VoiceLine.IsRegular(line.apiName))
			{
				continue;
			}
			float num = 0f;
			for (int j = 0; j < this.queue.Count && !VoiceLine.IsRegular(this.queue[j].apiName); j++)
			{
				if (VoiceLine.IsYield(this.queue[j].apiName, out var value2))
				{
					num = value2;
					break;
				}
			}
			if (num > 0f)
			{
				yield return new WaitForSeconds(num);
				continue;
			}
			float num2 = (line.length + relativeTimeAddition) / speed + absoluteTimeAddition;
			if (num2 > 0f)
			{
				yield return new WaitForSeconds(num2);
			}
		}
	}

	private void Update()
	{
		if (NineTailedFoxAnnouncer.scpDeaths.Count <= 0)
		{
			return;
		}
		this.scpListTimer += Time.deltaTime;
		if (this.scpListTimer <= 1f)
		{
			return;
		}
		for (int i = 0; i < NineTailedFoxAnnouncer.scpDeaths.Count; i++)
		{
			ScpDeath scpDeath = NineTailedFoxAnnouncer.scpDeaths[i];
			List<SubtitlePart> list = new List<SubtitlePart>(1);
			string text = "";
			for (int j = 0; j < scpDeath.scpSubjects.Count; j++)
			{
				NineTailedFoxAnnouncer.ConvertSCP(scpDeath.scpSubjects[j], out var withoutSpace, out var withSpace);
				text = ((j != 0) ? (text + ". SCP " + withSpace) : (text + "SCP " + withSpace));
				list.Add(new SubtitlePart(SubtitleType.SCP, withoutSpace));
			}
			text += scpDeath.announcement;
			if (scpDeath.subtitleParts != null)
			{
				list.AddRange(scpDeath.subtitleParts);
			}
			float num = (AlphaWarheadController.Detonated ? 3.5f : 1f);
			this.ServerOnlyAddGlitchyPhrase(text, UnityEngine.Random.Range(0.1f, 0.14f) * num, UnityEngine.Random.Range(0.07f, 0.08f) * num);
			new SubtitleMessage(list.ToArray()).SendToAuthenticated();
		}
		this.scpListTimer = 0f;
		NineTailedFoxAnnouncer.scpDeaths.Clear();
	}

	public static void ConvertSCP(RoleTypeId role, out string withoutSpace, out string withSpace)
	{
		if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out var result))
		{
			withoutSpace = string.Empty;
			withSpace = string.Empty;
		}
		else
		{
			NineTailedFoxAnnouncer.ConvertSCP(result.RoleName, out withoutSpace, out withSpace);
		}
	}

	public static void ConvertSCP(string roleName, out string withoutSpace, out string withSpace)
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		string[] array = roleName.Split('-');
		if (array.Length < 2)
		{
			Debug.LogError("Cassie role cannot be split by '-'. Possibly missing translation.");
			withoutSpace = "404";
			withSpace = "4 0 4";
			return;
		}
		withoutSpace = array[1];
		string text = withoutSpace;
		foreach (char value in text)
		{
			stringBuilder.Append(value);
			stringBuilder.Append(" ");
		}
		withSpace = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}

	public static string ConvertTeam(Team team, string unitName)
	{
		string result = "CONTAINMENTUNIT UNKNOWN";
		switch (team)
		{
		case Team.ClassD:
			return "BY CLASSD PERSONNEL";
		case Team.ChaosInsurgency:
			return "BY CHAOSINSURGENCY";
		case Team.FoundationForces:
		{
			if (!NamingRulesManager.TryGetNamingRule(team, out var rule))
			{
				return result;
			}
			string text = rule.TranslateToCassie(unitName);
			return "CONTAINMENTUNIT " + text;
		}
		case Team.Scientists:
			return "BY SCIENCE PERSONNEL";
		default:
			return result;
		}
	}
}

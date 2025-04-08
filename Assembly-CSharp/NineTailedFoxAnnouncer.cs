using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
			num2 += 1;
			num -= 1000;
		}
		while ((float)num / 100f >= 1f)
		{
			b += 1;
			num -= 100;
		}
		if (num >= 20)
		{
			while ((float)num / 10f >= 1f)
			{
				b2 += 1;
				num -= 10;
			}
		}
		string text = string.Empty;
		if (num2 > 0)
		{
			text = text + NineTailedFoxAnnouncer.ConvertNumber((int)num2) + " thousand ";
		}
		if (b > 0)
		{
			text = text + b.ToString() + " hundred ";
		}
		if ((ushort)b + num2 > 0 && (num > 0 || b2 > 0))
		{
			text += " and ";
		}
		if (b2 > 0)
		{
			text = text + b2.ToString() + "0 ";
		}
		if (num > 0)
		{
			text = text + num.ToString() + " ";
		}
		return text;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerStats.OnAnyPlayerDied += NineTailedFoxAnnouncer.AnnounceScpTermination;
	}

	public static void AnnounceScpTermination(ReferenceHub scp, DamageHandlerBase hit)
	{
		NineTailedFoxAnnouncer.singleton.scpListTimer = 0f;
		if (!scp.IsSCP(false))
		{
			return;
		}
		string announcement = hit.CassieDeathAnnouncement.Announcement;
		if (string.IsNullOrEmpty(announcement))
		{
			return;
		}
		foreach (NineTailedFoxAnnouncer.ScpDeath scpDeath in NineTailedFoxAnnouncer.scpDeaths)
		{
			if (!(scpDeath.announcement != announcement))
			{
				scpDeath.scpSubjects.Add(scp.GetRoleId());
				return;
			}
		}
		NineTailedFoxAnnouncer.scpDeaths.Add(new NineTailedFoxAnnouncer.ScpDeath
		{
			scpSubjects = new List<RoleTypeId>(new RoleTypeId[] { scp.GetRoleId() }),
			announcement = announcement,
			subtitleParts = hit.CassieDeathAnnouncement.SubtitleParts
		});
	}

	public float CalculateDuration(string tts, bool rawNumber = false, float speed = 1f)
	{
		float num = 0f;
		string[] array = tts.Split(' ', StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			int j;
			int num2;
			if (NineTailedFoxAnnouncer.VoiceLine.IsJam(text, out j, out num2))
			{
				num += 0.13f * (float)num2;
			}
			else
			{
				if (NineTailedFoxAnnouncer.VoiceLine.IsRegular(text))
				{
					bool flag = false;
					int num3 = i + 1;
					while (num3 < array.Length && !NineTailedFoxAnnouncer.VoiceLine.IsRegular(array[num3]))
					{
						float num4;
						if (NineTailedFoxAnnouncer.VoiceLine.IsYield(array[num3], out num4))
						{
							num += num4;
							flag = true;
							break;
						}
						num3++;
					}
					if (flag)
					{
						goto IL_0165;
					}
				}
				float num5;
				int num6;
				if (NineTailedFoxAnnouncer.VoiceLine.IsPitch(text, out num5))
				{
					speed = num5;
				}
				else if (int.TryParse(text, out num6) && !rawNumber)
				{
					num += this.CalculateDuration(NineTailedFoxAnnouncer.ConvertNumber(num6), true, speed);
				}
				else
				{
					bool flag2 = false;
					foreach (NineTailedFoxAnnouncer.VoiceLine voiceLine in this.voiceLines)
					{
						if (string.Equals(text, voiceLine.apiName, StringComparison.OrdinalIgnoreCase))
						{
							flag2 = true;
							num += voiceLine.length / speed;
						}
					}
					if (!flag2 && text.Length > 3)
					{
						for (byte b = 1; b <= 3; b += 1)
						{
							foreach (NineTailedFoxAnnouncer.VoiceLine voiceLine2 in this.voiceLines)
							{
								if (string.Equals(text.Remove(text.Length - (int)b), voiceLine2.apiName, StringComparison.OrdinalIgnoreCase))
								{
									num += voiceLine2.length / speed;
								}
							}
						}
					}
				}
			}
			IL_0165:;
		}
		return num;
	}

	public void ServerOnlyAddGlitchyPhrase(string tts, float glitchChance, float jamChance)
	{
		string[] array = tts.Split(' ', StringSplitOptions.None);
		this.newWords.Clear();
		this.newWords.EnsureCapacity(array.Length);
		for (int i = 0; i < array.Length; i++)
		{
			this.newWords.Add(array[i]);
			if (i < array.Length - 1)
			{
				if (global::UnityEngine.Random.value < glitchChance)
				{
					this.newWords.Add(".G" + global::UnityEngine.Random.Range(1, 7).ToString());
				}
				if (global::UnityEngine.Random.value < jamChance)
				{
					this.newWords.Add("JAM_" + global::UnityEngine.Random.Range(0, 70).ToString("000") + "_" + global::UnityEngine.Random.Range(2, 6).ToString());
				}
			}
		}
		tts = "";
		foreach (string text in this.newWords)
		{
			tts = tts + text + " ";
		}
		RespawnEffectsController.PlayCassieAnnouncement(tts, false, true, false, "");
	}

	public void ClearQueue()
	{
		this.queue.Clear();
		this.backgroundSource.Stop();
		this.speakerSource.Stop();
	}

	public void AddPhraseToQueue(string tts, bool generateNoise, bool rawNumber = false, bool makeHold = false, bool customAnnouncement = false, string customSubtitles = "")
	{
		string[] array = tts.Split(' ', StringSplitOptions.None);
		if (!rawNumber)
		{
			float num = this.CalculateDuration(tts, false, 1f);
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
				this.queue.Add(new NineTailedFoxAnnouncer.VoiceLine
				{
					apiName = "BG_BACKGROUND",
					clip = this.backgroundLines[num2],
					length = 2.5f
				});
			}
		}
		float num3 = 1f;
		foreach (string text in array)
		{
			float num4;
			if (!NineTailedFoxAnnouncer.VoiceLine.IsRegular(text))
			{
				this.queue.Add(new NineTailedFoxAnnouncer.VoiceLine
				{
					apiName = text.ToUpper()
				});
			}
			else if (!rawNumber && float.TryParse(text, out num4))
			{
				this.AddPhraseToQueue(NineTailedFoxAnnouncer.ConvertNumber((int)num4), false, true, false, false, "");
			}
			else
			{
				bool flag = false;
				foreach (NineTailedFoxAnnouncer.VoiceLine voiceLine in this.voiceLines)
				{
					if (string.Equals(text, voiceLine.apiName, StringComparison.OrdinalIgnoreCase))
					{
						this.queue.Add(new NineTailedFoxAnnouncer.VoiceLine
						{
							apiName = voiceLine.apiName,
							clip = voiceLine.clip,
							length = voiceLine.length / num3
						});
						flag = true;
					}
				}
				if (!flag)
				{
					NineTailedFoxAnnouncer.VoiceLine voiceLine2 = null;
					if (text.Length > 3)
					{
						for (byte b = 1; b <= 4; b += 1)
						{
							if (text.Length > (int)b)
							{
								foreach (NineTailedFoxAnnouncer.VoiceLine voiceLine3 in this.voiceLines)
								{
									if ((string.Equals(text.Remove(text.Length - (int)b), voiceLine3.apiName, StringComparison.OrdinalIgnoreCase) || (text.EndsWith("ING", StringComparison.OrdinalIgnoreCase) && string.Equals(text.Remove(text.Length - (int)b) + "E", voiceLine3.apiName, StringComparison.OrdinalIgnoreCase))) && voiceLine2 == null)
									{
										voiceLine2 = new NineTailedFoxAnnouncer.VoiceLine
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
						AudioClip audioClip;
						if (text.EndsWith("TED", StringComparison.OrdinalIgnoreCase) || text.EndsWith("DED", StringComparison.OrdinalIgnoreCase))
						{
							audioClip = this.suffixPastException;
						}
						else if (text.EndsWith("D", StringComparison.OrdinalIgnoreCase))
						{
							audioClip = this.suffixPastStandard;
						}
						else if (text.EndsWith("ING", StringComparison.OrdinalIgnoreCase))
						{
							audioClip = this.suffixContinuous;
						}
						else if (voiceLine2.apiName.EndsWith("S") || voiceLine2.apiName.EndsWith("SH") || voiceLine2.apiName.EndsWith("CH") || voiceLine2.apiName.EndsWith("X") || voiceLine2.apiName.EndsWith("Z"))
						{
							audioClip = this.suffixPluralException;
						}
						else
						{
							audioClip = this.suffixPluralStandard;
						}
						this.queue.Add(new NineTailedFoxAnnouncer.VoiceLine
						{
							apiName = voiceLine2.apiName,
							clip = voiceLine2.clip,
							length = (voiceLine2.length - (text.EndsWith("ING", StringComparison.OrdinalIgnoreCase) ? 0.1f : 0.06f)) / num3
						});
						this.queue.Add(new NineTailedFoxAnnouncer.VoiceLine
						{
							apiName = "SUFFIX_" + audioClip.name,
							clip = audioClip,
							length = audioClip.length / num3
						});
					}
				}
			}
		}
		if (!rawNumber)
		{
			this.queue.Add(new NineTailedFoxAnnouncer.VoiceLine
			{
				apiName = "PITCH_1"
			});
			for (byte b2 = 0; b2 < (makeHold ? 3 : 1); b2 += 1)
			{
				this.queue.Add(new NineTailedFoxAnnouncer.VoiceLine
				{
					apiName = "END_OF_MESSAGE"
				});
			}
			return;
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
			}
			else
			{
				NineTailedFoxAnnouncer.VoiceLine line = this.queue[0];
				this.queue.RemoveAt(0);
				if (line.apiName == "END_OF_MESSAGE")
				{
					this.speakerSource.pitch = 1f;
					yield return new WaitForSeconds(4f);
				}
				else
				{
					bool flag = line.apiName.StartsWith("BG_") || line.apiName.StartsWith("BELL_");
					bool flag2 = line.apiName.StartsWith("SUFFIX_");
					float absoluteTimeAddition = 0f;
					float relativeTimeAddition = 0f;
					float num2;
					int num3;
					int num4;
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
							int num;
							for (int i = 0; i < jamSize; i = num + 1)
							{
								absoluteTimeAddition -= stepSize * 3f;
								this.speakerSource.time = timeToJam;
								yield return new WaitForSeconds(stepSize);
								num = i;
							}
							jammed = 0;
						}
						else
						{
							this.speakerSource.PlayOneShot(line.clip);
						}
					}
					else if (NineTailedFoxAnnouncer.VoiceLine.IsPitch(line.apiName, out num2))
					{
						speed = num2;
						this.speakerSource.pitch = speed;
					}
					else if (NineTailedFoxAnnouncer.VoiceLine.IsJam(line.apiName, out num3, out num4))
					{
						jammed = num3;
						jamSize = num4;
					}
					if (NineTailedFoxAnnouncer.VoiceLine.IsRegular(line.apiName))
					{
						float num5 = 0f;
						int num6 = 0;
						while (num6 < this.queue.Count && !NineTailedFoxAnnouncer.VoiceLine.IsRegular(this.queue[num6].apiName))
						{
							float num7;
							if (NineTailedFoxAnnouncer.VoiceLine.IsYield(this.queue[num6].apiName, out num7))
							{
								num5 = num7;
								break;
							}
							num6++;
						}
						if (num5 > 0f)
						{
							yield return new WaitForSeconds(num5);
						}
						else
						{
							float num8 = (line.length + relativeTimeAddition) / speed + absoluteTimeAddition;
							if (num8 > 0f)
							{
								yield return new WaitForSeconds(num8);
							}
						}
					}
					line = null;
				}
			}
		}
		yield break;
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
			NineTailedFoxAnnouncer.ScpDeath scpDeath = NineTailedFoxAnnouncer.scpDeaths[i];
			List<SubtitlePart> list = new List<SubtitlePart>(1);
			string text = "";
			for (int j = 0; j < scpDeath.scpSubjects.Count; j++)
			{
				string text2;
				string text3;
				NineTailedFoxAnnouncer.ConvertSCP(scpDeath.scpSubjects[j], out text2, out text3);
				if (j == 0)
				{
					text = text + "SCP " + text3;
				}
				else
				{
					text = text + ". SCP " + text3;
				}
				list.Add(new SubtitlePart(SubtitleType.SCP, new string[] { text2 }));
			}
			text += scpDeath.announcement;
			if (scpDeath.subtitleParts != null)
			{
				list.AddRange(scpDeath.subtitleParts);
			}
			float num = (AlphaWarheadController.Detonated ? 3.5f : 1f);
			this.ServerOnlyAddGlitchyPhrase(text, global::UnityEngine.Random.Range(0.1f, 0.14f) * num, global::UnityEngine.Random.Range(0.07f, 0.08f) * num);
			new SubtitleMessage(list.ToArray()).SendToAuthenticated(0);
		}
		this.scpListTimer = 0f;
		NineTailedFoxAnnouncer.scpDeaths.Clear();
	}

	public static void ConvertSCP(RoleTypeId role, out string withoutSpace, out string withSpace)
	{
		PlayerRoleBase playerRoleBase;
		if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out playerRoleBase))
		{
			withoutSpace = string.Empty;
			withSpace = string.Empty;
			return;
		}
		NineTailedFoxAnnouncer.ConvertSCP(playerRoleBase.RoleName, out withoutSpace, out withSpace);
	}

	public static void ConvertSCP(string roleName, out string withoutSpace, out string withSpace)
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		string[] array = roleName.Split('-', StringSplitOptions.None);
		if (array.Length < 2)
		{
			Debug.LogError("Cassie role cannot be split by '-'. Possibly missing translation.");
			withoutSpace = "404";
			withSpace = "4 0 4";
			return;
		}
		withoutSpace = array[1];
		foreach (char c in withoutSpace)
		{
			stringBuilder.Append(c);
			stringBuilder.Append(" ");
		}
		withSpace = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}

	public static string ConvertTeam(Team team, string unitName)
	{
		string text = "CONTAINMENTUNIT UNKNOWN";
		switch (team)
		{
		case Team.FoundationForces:
		{
			UnitNamingRule unitNamingRule;
			if (!NamingRulesManager.TryGetNamingRule(team, out unitNamingRule))
			{
				return text;
			}
			string text2 = unitNamingRule.TranslateToCassie(unitName);
			return "CONTAINMENTUNIT " + text2;
		}
		case Team.ChaosInsurgency:
			return "BY CHAOSINSURGENCY";
		case Team.Scientists:
			return "BY SCIENCE PERSONNEL";
		case Team.ClassD:
			return "BY CLASSD PERSONNEL";
		default:
			return text;
		}
	}

	public NineTailedFoxAnnouncer.VoiceLine[] voiceLines;

	public AudioClip[] backgroundLines;

	public AudioClip suffixPluralStandard;

	public AudioClip suffixPluralException;

	public AudioClip suffixPastStandard;

	public AudioClip suffixPastException;

	public AudioClip suffixContinuous;

	public readonly List<NineTailedFoxAnnouncer.VoiceLine> queue = new List<NineTailedFoxAnnouncer.VoiceLine>();

	private List<string> newWords = new List<string>();

	private readonly List<NineTailedFoxAnnouncer.VoiceLine> newLines = new List<NineTailedFoxAnnouncer.VoiceLine>();

	private static readonly List<NineTailedFoxAnnouncer.ScpDeath> scpDeaths = new List<NineTailedFoxAnnouncer.ScpDeath>();

	public AudioSource speakerSource;

	public AudioSource backgroundSource;

	private readonly Regex UniqueKeyRegex = new Regex("(jam_\\d{1,3}_\\d{1,3})|(pitch_[\\d\\.]{1,4})|(\\.g\\d{1,3})|(bell_start)|(bell_end)", RegexOptions.IgnoreCase);

	private readonly Regex WhiteSpaceRegex = new Regex("\\s+");

	public static NineTailedFoxAnnouncer singleton;

	private float scpListTimer;

	[Serializable]
	public class VoiceLine
	{
		public static bool IsYield(string s, out float value)
		{
			if (!s.StartsWith("YIELD_", StringComparison.OrdinalIgnoreCase) && !s.StartsWith("YD_", StringComparison.OrdinalIgnoreCase))
			{
				value = 0f;
				return false;
			}
			string[] array = s.Split('_', StringSplitOptions.None);
			if (array.Length == 2)
			{
				return CustomParser.TryParseFloat(array[1], out value) != CustomParser.ParseResult.Failed;
			}
			value = 0f;
			return false;
		}

		public static bool IsJam(string s, out int percent, out int amount)
		{
			bool flag = s.StartsWith("JAM_", StringComparison.OrdinalIgnoreCase);
			percent = 0;
			amount = 0;
			if (!flag)
			{
				return false;
			}
			string[] array = s.Split('_', StringSplitOptions.None);
			if (array.Length != 3)
			{
				return false;
			}
			int num;
			if (CustomParser.TryParseInt(array[1], out num) == CustomParser.ParseResult.Failed)
			{
				return false;
			}
			percent = num;
			if (CustomParser.TryParseInt(array[2], out num) == CustomParser.ParseResult.Failed)
			{
				return false;
			}
			amount = num;
			return true;
		}

		public static bool IsPitch(string s, out float value)
		{
			bool flag = s.StartsWith("PITCH_", StringComparison.OrdinalIgnoreCase) || s.StartsWith("PI_", StringComparison.OrdinalIgnoreCase);
			value = 1f;
			if (!flag)
			{
				return false;
			}
			string[] array = s.Split('_', StringSplitOptions.None);
			if (array.Length < 2)
			{
				return false;
			}
			float num;
			if (CustomParser.TryParseFloat(array[1], out num) == CustomParser.ParseResult.Failed)
			{
				return false;
			}
			if (num > 0f)
			{
				value = num;
			}
			return true;
		}

		public static bool IsRegular(string s)
		{
			float num;
			int num2;
			int num3;
			return !NineTailedFoxAnnouncer.VoiceLine.IsYield(s, out num) && !NineTailedFoxAnnouncer.VoiceLine.IsJam(s, out num2, out num3) && !NineTailedFoxAnnouncer.VoiceLine.IsPitch(s, out num);
		}

		public string GetName()
		{
			return this.apiName;
		}

		public string apiName;

		public AudioClip clip;

		public float length;

		public string collection;
	}

	[Serializable]
	public struct ScpDeath : IEquatable<NineTailedFoxAnnouncer.ScpDeath>
	{
		public bool Equals(NineTailedFoxAnnouncer.ScpDeath other)
		{
			return this.scpSubjects == other.scpSubjects && string.Equals(this.announcement, other.announcement);
		}

		public override bool Equals(object obj)
		{
			if (obj is NineTailedFoxAnnouncer.ScpDeath)
			{
				NineTailedFoxAnnouncer.ScpDeath scpDeath = (NineTailedFoxAnnouncer.ScpDeath)obj;
				return this.Equals(scpDeath);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((this.scpSubjects != null) ? this.scpSubjects.GetHashCode() : 0) * 397) ^ ((this.announcement != null) ? this.announcement.GetHashCode() : 0);
		}

		public static bool operator ==(NineTailedFoxAnnouncer.ScpDeath left, NineTailedFoxAnnouncer.ScpDeath right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(NineTailedFoxAnnouncer.ScpDeath left, NineTailedFoxAnnouncer.ScpDeath right)
		{
			return !left.Equals(right);
		}

		public List<RoleTypeId> scpSubjects;

		public string announcement;

		public SubtitlePart[] subtitleParts;
	}

	private class ItemEqualityComparer : IEqualityComparer<NineTailedFoxAnnouncer.VoiceLine>
	{
		public bool Equals(NineTailedFoxAnnouncer.VoiceLine x, NineTailedFoxAnnouncer.VoiceLine y)
		{
			return x != null && x.clip != null && x.clip == y.clip;
		}

		public int GetHashCode(NineTailedFoxAnnouncer.VoiceLine obj)
		{
			return obj.clip.GetHashCode();
		}
	}
}

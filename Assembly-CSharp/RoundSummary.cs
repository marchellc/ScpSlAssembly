using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CentralAuth;
using GameCore;
using InventorySystem.Disarming;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MEC;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using PlayerStatsSystem;
using RoundRestarting;
using UnityEngine;
using Utils.NonAllocLINQ;

public class RoundSummary : NetworkBehaviour
{
	public static bool SummaryActive
	{
		get
		{
			return RoundSummary._singletonSet && RoundSummary.singleton._summaryActive;
		}
	}

	public int ExtraTargets
	{
		get
		{
			return this._extraTargets;
		}
		set
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.Network_extraTargets = value;
		}
	}

	public static int Kills { get; private set; }

	public static int EscapedClassD { get; private set; }

	public static int EscapedScientists { get; private set; }

	public static int SurvivingSCPs { get; private set; }

	public static int KilledBySCPs { get; private set; }

	public static int ChangedIntoZombies { get; private set; }

	public static event RoundSummary.RoundEnded OnRoundEnded;

	private void Start()
	{
		RoundSummary.singleton = this;
		RoundSummary._singletonSet = true;
		if (!NetworkServer.active)
		{
			return;
		}
		RoundSummary.roundTime = 0;
		this.KeepRoundOnOne = !ConfigFile.ServerConfig.GetBool("end_round_on_one_player", false);
		Timing.RunCoroutine(this._ProcessServerSideCode(), Segment.FixedUpdate);
		RoundSummary.KilledBySCPs = 0;
		RoundSummary.EscapedClassD = 0;
		RoundSummary.EscapedScientists = 0;
		RoundSummary.ChangedIntoZombies = 0;
		RoundSummary.Kills = 0;
		PlayerRoleManager.OnServerRoleSet += this.OnServerRoleSet;
		PlayerStats.OnAnyPlayerDied += this.OnAnyPlayerDied;
	}

	private void OnDestroy()
	{
		RoundSummary._singletonSet = false;
		PlayerRoleManager.OnServerRoleSet -= this.OnServerRoleSet;
		PlayerStats.OnAnyPlayerDied -= this.OnAnyPlayerDied;
	}

	private void OnAnyPlayerDied(ReferenceHub ply, DamageHandlerBase handler)
	{
		RoundSummary.Kills++;
		UniversalDamageHandler universalDamageHandler = handler as UniversalDamageHandler;
		if (universalDamageHandler != null)
		{
			if (universalDamageHandler.TranslationId != DeathTranslations.PocketDecay.Id)
			{
				return;
			}
		}
		else
		{
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(attackerDamageHandler.Attacker.Role, out playerRoleBase))
			{
				return;
			}
			if (playerRoleBase.Team != Team.SCPs)
			{
				return;
			}
		}
		RoundSummary.KilledBySCPs++;
	}

	private void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
	{
		switch (reason)
		{
		case RoleChangeReason.RoundStart:
		case RoleChangeReason.LateJoin:
			this.ModifySpawnedTeam(userHub.GetTeam(), -1);
			this.ModifySpawnedTeam(newRole.GetTeam(), 1);
			return;
		case RoleChangeReason.Respawn:
		case RoleChangeReason.Died:
			break;
		case RoleChangeReason.Escaped:
			if (!userHub.inventory.IsDisarmed())
			{
				Team team = newRole.GetTeam();
				if (team == Team.FoundationForces)
				{
					RoundSummary.EscapedScientists++;
					return;
				}
				if (team != Team.ChaosInsurgency)
				{
					return;
				}
				RoundSummary.EscapedClassD++;
				return;
			}
			break;
		case RoleChangeReason.Revived:
			if (newRole != RoleTypeId.Flamingo)
			{
				RoundSummary.ChangedIntoZombies++;
				this.classlistStart.zombies = this.classlistStart.zombies + 1;
			}
			break;
		default:
			return;
		}
	}

	private void ModifySpawnedTeam(Team t, int modifyAmount)
	{
		switch (t)
		{
		case Team.SCPs:
			this.classlistStart.scps_except_zombies = this.classlistStart.scps_except_zombies + modifyAmount;
			return;
		case Team.FoundationForces:
			this.classlistStart.mtf_and_guards = this.classlistStart.mtf_and_guards + modifyAmount;
			return;
		case Team.ChaosInsurgency:
			this.classlistStart.chaos_insurgents = this.classlistStart.chaos_insurgents + modifyAmount;
			return;
		case Team.Scientists:
			this.classlistStart.scientists = this.classlistStart.scientists + modifyAmount;
			return;
		case Team.ClassD:
			this.classlistStart.class_ds = this.classlistStart.class_ds + modifyAmount;
			return;
		default:
			return;
		}
	}

	public void ForceEnd()
	{
		this._roundEnded = true;
	}

	public int CountRole(RoleTypeId role)
	{
		return ReferenceHub.AllHubs.Count((ReferenceHub x) => x.GetRoleId() == role);
	}

	public int CountTeam(Team team)
	{
		return ReferenceHub.AllHubs.Count((ReferenceHub x) => x.GetTeam() == team);
	}

	private IEnumerator<float> _ProcessServerSideCode()
	{
		float time = Time.unscaledTime;
		while (this != null)
		{
			yield return Timing.WaitForSeconds(2.5f);
			if (!this._roundEnded)
			{
				if (RoundSummary.RoundLock)
				{
					continue;
				}
				if (this.KeepRoundOnOne)
				{
					if (ReferenceHub.AllHubs.Count((ReferenceHub x) => x.authManager.InstanceMode != ClientInstanceMode.DedicatedServer) < 2)
					{
						continue;
					}
				}
				if (!RoundSummary.RoundInProgress())
				{
					continue;
				}
			}
			if (this._roundEnded || Time.unscaledTime - time >= 15f)
			{
				RoundSummary.SumInfo_ClassList newList = default(RoundSummary.SumInfo_ClassList);
				foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
				{
					switch (referenceHub.GetTeam())
					{
					case Team.SCPs:
						if (referenceHub.GetRoleId() == RoleTypeId.Scp0492)
						{
							newList.zombies++;
						}
						else
						{
							newList.scps_except_zombies++;
						}
						break;
					case Team.FoundationForces:
						newList.mtf_and_guards++;
						break;
					case Team.ChaosInsurgency:
						newList.chaos_insurgents++;
						break;
					case Team.Scientists:
						newList.scientists++;
						break;
					case Team.ClassD:
						newList.class_ds++;
						break;
					case Team.Flamingos:
						newList.flamingos++;
						break;
					}
				}
				yield return float.NegativeInfinity;
				newList.warhead_kills = (AlphaWarheadController.Detonated ? AlphaWarheadController.Singleton.WarheadKills : (-1));
				yield return float.NegativeInfinity;
				int num = newList.mtf_and_guards + newList.scientists;
				int num2 = newList.chaos_insurgents + newList.class_ds;
				int num3 = newList.scps_except_zombies + newList.zombies;
				int num4 = newList.class_ds + RoundSummary.EscapedClassD;
				int num5 = newList.scientists + RoundSummary.EscapedScientists;
				int flamingos = newList.flamingos;
				RoundSummary.SurvivingSCPs = newList.scps_except_zombies;
				float num6 = (float)((this.classlistStart.class_ds == 0) ? 0 : (num4 / this.classlistStart.class_ds));
				float num7 = (float)((this.classlistStart.scientists == 0) ? 1 : (num5 / this.classlistStart.scientists));
				int num8 = 0;
				if (num > 0)
				{
					num8++;
				}
				if (num2 > 0)
				{
					num8++;
				}
				if (num3 > 0)
				{
					num8++;
				}
				if (flamingos > 0)
				{
					num8++;
				}
				if (this.ExtraTargets > 0)
				{
					num8++;
				}
				this._roundEnded = num8 <= 1;
				if (this._roundEnded)
				{
					bool flag = num > 0;
					bool flag2 = num2 > 0;
					bool flag3 = num3 > 0;
					bool flag4 = flamingos > 0;
					RoundSummary.LeadingTeam leadingTeam = RoundSummary.LeadingTeam.Draw;
					if (flag)
					{
						leadingTeam = ((RoundSummary.EscapedScientists >= RoundSummary.EscapedClassD) ? RoundSummary.LeadingTeam.FacilityForces : RoundSummary.LeadingTeam.Draw);
					}
					else if (flag3 || (flag3 && flag2))
					{
						leadingTeam = ((RoundSummary.EscapedClassD > RoundSummary.SurvivingSCPs) ? RoundSummary.LeadingTeam.ChaosInsurgency : ((RoundSummary.SurvivingSCPs > RoundSummary.EscapedScientists) ? RoundSummary.LeadingTeam.Anomalies : RoundSummary.LeadingTeam.Draw));
					}
					else if (flag2)
					{
						leadingTeam = ((RoundSummary.EscapedClassD >= RoundSummary.EscapedScientists) ? RoundSummary.LeadingTeam.ChaosInsurgency : RoundSummary.LeadingTeam.Draw);
					}
					else if (flag4)
					{
						leadingTeam = RoundSummary.LeadingTeam.Flamingos;
					}
					RoundEndingEventArgs roundEndingEventArgs = new RoundEndingEventArgs(leadingTeam);
					ServerEvents.OnRoundEnding(roundEndingEventArgs);
					if (roundEndingEventArgs.IsAllowed)
					{
						leadingTeam = roundEndingEventArgs.LeadingTeam;
						RoundSummary.RoundEnded onRoundEnded = RoundSummary.OnRoundEnded;
						if (onRoundEnded != null)
						{
							onRoundEnded(leadingTeam, newList);
						}
						FriendlyFireConfig.PauseDetector = true;
						string text = string.Concat(new string[]
						{
							"Round finished! Anomalies: ",
							num3.ToString(),
							" | Chaos: ",
							num2.ToString(),
							" | Facility Forces: ",
							num.ToString(),
							" | D escaped percentage: ",
							num6.ToString(),
							" | S escaped percentage: ",
							num7.ToString(),
							"."
						});
						global::GameCore.Console.AddLog(text, Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
						ServerLogs.AddLog(ServerLogs.Modules.GameLogic, text, ServerLogs.ServerLogType.GameEvent, false);
						yield return Timing.WaitForSeconds(1.5f);
						RoundEndedEventArgs roundEndedEventArgs = new RoundEndedEventArgs(leadingTeam);
						ServerEvents.OnRoundEnded(roundEndedEventArgs);
						bool showSummary = roundEndedEventArgs.ShowSummary;
						int num9 = Mathf.Clamp(ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000);
						if (this != null && showSummary)
						{
							this.RpcShowRoundSummary(this.classlistStart, newList, leadingTeam, RoundSummary.EscapedClassD, RoundSummary.EscapedScientists, RoundSummary.KilledBySCPs, num9, (int)RoundStart.RoundLength.TotalSeconds);
						}
						yield return Timing.WaitForSeconds((float)(num9 - 1));
						this.RpcDimScreen();
						yield return Timing.WaitForSeconds(1f);
						RoundRestart.InitiateRoundRestart();
						yield break;
					}
				}
			}
		}
		yield break;
	}

	[ClientRpc]
	private void RpcShowRoundSummary(RoundSummary.SumInfo_ClassList listStart, RoundSummary.SumInfo_ClassList listFinish, RoundSummary.LeadingTeam leadingTeam, int eDS, int eSc, int scpKills, int roundCd, int seconds)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		global::Mirror.GeneratedNetworkCode._Write_RoundSummary/SumInfo_ClassList(networkWriterPooled, listStart);
		global::Mirror.GeneratedNetworkCode._Write_RoundSummary/SumInfo_ClassList(networkWriterPooled, listFinish);
		global::Mirror.GeneratedNetworkCode._Write_RoundSummary/LeadingTeam(networkWriterPooled, leadingTeam);
		networkWriterPooled.WriteInt(eDS);
		networkWriterPooled.WriteInt(eSc);
		networkWriterPooled.WriteInt(scpKills);
		networkWriterPooled.WriteInt(roundCd);
		networkWriterPooled.WriteInt(seconds);
		this.SendRPCInternal("System.Void RoundSummary::RpcShowRoundSummary(RoundSummary/SumInfo_ClassList,RoundSummary/SumInfo_ClassList,RoundSummary/LeadingTeam,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)", -173396156, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	[ClientRpc]
	private void RpcDimScreen()
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void RoundSummary::RpcDimScreen()", -1745793588, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	public static bool RoundInProgress()
	{
		ReferenceHub referenceHub;
		return RoundSummary._singletonSet && ReferenceHub.TryGetHostHub(out referenceHub) && referenceHub.characterClassManager.RoundStarted;
	}

	public override bool Weaved()
	{
		return true;
	}

	public int Network_extraTargets
	{
		get
		{
			return this._extraTargets;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<int>(value, ref this._extraTargets, 1UL, null);
		}
	}

	protected void UserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32(RoundSummary.SumInfo_ClassList listStart, RoundSummary.SumInfo_ClassList listFinish, RoundSummary.LeadingTeam leadingTeam, int eDS, int eSc, int scpKills, int roundCd, int seconds)
	{
		this._summaryActive = true;
	}

	protected static void InvokeUserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcShowRoundSummary called on server.");
			return;
		}
		((RoundSummary)obj).UserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32(global::Mirror.GeneratedNetworkCode._Read_RoundSummary/SumInfo_ClassList(reader), global::Mirror.GeneratedNetworkCode._Read_RoundSummary/SumInfo_ClassList(reader), global::Mirror.GeneratedNetworkCode._Read_RoundSummary/LeadingTeam(reader), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
	}

	protected void UserCode_RpcDimScreen()
	{
	}

	protected static void InvokeUserCode_RpcDimScreen(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDimScreen called on server.");
			return;
		}
		((RoundSummary)obj).UserCode_RpcDimScreen();
	}

	static RoundSummary()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(RoundSummary), "System.Void RoundSummary::RpcShowRoundSummary(RoundSummary/SumInfo_ClassList,RoundSummary/SumInfo_ClassList,RoundSummary/LeadingTeam,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)", new RemoteCallDelegate(RoundSummary.InvokeUserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32));
		RemoteProcedureCalls.RegisterRpc(typeof(RoundSummary), "System.Void RoundSummary::RpcDimScreen()", new RemoteCallDelegate(RoundSummary.InvokeUserCode_RpcDimScreen));
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteInt(this._extraTargets);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteInt(this._extraTargets);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<int>(ref this._extraTargets, null, reader.ReadInt());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<int>(ref this._extraTargets, null, reader.ReadInt());
		}
	}

	[SyncVar]
	private int _extraTargets;

	private bool _roundEnded;

	private bool _summaryActive;

	public bool KeepRoundOnOne;

	public RoundSummary.SumInfo_ClassList classlistStart;

	public GameObject ui_root;

	public static bool RoundLock;

	public static RoundSummary singleton;

	private static bool _singletonSet;

	public static int roundTime;

	public delegate void RoundEnded(RoundSummary.LeadingTeam leadingTeam, RoundSummary.SumInfo_ClassList sumInfo);

	public enum LeadingTeam : byte
	{
		FacilityForces,
		ChaosInsurgency,
		Anomalies,
		Flamingos,
		Draw
	}

	[Serializable]
	public struct SumInfo_ClassList : IEquatable<RoundSummary.SumInfo_ClassList>
	{
		public bool Equals(RoundSummary.SumInfo_ClassList other)
		{
			return this.class_ds == other.class_ds && this.scientists == other.scientists && this.chaos_insurgents == other.chaos_insurgents && this.mtf_and_guards == other.mtf_and_guards && this.scps_except_zombies == other.scps_except_zombies && this.zombies == other.zombies && this.warhead_kills == other.warhead_kills && this.flamingos == other.flamingos;
		}

		public override bool Equals(object obj)
		{
			if (obj is RoundSummary.SumInfo_ClassList)
			{
				RoundSummary.SumInfo_ClassList sumInfo_ClassList = (RoundSummary.SumInfo_ClassList)obj;
				return this.Equals(sumInfo_ClassList);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((((((((this.class_ds * 397) ^ this.scientists) * 397) ^ this.chaos_insurgents) * 397) ^ this.mtf_and_guards) * 397) ^ this.scps_except_zombies) * 397) ^ this.zombies) * 397) ^ this.warhead_kills) * 397) ^ this.flamingos;
		}

		public static bool operator ==(RoundSummary.SumInfo_ClassList left, RoundSummary.SumInfo_ClassList right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(RoundSummary.SumInfo_ClassList left, RoundSummary.SumInfo_ClassList right)
		{
			return !left.Equals(right);
		}

		public int class_ds;

		public int scientists;

		public int chaos_insurgents;

		public int mtf_and_guards;

		public int scps_except_zombies;

		public int zombies;

		public int warhead_kills;

		public int flamingos;
	}
}

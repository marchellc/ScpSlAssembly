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
	public delegate void RoundEnded(LeadingTeam leadingTeam, SumInfo_ClassList sumInfo);

	public enum LeadingTeam : byte
	{
		FacilityForces,
		ChaosInsurgency,
		Anomalies,
		Flamingos,
		Draw
	}

	[Serializable]
	public struct SumInfo_ClassList : IEquatable<SumInfo_ClassList>
	{
		public int class_ds;

		public int scientists;

		public int chaos_insurgents;

		public int mtf_and_guards;

		public int scps_except_zombies;

		public int zombies;

		public int warhead_kills;

		public int flamingos;

		public bool Equals(SumInfo_ClassList other)
		{
			if (this.class_ds == other.class_ds && this.scientists == other.scientists && this.chaos_insurgents == other.chaos_insurgents && this.mtf_and_guards == other.mtf_and_guards && this.scps_except_zombies == other.scps_except_zombies && this.zombies == other.zombies && this.warhead_kills == other.warhead_kills)
			{
				return this.flamingos == other.flamingos;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is SumInfo_ClassList other)
			{
				return this.Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((((((((((this.class_ds * 397) ^ this.scientists) * 397) ^ this.chaos_insurgents) * 397) ^ this.mtf_and_guards) * 397) ^ this.scps_except_zombies) * 397) ^ this.zombies) * 397) ^ this.warhead_kills) * 397) ^ this.flamingos;
		}

		public static bool operator ==(SumInfo_ClassList left, SumInfo_ClassList right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SumInfo_ClassList left, SumInfo_ClassList right)
		{
			return !left.Equals(right);
		}
	}

	[SyncVar]
	private int _extraTargets;

	private bool _summaryActive;

	private CoroutineHandle _roundEndCoroutine;

	public bool KeepRoundOnOne;

	public bool IsRoundEnded;

	public SumInfo_ClassList classlistStart;

	public GameObject ui_root;

	public static bool RoundLock;

	public static RoundSummary singleton;

	private static bool _singletonSet;

	public static int roundTime;

	public static bool SummaryActive
	{
		get
		{
			if (RoundSummary._singletonSet)
			{
				return RoundSummary.singleton._summaryActive;
			}
			return false;
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
			if (NetworkServer.active)
			{
				this.Network_extraTargets = value;
			}
		}
	}

	public static int Kills { get; private set; }

	public static int EscapedClassD { get; private set; }

	public static int EscapedScientists { get; private set; }

	public static int SurvivingSCPs { get; private set; }

	public static int KilledBySCPs { get; private set; }

	public static int ChangedIntoZombies { get; private set; }

	public int Network_extraTargets
	{
		get
		{
			return this._extraTargets;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._extraTargets, 1uL, null);
		}
	}

	public static event RoundEnded OnRoundEnded;

	private void Start()
	{
		RoundSummary.singleton = this;
		RoundSummary._singletonSet = true;
		this.IsRoundEnded = false;
		if (NetworkServer.active)
		{
			RoundSummary.roundTime = 0;
			this.KeepRoundOnOne = !ConfigFile.ServerConfig.GetBool("end_round_on_one_player");
			Timing.RunCoroutine(this._ProcessServerSideCode().CancelWith(base.gameObject), Segment.FixedUpdate);
			RoundSummary.KilledBySCPs = 0;
			RoundSummary.EscapedClassD = 0;
			RoundSummary.EscapedScientists = 0;
			RoundSummary.ChangedIntoZombies = 0;
			RoundSummary.Kills = 0;
			PlayerRoleManager.OnServerRoleSet += OnServerRoleSet;
			PlayerStats.OnAnyPlayerDied += OnAnyPlayerDied;
		}
	}

	private void OnDestroy()
	{
		RoundSummary._singletonSet = false;
		PlayerRoleManager.OnServerRoleSet -= OnServerRoleSet;
		PlayerStats.OnAnyPlayerDied -= OnAnyPlayerDied;
	}

	private void OnAnyPlayerDied(ReferenceHub ply, DamageHandlerBase handler)
	{
		RoundSummary.Kills++;
		PlayerRoleBase result;
		if (handler is UniversalDamageHandler universalDamageHandler)
		{
			if (universalDamageHandler.TranslationId != DeathTranslations.PocketDecay.Id)
			{
				return;
			}
		}
		else if (!(handler is AttackerDamageHandler attackerDamageHandler) || !PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(attackerDamageHandler.Attacker.Role, out result) || result.Team != Team.SCPs)
		{
			return;
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
			break;
		case RoleChangeReason.Escaped:
			if (!userHub.inventory.IsDisarmed())
			{
				switch (newRole.GetTeam())
				{
				case Team.FoundationForces:
					RoundSummary.EscapedScientists++;
					break;
				case Team.ChaosInsurgency:
					RoundSummary.EscapedClassD++;
					break;
				}
			}
			break;
		case RoleChangeReason.Revived:
			if (newRole != RoleTypeId.Flamingo)
			{
				RoundSummary.ChangedIntoZombies++;
				this.classlistStart.zombies++;
			}
			break;
		case RoleChangeReason.Respawn:
		case RoleChangeReason.Died:
			break;
		}
	}

	private void ModifySpawnedTeam(Team t, int modifyAmount)
	{
		switch (t)
		{
		case Team.ChaosInsurgency:
			this.classlistStart.chaos_insurgents += modifyAmount;
			break;
		case Team.ClassD:
			this.classlistStart.class_ds += modifyAmount;
			break;
		case Team.FoundationForces:
			this.classlistStart.mtf_and_guards += modifyAmount;
			break;
		case Team.Scientists:
			this.classlistStart.scientists += modifyAmount;
			break;
		case Team.SCPs:
			this.classlistStart.scps_except_zombies += modifyAmount;
			break;
		}
	}

	public void ForceEnd()
	{
		this.IsRoundEnded = true;
	}

	public void CancelRoundEnding()
	{
		if (NetworkServer.active && this.IsRoundEnded)
		{
			this.IsRoundEnded = false;
			Timing.KillCoroutines(this._roundEndCoroutine);
			this.RpcHideRoundSummary();
			this.RpcUndimScreen();
		}
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
			if (!this.IsRoundEnded && (RoundSummary.RoundLock || (this.KeepRoundOnOne && ReferenceHub.AllHubs.Count((ReferenceHub x) => x.authManager.InstanceMode != ClientInstanceMode.DedicatedServer) < 2) || !RoundSummary.RoundInProgress() || Time.unscaledTime - time < 15f))
			{
				continue;
			}
			SumInfo_ClassList newList = default(SumInfo_ClassList);
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				switch (allHub.GetTeam())
				{
				case Team.ClassD:
					newList.class_ds++;
					break;
				case Team.ChaosInsurgency:
					newList.chaos_insurgents++;
					break;
				case Team.FoundationForces:
					newList.mtf_and_guards++;
					break;
				case Team.Scientists:
					newList.scientists++;
					break;
				case Team.Flamingos:
					newList.flamingos++;
					break;
				case Team.SCPs:
					if (allHub.GetRoleId() == RoleTypeId.Scp0492)
					{
						newList.zombies++;
					}
					else
					{
						newList.scps_except_zombies++;
					}
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
			float num6 = ((this.classlistStart.class_ds != 0) ? (num4 / this.classlistStart.class_ds) : 0);
			float num7 = ((this.classlistStart.scientists == 0) ? 1 : (num5 / this.classlistStart.scientists));
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
			this.IsRoundEnded = num8 <= 1;
			RoundEndingConditionsCheckEventArgs e = new RoundEndingConditionsCheckEventArgs(this.IsRoundEnded);
			ServerEvents.OnRoundEndingConditionsCheck(e);
			this.IsRoundEnded = e.CanEnd;
			if (!this.IsRoundEnded)
			{
				continue;
			}
			bool num9 = num > 0;
			bool flag = num2 > 0;
			bool flag2 = num3 > 0;
			bool flag3 = flamingos > 0;
			LeadingTeam leadingTeam = LeadingTeam.Draw;
			if (num9)
			{
				leadingTeam = ((RoundSummary.EscapedScientists < RoundSummary.EscapedClassD) ? LeadingTeam.Draw : LeadingTeam.FacilityForces);
			}
			else if (flag2 || (flag2 && flag))
			{
				leadingTeam = ((RoundSummary.EscapedClassD > RoundSummary.SurvivingSCPs) ? LeadingTeam.ChaosInsurgency : ((RoundSummary.SurvivingSCPs > RoundSummary.EscapedScientists) ? LeadingTeam.Anomalies : LeadingTeam.Draw));
			}
			else if (flag)
			{
				leadingTeam = ((RoundSummary.EscapedClassD >= RoundSummary.EscapedScientists) ? LeadingTeam.ChaosInsurgency : LeadingTeam.Draw);
			}
			else if (flag3)
			{
				leadingTeam = LeadingTeam.Flamingos;
			}
			RoundEndingEventArgs e2 = new RoundEndingEventArgs(leadingTeam);
			ServerEvents.OnRoundEnding(e2);
			if (e2.IsAllowed)
			{
				leadingTeam = e2.LeadingTeam;
				RoundSummary.OnRoundEnded?.Invoke(leadingTeam, newList);
				FriendlyFireConfig.PauseDetector = true;
				string text = "Round finished! Anomalies: " + num3 + " | Chaos: " + num2 + " | Facility Forces: " + num + " | D escaped percentage: " + num6 + " | S escaped percentage: " + num7 + ".";
				GameCore.Console.AddLog(text, Color.gray);
				ServerLogs.AddLog(ServerLogs.Modules.GameLogic, text, ServerLogs.ServerLogType.GameEvent);
				yield return Timing.WaitForSeconds(1.5f);
				RoundEndedEventArgs e3 = new RoundEndedEventArgs(leadingTeam);
				ServerEvents.OnRoundEnded(e3);
				bool showSummary = e3.ShowSummary;
				int num10 = Mathf.Clamp(ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000);
				if (this != null && showSummary)
				{
					this.RpcShowRoundSummary(this.classlistStart, newList, leadingTeam, RoundSummary.EscapedClassD, RoundSummary.EscapedScientists, RoundSummary.KilledBySCPs, num10, (int)RoundStart.RoundLength.TotalSeconds);
				}
				this._roundEndCoroutine = Timing.RunCoroutine(this.InitiateRoundEnd(num10), Segment.FixedUpdate);
				yield return Timing.WaitUntilDone(this._roundEndCoroutine);
			}
		}
	}

	private IEnumerator<float> InitiateRoundEnd(int timeToRoundRestart)
	{
		yield return Timing.WaitForSeconds(timeToRoundRestart - 1);
		if (this.IsRoundEnded)
		{
			this.RpcDimScreen();
			yield return Timing.WaitForSeconds(1f);
			RoundRestart.InitiateRoundRestart();
		}
	}

	[ClientRpc]
	private void RpcShowRoundSummary(SumInfo_ClassList listStart, SumInfo_ClassList listFinish, LeadingTeam leadingTeam, int eDS, int eSc, int scpKills, int roundCd, int seconds)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_RoundSummary_002FSumInfo_ClassList(writer, listStart);
		GeneratedNetworkCode._Write_RoundSummary_002FSumInfo_ClassList(writer, listFinish);
		GeneratedNetworkCode._Write_RoundSummary_002FLeadingTeam(writer, leadingTeam);
		writer.WriteInt(eDS);
		writer.WriteInt(eSc);
		writer.WriteInt(scpKills);
		writer.WriteInt(roundCd);
		writer.WriteInt(seconds);
		this.SendRPCInternal("System.Void RoundSummary::RpcShowRoundSummary(RoundSummary/SumInfo_ClassList,RoundSummary/SumInfo_ClassList,RoundSummary/LeadingTeam,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)", -173396156, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcHideRoundSummary()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void RoundSummary::RpcHideRoundSummary()", 593541252, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcDimScreen()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void RoundSummary::RpcDimScreen()", -1745793588, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcUndimScreen()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void RoundSummary::RpcUndimScreen()", 44991443, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public static bool RoundInProgress()
	{
		if (!RoundSummary._singletonSet)
		{
			return false;
		}
		if (!ReferenceHub.TryGetHostHub(out var hub))
		{
			return false;
		}
		return hub.characterClassManager.RoundStarted;
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32(SumInfo_ClassList listStart, SumInfo_ClassList listFinish, LeadingTeam leadingTeam, int eDS, int eSc, int scpKills, int roundCd, int seconds)
	{
		this._summaryActive = true;
	}

	protected static void InvokeUserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcShowRoundSummary called on server.");
		}
		else
		{
			((RoundSummary)obj).UserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32(GeneratedNetworkCode._Read_RoundSummary_002FSumInfo_ClassList(reader), GeneratedNetworkCode._Read_RoundSummary_002FSumInfo_ClassList(reader), GeneratedNetworkCode._Read_RoundSummary_002FLeadingTeam(reader), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt(), reader.ReadInt());
		}
	}

	protected void UserCode_RpcHideRoundSummary()
	{
		this._summaryActive = false;
	}

	protected static void InvokeUserCode_RpcHideRoundSummary(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcHideRoundSummary called on server.");
		}
		else
		{
			((RoundSummary)obj).UserCode_RpcHideRoundSummary();
		}
	}

	protected void UserCode_RpcDimScreen()
	{
	}

	protected static void InvokeUserCode_RpcDimScreen(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDimScreen called on server.");
		}
		else
		{
			((RoundSummary)obj).UserCode_RpcDimScreen();
		}
	}

	protected void UserCode_RpcUndimScreen()
	{
	}

	protected static void InvokeUserCode_RpcUndimScreen(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUndimScreen called on server.");
		}
		else
		{
			((RoundSummary)obj).UserCode_RpcUndimScreen();
		}
	}

	static RoundSummary()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(RoundSummary), "System.Void RoundSummary::RpcShowRoundSummary(RoundSummary/SumInfo_ClassList,RoundSummary/SumInfo_ClassList,RoundSummary/LeadingTeam,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)", InvokeUserCode_RpcShowRoundSummary__SumInfo_ClassList__SumInfo_ClassList__LeadingTeam__Int32__Int32__Int32__Int32__Int32);
		RemoteProcedureCalls.RegisterRpc(typeof(RoundSummary), "System.Void RoundSummary::RpcHideRoundSummary()", InvokeUserCode_RpcHideRoundSummary);
		RemoteProcedureCalls.RegisterRpc(typeof(RoundSummary), "System.Void RoundSummary::RpcDimScreen()", InvokeUserCode_RpcDimScreen);
		RemoteProcedureCalls.RegisterRpc(typeof(RoundSummary), "System.Void RoundSummary::RpcUndimScreen()", InvokeUserCode_RpcUndimScreen);
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
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteInt(this._extraTargets);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._extraTargets, null, reader.ReadInt());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._extraTargets, null, reader.ReadInt());
		}
	}
}

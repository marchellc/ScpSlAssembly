using System;
using System.Collections.Generic;
using GameCore;
using MapGeneration.RoomConnectors;
using MEC;
using Mirror;
using Mirror.RemoteCalls;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Christmas.Scp2536;

public class Scp2536Controller : NetworkBehaviour
{
	[Serializable]
	private struct SoundPerTeam
	{
		public Team Team;

		public AudioClip Clip;
	}

	private const int AppearanceLifetime = 20;

	private const int TimeBetweenAppearances = 60;

	private const float AppearDelay = 3f;

	private const float DisappearDelay = 1.3f;

	private const float TimeBetweenRetries = 10f;

	private static readonly Team[] WhitelistedTeams;

	public static readonly Dictionary<Team, AudioClip> TeamClips;

	private static bool _init;

	private readonly HashSet<uint> _ignoredPlayers = new HashSet<uint>();

	[SerializeField]
	private SoundPerTeam[] _sounds;

	[SerializeField]
	private List<AudioClip> _musicClips;

	[SerializeField]
	private AudioClip _disappearClip;

	[SerializeField]
	private AudioSource _musicSource;

	[SerializeField]
	private List<Material> _mats = new List<Material>();

	[SerializeField]
	private float _minDistanceSqr = 2f;

	[SerializeField]
	private float _maxDistanceSqr = 300f;

	[SerializeField]
	private Collider _collider;

	private bool _shaderIncrease = true;

	private bool _hidden = true;

	public static Scp2536Controller Singleton { get; private set; }

	[field: SerializeField]
	public Scp2536GiftController GiftController { get; private set; }

	private bool ServerFindTarget(out ReferenceHub target, out Scp2536Spawnpoint spawnpoint)
	{
		target = null;
		spawnpoint = null;
		if (!NetworkServer.active)
		{
			return false;
		}
		List<Team> list = ListPool<Team>.Shared.Rent();
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			Team team = allHub.GetTeam();
			if (Scp2536Controller.WhitelistedTeams.Contains(team) && !list.Contains(team))
			{
				list.Add(team);
			}
		}
		if (list.Count == 0)
		{
			ListPool<Team>.Shared.Return(list);
			return false;
		}
		Team team2 = list.RandomItem();
		List<ReferenceHub> list2 = ListPool<ReferenceHub>.Shared.Rent();
		foreach (ReferenceHub allHub2 in ReferenceHub.AllHubs)
		{
			if (allHub2.GetTeam() == team2 && !this._ignoredPlayers.Contains(allHub2.netId) && allHub2.roleManager.CurrentRole is IFpcRole)
			{
				list2.Add(allHub2);
			}
		}
		ListPool<Team>.Shared.Return(list);
		if (list2.Count == 0)
		{
			this._ignoredPlayers.Clear();
			ListPool<ReferenceHub>.Shared.Return(list2);
			return false;
		}
		target = list2.RandomItem();
		ListPool<ReferenceHub>.Shared.Return(list2);
		return this.CanFindPosition(target, out spawnpoint);
	}

	private bool CanFindPosition(ReferenceHub target, out Scp2536Spawnpoint foundSpot)
	{
		foundSpot = null;
		if (!(target.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		float num = this._maxDistanceSqr;
		Bounds bounds = this._collider.bounds;
		foreach (Scp2536Spawnpoint spawnpoint in Scp2536Spawnpoint.Spawnpoints)
		{
			if (!SpawnableClutterConnector.Instances.Any((SpawnableClutterConnector c) => c.Intersects(bounds)))
			{
				float sqrMagnitude = (fpcRole.FpcModule.Position - spawnpoint.Position).sqrMagnitude;
				if (!(sqrMagnitude > this._maxDistanceSqr) && !(sqrMagnitude < this._minDistanceSqr) && sqrMagnitude < num)
				{
					num = sqrMagnitude;
					foundSpot = spawnpoint;
				}
			}
		}
		return num < this._maxDistanceSqr;
	}

	[ClientRpc]
	public void RpcMoveTree(Vector3 newPos, Quaternion newRot, byte randomSeed)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteVector3(newPos);
		writer.WriteQuaternion(newRot);
		NetworkWriterExtensions.WriteByte(writer, randomSeed);
		this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcMoveTree(UnityEngine.Vector3,UnityEngine.Quaternion,System.Byte)", 1984975944, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcHideTree()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcHideTree()", 1400729660, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private void Awake()
	{
		Scp2536Controller.Singleton = this;
		Timing.RunCoroutine(this.TeleportingLogicCoroutine().CancelWith(this));
		if (!Scp2536Controller._init)
		{
			Scp2536Controller._init = true;
			SoundPerTeam[] sounds = this._sounds;
			for (int i = 0; i < sounds.Length; i++)
			{
				SoundPerTeam soundPerTeam = sounds[i];
				Scp2536Controller.TeamClips.Add(soundPerTeam.Team, soundPerTeam.Clip);
			}
		}
	}

	private void Update()
	{
	}

	[ClientRpc]
	private void RpcPlayDisappear()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayDisappear()", 295100853, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void RpcPlayTeamSpawn(Team team, Vector3 position)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_PlayerRoles_002ETeam(writer, team);
		writer.WriteVector3(position);
		this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayTeamSpawn(PlayerRoles.Team,UnityEngine.Vector3)", -1071699852, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private IEnumerator<float> TeleportingLogicCoroutine()
	{
		while (!RoundStart.RoundStarted)
		{
			yield return float.NegativeInfinity;
		}
		yield return Timing.WaitForSeconds(60f);
		while (true)
		{
			yield return Timing.WaitForSeconds(10f);
			if (this.ServerFindTarget(out var target, out var spawnpoint))
			{
				Vector3 spawnPos = spawnpoint.Position;
				Quaternion spawnRot = spawnpoint.Rotation;
				this._ignoredPlayers.Add(target.netId);
				this.RpcPlayTeamSpawn(target.GetTeam(), spawnpoint.Position);
				yield return Timing.WaitForSeconds(3f);
				this.GiftController.ServerPrepareGifts();
				this.RpcMoveTree(spawnPos, spawnRot, (byte)UnityEngine.Random.Range(0, 256));
				yield return Timing.WaitForSeconds(20f);
				this.RpcPlayDisappear();
				yield return Timing.WaitForSeconds(1.3f);
				this.RpcHideTree();
				yield return Timing.WaitForSeconds(60f);
			}
		}
	}

	static Scp2536Controller()
	{
		Scp2536Controller.WhitelistedTeams = new Team[4]
		{
			Team.ClassD,
			Team.Scientists,
			Team.FoundationForces,
			Team.ChaosInsurgency
		};
		Scp2536Controller.TeamClips = new Dictionary<Team, AudioClip>();
		RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcMoveTree(UnityEngine.Vector3,UnityEngine.Quaternion,System.Byte)", InvokeUserCode_RpcMoveTree__Vector3__Quaternion__Byte);
		RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcHideTree()", InvokeUserCode_RpcHideTree);
		RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayDisappear()", InvokeUserCode_RpcPlayDisappear);
		RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayTeamSpawn(PlayerRoles.Team,UnityEngine.Vector3)", InvokeUserCode_RpcPlayTeamSpawn__Team__Vector3);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcMoveTree__Vector3__Quaternion__Byte(Vector3 newPos, Quaternion newRot, byte randomSeed)
	{
		base.transform.SetPositionAndRotation(newPos, newRot);
	}

	protected static void InvokeUserCode_RpcMoveTree__Vector3__Quaternion__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcMoveTree called on server.");
		}
		else
		{
			((Scp2536Controller)obj).UserCode_RpcMoveTree__Vector3__Quaternion__Byte(reader.ReadVector3(), reader.ReadQuaternion(), NetworkReaderExtensions.ReadByte(reader));
		}
	}

	protected void UserCode_RpcHideTree()
	{
	}

	protected static void InvokeUserCode_RpcHideTree(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcHideTree called on server.");
		}
		else
		{
			((Scp2536Controller)obj).UserCode_RpcHideTree();
		}
	}

	protected void UserCode_RpcPlayDisappear()
	{
	}

	protected static void InvokeUserCode_RpcPlayDisappear(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayDisappear called on server.");
		}
		else
		{
			((Scp2536Controller)obj).UserCode_RpcPlayDisappear();
		}
	}

	protected void UserCode_RpcPlayTeamSpawn__Team__Vector3(Team team, Vector3 position)
	{
	}

	protected static void InvokeUserCode_RpcPlayTeamSpawn__Team__Vector3(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayTeamSpawn called on server.");
		}
		else
		{
			((Scp2536Controller)obj).UserCode_RpcPlayTeamSpawn__Team__Vector3(GeneratedNetworkCode._Read_PlayerRoles_002ETeam(reader), reader.ReadVector3());
		}
	}
}

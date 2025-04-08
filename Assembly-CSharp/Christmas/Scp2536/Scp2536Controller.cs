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

namespace Christmas.Scp2536
{
	public class Scp2536Controller : NetworkBehaviour
	{
		public static Scp2536Controller Singleton { get; private set; }

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
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				Team team = referenceHub.GetTeam();
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
			Team team2 = list.RandomItem<Team>();
			List<ReferenceHub> list2 = ListPool<ReferenceHub>.Shared.Rent();
			foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
			{
				if (referenceHub2.GetTeam() == team2 && !this._ignoredPlayers.Contains(referenceHub2.netId) && referenceHub2.roleManager.CurrentRole is IFpcRole)
				{
					list2.Add(referenceHub2);
				}
			}
			ListPool<Team>.Shared.Return(list);
			if (list2.Count == 0)
			{
				this._ignoredPlayers.Clear();
				ListPool<ReferenceHub>.Shared.Return(list2);
				return false;
			}
			target = list2.RandomItem<ReferenceHub>();
			ListPool<ReferenceHub>.Shared.Return(list2);
			return this.CanFindPosition(target, out spawnpoint);
		}

		private bool CanFindPosition(ReferenceHub target, out Scp2536Spawnpoint foundSpot)
		{
			foundSpot = null;
			IFpcRole fpcRole = target.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			float num = this._maxDistanceSqr;
			Bounds bounds = this._collider.bounds;
			Func<SpawnableClutterConnector, bool> <>9__0;
			foreach (Scp2536Spawnpoint scp2536Spawnpoint in Scp2536Spawnpoint.Spawnpoints)
			{
				List<SpawnableClutterConnector> instances = SpawnableClutterConnector.Instances;
				Func<SpawnableClutterConnector, bool> func;
				if ((func = <>9__0) == null)
				{
					func = (<>9__0 = (SpawnableClutterConnector c) => c.Intersects(bounds));
				}
				if (!instances.Any(func))
				{
					float sqrMagnitude = (fpcRole.FpcModule.Position - scp2536Spawnpoint.Position).sqrMagnitude;
					if (sqrMagnitude <= this._maxDistanceSqr && sqrMagnitude >= this._minDistanceSqr && sqrMagnitude < num)
					{
						num = sqrMagnitude;
						foundSpot = scp2536Spawnpoint;
					}
				}
			}
			return num < this._maxDistanceSqr;
		}

		[ClientRpc]
		public void RpcMoveTree(Vector3 newPos, Quaternion newRot, byte randomSeed)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteVector3(newPos);
			networkWriterPooled.WriteQuaternion(newRot);
			networkWriterPooled.WriteByte(randomSeed);
			this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcMoveTree(UnityEngine.Vector3,UnityEngine.Quaternion,System.Byte)", 1984975944, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[ClientRpc]
		private void RpcHideTree()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcHideTree()", 1400729660, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private void Awake()
		{
			Scp2536Controller.Singleton = this;
			Timing.RunCoroutine(this.TeleportingLogicCoroutine().CancelWith(this));
			if (Scp2536Controller._init)
			{
				return;
			}
			Scp2536Controller._init = true;
			foreach (Scp2536Controller.SoundPerTeam soundPerTeam in this._sounds)
			{
				Scp2536Controller.TeamClips.Add(soundPerTeam.Team, soundPerTeam.Clip);
			}
		}

		private void Update()
		{
		}

		[ClientRpc]
		private void RpcPlayDisappear()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayDisappear()", 295100853, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[ClientRpc]
		private void RpcPlayTeamSpawn(Team team, Vector3 position)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			global::Mirror.GeneratedNetworkCode._Write_PlayerRoles.Team(networkWriterPooled, team);
			networkWriterPooled.WriteVector3(position);
			this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayTeamSpawn(PlayerRoles.Team,UnityEngine.Vector3)", -1071699852, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private IEnumerator<float> TeleportingLogicCoroutine()
		{
			while (!RoundStart.RoundStarted)
			{
				yield return float.NegativeInfinity;
			}
			yield return Timing.WaitForSeconds(60f);
			for (;;)
			{
				yield return Timing.WaitForSeconds(10f);
				ReferenceHub referenceHub;
				Scp2536Spawnpoint scp2536Spawnpoint;
				if (this.ServerFindTarget(out referenceHub, out scp2536Spawnpoint))
				{
					Vector3 spawnPos = scp2536Spawnpoint.Position;
					Quaternion spawnRot = scp2536Spawnpoint.Rotation;
					this._ignoredPlayers.Add(referenceHub.netId);
					this.RpcPlayTeamSpawn(referenceHub.GetTeam(), scp2536Spawnpoint.Position);
					yield return Timing.WaitForSeconds(3f);
					this.GiftController.ServerPrepareGifts(true);
					this.RpcMoveTree(spawnPos, spawnRot, (byte)global::UnityEngine.Random.Range(0, 256));
					yield return Timing.WaitForSeconds(20f);
					this.RpcPlayDisappear();
					yield return Timing.WaitForSeconds(1.3f);
					this.RpcHideTree();
					yield return Timing.WaitForSeconds(60f);
					spawnPos = default(Vector3);
					spawnRot = default(Quaternion);
				}
			}
			yield break;
		}

		static Scp2536Controller()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcMoveTree(UnityEngine.Vector3,UnityEngine.Quaternion,System.Byte)", new RemoteCallDelegate(Scp2536Controller.InvokeUserCode_RpcMoveTree__Vector3__Quaternion__Byte));
			RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcHideTree()", new RemoteCallDelegate(Scp2536Controller.InvokeUserCode_RpcHideTree));
			RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayDisappear()", new RemoteCallDelegate(Scp2536Controller.InvokeUserCode_RpcPlayDisappear));
			RemoteProcedureCalls.RegisterRpc(typeof(Scp2536Controller), "System.Void Christmas.Scp2536.Scp2536Controller::RpcPlayTeamSpawn(PlayerRoles.Team,UnityEngine.Vector3)", new RemoteCallDelegate(Scp2536Controller.InvokeUserCode_RpcPlayTeamSpawn__Team__Vector3));
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
				return;
			}
			((Scp2536Controller)obj).UserCode_RpcMoveTree__Vector3__Quaternion__Byte(reader.ReadVector3(), reader.ReadQuaternion(), reader.ReadByte());
		}

		protected void UserCode_RpcHideTree()
		{
		}

		protected static void InvokeUserCode_RpcHideTree(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcHideTree called on server.");
				return;
			}
			((Scp2536Controller)obj).UserCode_RpcHideTree();
		}

		protected void UserCode_RpcPlayDisappear()
		{
		}

		protected static void InvokeUserCode_RpcPlayDisappear(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayDisappear called on server.");
				return;
			}
			((Scp2536Controller)obj).UserCode_RpcPlayDisappear();
		}

		protected void UserCode_RpcPlayTeamSpawn__Team__Vector3(Team team, Vector3 position)
		{
		}

		protected static void InvokeUserCode_RpcPlayTeamSpawn__Team__Vector3(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayTeamSpawn called on server.");
				return;
			}
			((Scp2536Controller)obj).UserCode_RpcPlayTeamSpawn__Team__Vector3(global::Mirror.GeneratedNetworkCode._Read_PlayerRoles.Team(reader), reader.ReadVector3());
		}

		private const int AppearanceLifetime = 20;

		private const int TimeBetweenAppearances = 60;

		private const float AppearDelay = 3f;

		private const float DisappearDelay = 1.3f;

		private const float TimeBetweenRetries = 10f;

		private static readonly Team[] WhitelistedTeams = new Team[]
		{
			Team.ClassD,
			Team.Scientists,
			Team.FoundationForces,
			Team.ChaosInsurgency
		};

		public static readonly Dictionary<Team, AudioClip> TeamClips = new Dictionary<Team, AudioClip>();

		private static bool _init;

		private readonly HashSet<uint> _ignoredPlayers = new HashSet<uint>();

		[SerializeField]
		private Scp2536Controller.SoundPerTeam[] _sounds;

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

		[Serializable]
		private struct SoundPerTeam
		{
			public Team Team;

			public AudioClip Clip;
		}
	}
}

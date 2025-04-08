using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using Respawning.Objectives;
using UnityEngine;

namespace Respawning
{
	public static class FactionInfluenceManager
	{
		public static event FactionInfluenceManager.OnInfluenceModified InfluenceModified;

		public static float Get(Faction faction)
		{
			float num;
			if (!FactionInfluenceManager.Influence.TryGetValue(faction, out num))
			{
				num = 0f;
				FactionInfluenceManager.Influence[faction] = num;
			}
			return num;
		}

		public static void Set(Faction faction, float influence)
		{
			FactionInfluenceManager.OnInfluenceModified influenceModified = FactionInfluenceManager.InfluenceModified;
			if (influenceModified != null)
			{
				influenceModified(faction, influence);
			}
			FactionInfluenceManager.Influence[faction] = influence;
			if (!NetworkServer.active)
			{
				return;
			}
			NetworkServer.SendToReady<InfluenceUpdateMessage>(new InfluenceUpdateMessage
			{
				Faction = faction,
				Influence = influence
			}, 0);
		}

		public static void Add(Faction faction, float influence)
		{
			FactionInfluenceManager.Set(faction, FactionInfluenceManager.Get(faction) + influence);
		}

		public static void Remove(Faction faction, float influence)
		{
			FactionInfluenceManager.Set(faction, FactionInfluenceManager.Get(faction) - influence);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<InfluenceUpdateMessage>(new Action<InfluenceUpdateMessage>(FactionInfluenceManager.ClientMessageReceived), true);
				if (!NetworkServer.active)
				{
					return;
				}
				FactionInfluenceManager.ServerResetInfluence();
			};
		}

		private static void ServerResetInfluence()
		{
			foreach (object obj in Enum.GetValues(typeof(Faction)))
			{
				Faction faction = (Faction)obj;
				FactionInfluenceManager.Influence[faction] = 0f;
			}
		}

		private static void ClientMessageReceived(InfluenceUpdateMessage msg)
		{
			if (NetworkServer.active)
			{
				return;
			}
			FactionInfluenceManager.Set(msg.Faction, msg.Influence);
		}

		public const float DefaultInfluence = 0f;

		public static readonly Dictionary<Faction, float> Influence = new Dictionary<Faction, float>();

		public static readonly List<FactionObjectiveBase> Objectives = new List<FactionObjectiveBase>
		{
			new HumanKillObjective(),
			new EscapeObjective(),
			new GeneratorActivatedObjective(),
			new ScpItemPickupObjective(),
			new HumanDamageObjective()
		};

		public delegate void OnInfluenceModified(Faction faction, float newValue);
	}
}

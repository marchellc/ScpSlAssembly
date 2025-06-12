using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using Respawning.Objectives;
using UnityEngine;

namespace Respawning;

public static class FactionInfluenceManager
{
	public delegate void OnInfluenceModified(Faction faction, float newValue);

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

	public static event OnInfluenceModified InfluenceModified;

	public static float Get(Faction faction)
	{
		if (!FactionInfluenceManager.Influence.TryGetValue(faction, out var value))
		{
			value = 0f;
			FactionInfluenceManager.Influence[faction] = value;
		}
		return value;
	}

	public static void Set(Faction faction, float influence)
	{
		FactionInfluenceManager.InfluenceModified?.Invoke(faction, influence);
		FactionInfluenceManager.Influence[faction] = influence;
		if (NetworkServer.active)
		{
			NetworkServer.SendToReady(new InfluenceUpdateMessage
			{
				Faction = faction,
				Influence = influence
			});
		}
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
			NetworkClient.ReplaceHandler<InfluenceUpdateMessage>(ClientMessageReceived);
			if (NetworkServer.active)
			{
				FactionInfluenceManager.ServerResetInfluence();
			}
		};
	}

	private static void ServerResetInfluence()
	{
		foreach (Faction value in Enum.GetValues(typeof(Faction)))
		{
			FactionInfluenceManager.Influence[value] = 0f;
		}
	}

	private static void ClientMessageReceived(InfluenceUpdateMessage msg)
	{
		if (!NetworkServer.active)
		{
			FactionInfluenceManager.Set(msg.Faction, msg.Influence);
		}
	}
}

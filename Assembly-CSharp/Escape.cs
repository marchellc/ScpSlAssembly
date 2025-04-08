using System;
using System.Collections.Generic;
using InventorySystem.Disarming;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.NonAllocLINQ;

public static class Escape
{
	public static event Action<ReferenceHub> OnServerPlayerEscape;

	public static bool CanEscape(ReferenceHub hub, out IFpcRole role)
	{
		role = null;
		IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
		if (fpcRole == null)
		{
			return false;
		}
		role = fpcRole;
		return (role.FpcModule.Position - Escape.WorldPos).sqrMagnitude <= 156.5f;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnUpdate += delegate
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub.AllHubs.ForEach(delegate(ReferenceHub x)
			{
				Escape.ServerHandlePlayer(x);
			});
		};
	}

	private static void ServerHandlePlayer(ReferenceHub hub)
	{
		IFpcRole fpcRole;
		if (!Escape.CanEscape(hub, out fpcRole))
		{
			return;
		}
		RoleTypeId roleTypeId = RoleTypeId.None;
		HumanRole humanRole = fpcRole as HumanRole;
		Escape.EscapeScenarioType escapeScenarioType = ((humanRole != null) ? Escape.ServerGetScenario(hub, humanRole) : Escape.EscapeScenarioType.None);
		switch (escapeScenarioType)
		{
		case Escape.EscapeScenarioType.ClassD:
		case Escape.EscapeScenarioType.CuffedScientist:
			roleTypeId = RoleTypeId.ChaosConscript;
			break;
		case Escape.EscapeScenarioType.CuffedClassD:
			roleTypeId = RoleTypeId.NtfPrivate;
			break;
		case Escape.EscapeScenarioType.Scientist:
			roleTypeId = RoleTypeId.NtfSpecialist;
			break;
		}
		PlayerEscapingEventArgs playerEscapingEventArgs = new PlayerEscapingEventArgs(hub, roleTypeId, escapeScenarioType);
		PlayerEvents.OnEscaping(playerEscapingEventArgs);
		if (!playerEscapingEventArgs.IsAllowed || escapeScenarioType == Escape.EscapeScenarioType.None)
		{
			return;
		}
		roleTypeId = playerEscapingEventArgs.NewRole;
		escapeScenarioType = playerEscapingEventArgs.EscapeScenario;
		Escape.OnServerPlayerEscape(hub);
		hub.roleManager.ServerSetRole(roleTypeId, RoleChangeReason.Escaped, RoleSpawnFlags.All);
		hub.connectionToClient.Send<Escape.EscapeMessage>(new Escape.EscapeMessage
		{
			ScenarioId = (byte)escapeScenarioType,
			EscapeTime = (ushort)Mathf.CeilToInt(hub.roleManager.CurrentRole.ActiveTime)
		}, 0);
		PlayerEvents.OnEscaped(new PlayerEscapedEventArgs(hub, roleTypeId, escapeScenarioType));
	}

	private static Escape.EscapeScenarioType ServerGetScenario(ReferenceHub hub, HumanRole role)
	{
		if (role.ActiveTime < 10f)
		{
			return Escape.EscapeScenarioType.None;
		}
		bool flag = hub.inventory.IsDisarmed();
		if (flag && !CharacterClassManager.CuffedChangeTeam)
		{
			return Escape.EscapeScenarioType.None;
		}
		RoleTypeId roleTypeId = role.RoleTypeId;
		if (roleTypeId != RoleTypeId.ClassD)
		{
			if (roleTypeId != RoleTypeId.Scientist)
			{
				return Escape.EscapeScenarioType.None;
			}
			if (!flag)
			{
				return Escape.EscapeScenarioType.Scientist;
			}
			return Escape.EscapeScenarioType.CuffedScientist;
		}
		else
		{
			if (!flag)
			{
				return Escape.EscapeScenarioType.ClassD;
			}
			return Escape.EscapeScenarioType.CuffedClassD;
		}
	}

	private static void ClientReceiveMessage(Escape.EscapeMessage msg)
	{
	}

	// Note: this type is marked as 'beforefieldinit'.
	static Escape()
	{
		Dictionary<Escape.EscapeScenarioType, Escape.EscapeScenarioText> dictionary = new Dictionary<Escape.EscapeScenarioType, Escape.EscapeScenarioText>();
		dictionary[Escape.EscapeScenarioType.ClassD] = new Escape.EscapeScenarioText(30, "You escaped as a Class D and joined the Chaos Insurgency.");
		dictionary[Escape.EscapeScenarioType.CuffedClassD] = new Escape.EscapeScenarioText(36, "You were recaptured by the Nine-Tailed Fox.\nWith one less threat in the facility, they were able to reinforce.");
		dictionary[Escape.EscapeScenarioType.Scientist] = new Escape.EscapeScenarioText(29, "You escaped as a Scientist and joined the MTF units.");
		dictionary[Escape.EscapeScenarioType.CuffedScientist] = new Escape.EscapeScenarioText(37, "You were taken prisoner as a scientist by the Chaos Insurgency.\nThey were able to gain an advantage from the information you gave them.");
		Escape.Scenarios = dictionary;
		Escape.WorldPos = new Vector3(124f, 989f, 31f);
	}

	private static readonly Dictionary<Escape.EscapeScenarioType, Escape.EscapeScenarioText> Scenarios;

	public static readonly Vector3 WorldPos;

	private const float RadiusSqr = 156.5f;

	private const float MinAliveTime = 10f;

	private const string TranslationKey = "Facility";

	public enum EscapeScenarioType
	{
		None,
		ClassD,
		CuffedClassD,
		Scientist,
		CuffedScientist,
		Custom
	}

	private readonly struct EscapeScenarioText
	{
		public string Text
		{
			get
			{
				return TranslationReader.Get("Facility", this._id, this._def);
			}
		}

		public EscapeScenarioText(int translationKey, string defaultText)
		{
			this._id = translationKey;
			this._def = defaultText;
		}

		private readonly int _id;

		private readonly string _def;
	}

	public struct EscapeMessage : NetworkMessage
	{
		public byte ScenarioId;

		public ushort EscapeTime;
	}
}

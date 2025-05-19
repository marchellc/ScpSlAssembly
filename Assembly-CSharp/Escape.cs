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
		private readonly int _id;

		private readonly string _def;

		public string Text => TranslationReader.Get("Facility", _id, _def);

		public EscapeScenarioText(int translationKey, string defaultText)
		{
			_id = translationKey;
			_def = defaultText;
		}
	}

	public struct EscapeMessage : NetworkMessage
	{
		public byte ScenarioId;

		public ushort EscapeTime;
	}

	private static readonly Dictionary<EscapeScenarioType, EscapeScenarioText> Scenarios = new Dictionary<EscapeScenarioType, EscapeScenarioText>
	{
		[EscapeScenarioType.ClassD] = new EscapeScenarioText(30, "You escaped as a Class D and joined the Chaos Insurgency."),
		[EscapeScenarioType.CuffedClassD] = new EscapeScenarioText(36, "You were recaptured by the Nine-Tailed Fox.\nWith one less threat in the facility, they were able to reinforce."),
		[EscapeScenarioType.Scientist] = new EscapeScenarioText(29, "You escaped as a Scientist and joined the MTF units."),
		[EscapeScenarioType.CuffedScientist] = new EscapeScenarioText(37, "You were taken prisoner as a scientist by the Chaos Insurgency.\nThey were able to gain an advantage from the information you gave them.")
	};

	public static readonly Vector3 WorldPos = new Vector3(124f, 289f, 31f);

	private const float RadiusSqr = 156.5f;

	private const float MinAliveTime = 10f;

	private const string TranslationKey = "Facility";

	public static event Action<ReferenceHub> OnServerPlayerEscape;

	public static bool CanEscape(ReferenceHub hub, out IFpcRole role)
	{
		role = null;
		if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		role = fpcRole;
		if ((role.FpcModule.Position - WorldPos).sqrMagnitude > 156.5f)
		{
			return false;
		}
		return true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnUpdate += delegate
		{
			if (NetworkServer.active)
			{
				ReferenceHub.AllHubs.ForEach(delegate(ReferenceHub x)
				{
					ServerHandlePlayer(x);
				});
			}
		};
	}

	private static void ServerHandlePlayer(ReferenceHub hub)
	{
		if (!CanEscape(hub, out var role))
		{
			return;
		}
		RoleTypeId newRole = RoleTypeId.None;
		EscapeScenarioType escapeScenarioType = ((role is HumanRole role2) ? ServerGetScenario(hub, role2) : EscapeScenarioType.None);
		switch (escapeScenarioType)
		{
		case EscapeScenarioType.ClassD:
		case EscapeScenarioType.CuffedScientist:
			newRole = RoleTypeId.ChaosConscript;
			break;
		case EscapeScenarioType.CuffedClassD:
			newRole = RoleTypeId.NtfPrivate;
			break;
		case EscapeScenarioType.Scientist:
			newRole = RoleTypeId.NtfSpecialist;
			break;
		}
		PlayerEscapingEventArgs playerEscapingEventArgs = new PlayerEscapingEventArgs(hub, newRole, escapeScenarioType);
		PlayerEvents.OnEscaping(playerEscapingEventArgs);
		if (playerEscapingEventArgs.IsAllowed)
		{
			newRole = playerEscapingEventArgs.NewRole;
			escapeScenarioType = playerEscapingEventArgs.EscapeScenario;
			if (escapeScenarioType != 0)
			{
				hub.connectionToClient.Send(new EscapeMessage
				{
					ScenarioId = (byte)escapeScenarioType,
					EscapeTime = (ushort)Mathf.CeilToInt(hub.roleManager.CurrentRole.ActiveTime)
				});
				Escape.OnServerPlayerEscape(hub);
				hub.roleManager.ServerSetRole(newRole, RoleChangeReason.Escaped);
				PlayerEvents.OnEscaped(new PlayerEscapedEventArgs(hub, newRole, escapeScenarioType));
			}
		}
	}

	private static EscapeScenarioType ServerGetScenario(ReferenceHub hub, HumanRole role)
	{
		if (role.ActiveTime < 10f)
		{
			return EscapeScenarioType.None;
		}
		bool flag = hub.inventory.IsDisarmed();
		if (flag && !CharacterClassManager.CuffedChangeTeam)
		{
			return EscapeScenarioType.None;
		}
		switch (role.RoleTypeId)
		{
		case RoleTypeId.Scientist:
			if (!flag)
			{
				return EscapeScenarioType.Scientist;
			}
			return EscapeScenarioType.CuffedScientist;
		case RoleTypeId.ClassD:
			if (!flag)
			{
				return EscapeScenarioType.ClassD;
			}
			return EscapeScenarioType.CuffedClassD;
		default:
			return EscapeScenarioType.None;
		}
	}

	private static void ClientReceiveMessage(EscapeMessage msg)
	{
	}
}

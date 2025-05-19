using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CommandSystem;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Enums;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using Security;
using UnityEngine;

namespace RemoteAdmin;

public class QueryProcessor : NetworkBehaviour
{
	public struct CommandData
	{
		public string Command;

		public string[] Usage;

		public string Description;

		public string AliasOf;

		public bool Hidden;

		public override bool Equals(object obj)
		{
			if (obj is CommandData other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (Command, Usage, Description, AliasOf, Hidden).GetHashCode();
		}

		public bool Equals(CommandData other)
		{
			if (Command == other.Command && Usage == other.Usage && Description == other.Description && AliasOf == other.AliasOf)
			{
				return Hidden == other.Hidden;
			}
			return false;
		}

		public static bool operator ==(CommandData lhs, CommandData rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(CommandData lhs, CommandData rhs)
		{
			return !lhs.Equals(rhs);
		}
	}

	private PlayerCommandSender _sender;

	private RateLimit _commandRateLimit;

	private static CommandData[] _commands;

	private float _lastPlayerlistRequest;

	private bool _commandsSynced;

	private static bool _eventsAssigned;

	private const ushort AdminChatLenghtThreshold = 400;

	private const ushort AdminChatShortMessagesDisplayTime = 5;

	private const ushort AdminChatLongMessagesDisplayTime = 8;

	private const ushort AdminChatMaximumDisplayTime = 15;

	private const string AdminChatMonospaceFormat = "<font=\"RobotoMono\">{0}</font>";

	[SyncVar]
	[HideInInspector]
	public bool OverridePasswordEnabled;

	internal bool PasswordSent;

	private bool _gameplayData;

	private bool _gdDirty;

	private ReferenceHub _hub;

	private const int CommandDescriptionSyncMaxLength = 80;

	internal static readonly char[] SpaceArray;

	public static readonly ClientCommandHandler DotCommandHandler;

	private string _ipAddress;

	public bool GameplayData
	{
		get
		{
			return _gameplayData;
		}
		set
		{
			_gameplayData = value;
			_gdDirty = true;
		}
	}

	public bool NetworkOverridePasswordEnabled
	{
		get
		{
			return OverridePasswordEnabled;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref OverridePasswordEnabled, 1uL, null);
		}
	}

	public bool TryGetSender(out PlayerCommandSender sender)
	{
		return (sender = _sender) != null;
	}

	private void Awake()
	{
		_hub = ReferenceHub.GetHub(this);
	}

	private void Start()
	{
		if (base.isLocalPlayer && NetworkServer.active)
		{
			EncryptedChannelManager.ReplaceServerHandler(EncryptedChannelManager.EncryptedChannel.RemoteAdmin, ServerHandleCommandFromClient);
			EncryptedChannelManager.ReplaceServerHandler(EncryptedChannelManager.EncryptedChannel.AdminChat, ServerHandleAdminChat);
		}
		_commandRateLimit = _hub.playerRateLimitHandler.RateLimits[1];
		if (NetworkServer.active)
		{
			_ipAddress = base.connectionToClient.address;
			NetworkOverridePasswordEnabled = ServerStatic.PermissionsHandler.OverrideEnabled;
			if (base.isLocalPlayer)
			{
				_commands = ParseCommandsToStruct(CommandProcessor.GetAllCommands());
			}
		}
		else if (base.isLocalPlayer)
		{
			_commands = null;
		}
		_sender = new PlayerCommandSender(_hub);
		_ = base.isLocalPlayer;
	}

	private void Update()
	{
		if (base.isLocalPlayer && _lastPlayerlistRequest < 1f)
		{
			_lastPlayerlistRequest += Time.deltaTime;
		}
		if (_gdDirty)
		{
			_gdDirty = false;
			if (NetworkServer.active)
			{
				TargetSyncGameplayData(base.connectionToClient, _gameplayData);
			}
		}
	}

	internal void SyncCommandsToClient()
	{
		if (!_commandsSynced)
		{
			_commandsSynced = true;
			TargetUpdateCommandList(_commands);
		}
	}

	[Server]
	private static CommandData[] ParseCommandsToStruct(List<ICommand> list)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RemoteAdmin.QueryProcessor/CommandData[] RemoteAdmin.QueryProcessor::ParseCommandsToStruct(System.Collections.Generic.List`1<CommandSystem.ICommand>)' called when server was not active");
			return null;
		}
		List<CommandData> list2 = new List<CommandData>();
		foreach (ICommand item2 in list)
		{
			string text = item2.Description;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = null;
			}
			else if (text.Length > 80)
			{
				text = text.Substring(0, 80) + "...";
			}
			CommandData commandData = default(CommandData);
			commandData.Command = item2.Command;
			commandData.Usage = ((item2 is IUsageProvider usageProvider) ? usageProvider.Usage : null);
			commandData.Description = text;
			commandData.AliasOf = null;
			commandData.Hidden = item2 is IHiddenCommand;
			CommandData item = commandData;
			list2.Add(item);
			if (item2.Aliases != null)
			{
				string[] aliases = item2.Aliases;
				foreach (string command in aliases)
				{
					list2.Add(new CommandData
					{
						Command = command,
						Usage = null,
						Description = null,
						AliasOf = item.Command,
						Hidden = item.Hidden
					});
				}
			}
		}
		return list2.ToArray();
	}

	[TargetRpc]
	private void TargetUpdateCommandList(CommandData[] commands)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_RemoteAdmin_002EQueryProcessor_002FCommandData_005B_005D(writer, commands);
		SendTargetRPCInternal(null, "System.Void RemoteAdmin.QueryProcessor::TargetUpdateCommandList(RemoteAdmin.QueryProcessor/CommandData[])", -1693762298, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	[Server]
	internal void SendToClient(string content, bool isSuccess, bool logInConsole, string overrideDisplay)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RemoteAdmin.QueryProcessor::SendToClient(System.String,System.Boolean,System.Boolean,System.String)' called when server was not active");
		}
		else
		{
			_hub.encryptedChannelManager.TrySendMessageToClient(new RemoteAdminResponse(content, isSuccess, logInConsole, overrideDisplay).Serialize(), EncryptedChannelManager.EncryptedChannel.RemoteAdmin);
		}
	}

	private static void ServerHandleCommandFromClient(ReferenceHub hub, EncryptedChannelManager.EncryptedMessage content, EncryptedChannelManager.SecurityLevel securityLevel)
	{
		if (hub.queryProcessor._commandRateLimit.CanExecute() && hub.serverRoles.RemoteAdmin)
		{
			CommandProcessor.ProcessQuery(content.ToString(), hub.queryProcessor._sender);
		}
	}

	private static void ServerHandleAdminChat(ReferenceHub hub, EncryptedChannelManager.EncryptedMessage content, EncryptedChannelManager.SecurityLevel securityLevel)
	{
		if (hub.queryProcessor._commandRateLimit.CanExecute())
		{
			CommandProcessor.ProcessAdminChat(content.ToString(), hub.queryProcessor._sender);
		}
	}

	internal static void ParseAdminChat(ref string content, out bool monospaced, out ushort time, out bool silent)
	{
		silent = content.StartsWith("@@", StringComparison.Ordinal);
		if (silent)
		{
			monospaced = false;
			time = 0;
			string text = content;
			content = text.Substring(2, text.Length - 2);
			return;
		}
		monospaced = content.StartsWith("@", StringComparison.Ordinal);
		if (monospaced)
		{
			string text = content;
			content = text.Substring(1, text.Length - 1);
		}
		time = (ushort)((content.Length > 400) ? 8 : 5);
		if (content.StartsWith("!", StringComparison.Ordinal) && content.Contains(" ", StringComparison.Ordinal))
		{
			int num = content.IndexOf(" ", StringComparison.Ordinal);
			if (ushort.TryParse(content.Substring(1, num - 1), out time))
			{
				string text = content;
				int num2 = num + 1;
				content = text.Substring(num2, text.Length - num2);
			}
			ushort num3 = time;
			ushort num4 = (ushort)((num3 > 15) ? 15 : ((num3 != 0) ? time : 5));
			time = num4;
		}
	}

	internal void ProcessGameConsoleQuery(string query)
	{
		PlayerCommandSender sender = _sender;
		string[] array = query.Trim().Split(SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
		ArraySegment<string> arguments = array.Segment(1);
		ICommand command;
		bool flag = DotCommandHandler.TryGetCommand(array[0], out command);
		CommandExecutingEventArgs commandExecutingEventArgs = new CommandExecutingEventArgs(sender, CommandType.Client, flag, command, arguments);
		ServerEvents.OnCommandExecuting(commandExecutingEventArgs);
		if (!commandExecutingEventArgs.IsAllowed || !commandExecutingEventArgs.CommandFound)
		{
			if (!commandExecutingEventArgs.CommandFound)
			{
				_hub.gameConsoleTransmission.SendToClient("Command not found.", "red");
			}
			else
			{
				_hub.gameConsoleTransmission.SendToClient("Command execution failed! Reason: Forcefully cancelled by a plugin.", "magenta");
			}
			return;
		}
		arguments = commandExecutingEventArgs.Arguments;
		sender = commandExecutingEventArgs.Sender as PlayerCommandSender;
		command = commandExecutingEventArgs.Command;
		string response = string.Empty;
		bool flag2 = false;
		string color = "red";
		try
		{
			if (flag)
			{
				flag2 = command.Execute(arguments, sender, out response);
				response = Misc.CloseAllRichTextTags(response);
				color = (flag2 ? "green" : "magenta");
			}
			else
			{
				flag2 = false;
				response = "Command not found.";
				color = "red";
			}
		}
		catch (Exception ex)
		{
			response = "Command execution failed! Error: " + ex;
			flag2 = false;
			color = "magenta";
		}
		finally
		{
			_hub.gameConsoleTransmission.SendToClient(response, color);
			ServerEvents.OnCommandExecuted(new CommandExecutedEventArgs(sender, CommandType.Client, command, arguments, flag2, response));
		}
	}

	[TargetRpc]
	public void TargetSyncGameplayData(NetworkConnection conn, bool gd)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteBool(gd);
		SendTargetRPCInternal(conn, "System.Void RemoteAdmin.QueryProcessor::TargetSyncGameplayData(Mirror.NetworkConnection,System.Boolean)", 471852874, writer, 0);
		NetworkWriterPool.Return(writer);
	}

	private void OnDestroy()
	{
		if (NetworkServer.active)
		{
			ServerConsole.ConsoleOutputs.TryRemove(_sender.OutputId, out var value);
			ServerLogs.LiveLogOutput.TryRemove(_sender.OutputId, out value);
			if (!base.isLocalPlayer || !ServerStatic.IsDedicated)
			{
				string text = $"{_hub.LoggedNameFromRefHub()} disconnected from IP address {_ipAddress}. Last class: {_hub.GetRoleId()}.";
				ServerLogs.AddLog(ServerLogs.Modules.Networking, text, ServerLogs.ServerLogType.ConnectionUpdate);
				ServerConsole.AddLog(text);
			}
		}
	}

	static QueryProcessor()
	{
		SpaceArray = new char[1] { ' ' };
		DotCommandHandler = ClientCommandHandler.Create();
		RemoteProcedureCalls.RegisterRpc(typeof(QueryProcessor), "System.Void RemoteAdmin.QueryProcessor::TargetUpdateCommandList(RemoteAdmin.QueryProcessor/CommandData[])", InvokeUserCode_TargetUpdateCommandList__CommandData_005B_005D);
		RemoteProcedureCalls.RegisterRpc(typeof(QueryProcessor), "System.Void RemoteAdmin.QueryProcessor::TargetSyncGameplayData(Mirror.NetworkConnection,System.Boolean)", InvokeUserCode_TargetSyncGameplayData__NetworkConnection__Boolean);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_TargetUpdateCommandList__CommandData_005B_005D(CommandData[] commands)
	{
	}

	protected static void InvokeUserCode_TargetUpdateCommandList__CommandData_005B_005D(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetUpdateCommandList called on server.");
		}
		else
		{
			((QueryProcessor)obj).UserCode_TargetUpdateCommandList__CommandData_005B_005D(GeneratedNetworkCode._Read_RemoteAdmin_002EQueryProcessor_002FCommandData_005B_005D(reader));
		}
	}

	protected void UserCode_TargetSyncGameplayData__NetworkConnection__Boolean(NetworkConnection conn, bool gd)
	{
		_gameplayData = gd;
	}

	protected static void InvokeUserCode_TargetSyncGameplayData__NetworkConnection__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("TargetRPC TargetSyncGameplayData called on server.");
		}
		else
		{
			((QueryProcessor)obj).UserCode_TargetSyncGameplayData__NetworkConnection__Boolean(null, reader.ReadBool());
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(OverridePasswordEnabled);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(OverridePasswordEnabled);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref OverridePasswordEnabled, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref OverridePasswordEnabled, null, reader.ReadBool());
		}
	}
}

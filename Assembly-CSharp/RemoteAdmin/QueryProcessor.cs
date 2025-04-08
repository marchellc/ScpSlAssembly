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

namespace RemoteAdmin
{
	public class QueryProcessor : NetworkBehaviour
	{
		public bool GameplayData
		{
			get
			{
				return this._gameplayData;
			}
			set
			{
				this._gameplayData = value;
				this._gdDirty = true;
			}
		}

		private void Awake()
		{
			this._hub = ReferenceHub.GetHub(this);
		}

		private void Start()
		{
			if (base.isLocalPlayer && NetworkServer.active)
			{
				EncryptedChannelManager.ReplaceServerHandler(EncryptedChannelManager.EncryptedChannel.RemoteAdmin, new EncryptedChannelManager.EncryptedMessageServerHandler(QueryProcessor.ServerHandleCommandFromClient));
				EncryptedChannelManager.ReplaceServerHandler(EncryptedChannelManager.EncryptedChannel.AdminChat, new EncryptedChannelManager.EncryptedMessageServerHandler(QueryProcessor.ServerHandleAdminChat));
			}
			this._commandRateLimit = this._hub.playerRateLimitHandler.RateLimits[1];
			if (NetworkServer.active)
			{
				this._ipAddress = base.connectionToClient.address;
				this.NetworkOverridePasswordEnabled = ServerStatic.PermissionsHandler.OverrideEnabled;
				if (base.isLocalPlayer)
				{
					QueryProcessor._commands = QueryProcessor.ParseCommandsToStruct(CommandProcessor.GetAllCommands());
				}
			}
			else if (base.isLocalPlayer)
			{
				QueryProcessor._commands = null;
			}
			this._sender = new PlayerCommandSender(this._hub);
			bool isLocalPlayer = base.isLocalPlayer;
		}

		private void Update()
		{
			if (base.isLocalPlayer && this._lastPlayerlistRequest < 1f)
			{
				this._lastPlayerlistRequest += Time.deltaTime;
			}
			if (!this._gdDirty)
			{
				return;
			}
			this._gdDirty = false;
			if (!NetworkServer.active)
			{
				return;
			}
			this.TargetSyncGameplayData(base.connectionToClient, this._gameplayData);
		}

		internal void SyncCommandsToClient()
		{
			if (this._commandsSynced)
			{
				return;
			}
			this._commandsSynced = true;
			this.TargetUpdateCommandList(QueryProcessor._commands);
		}

		[Server]
		private static QueryProcessor.CommandData[] ParseCommandsToStruct(List<ICommand> list)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'RemoteAdmin.QueryProcessor/CommandData[] RemoteAdmin.QueryProcessor::ParseCommandsToStruct(System.Collections.Generic.List`1<CommandSystem.ICommand>)' called when server was not active");
				return default(QueryProcessor.CommandData[]);
			}
			List<QueryProcessor.CommandData> list2 = new List<QueryProcessor.CommandData>();
			foreach (ICommand command in list)
			{
				string text = command.Description;
				if (string.IsNullOrWhiteSpace(text))
				{
					text = null;
				}
				else if (text.Length > 80)
				{
					text = text.Substring(0, 80) + "...";
				}
				QueryProcessor.CommandData commandData = default(QueryProcessor.CommandData);
				commandData.Command = command.Command;
				IUsageProvider usageProvider = command as IUsageProvider;
				commandData.Usage = ((usageProvider != null) ? usageProvider.Usage : null);
				commandData.Description = text;
				commandData.AliasOf = null;
				commandData.Hidden = command is IHiddenCommand;
				QueryProcessor.CommandData commandData2 = commandData;
				list2.Add(commandData2);
				if (command.Aliases != null)
				{
					foreach (string text2 in command.Aliases)
					{
						List<QueryProcessor.CommandData> list3 = list2;
						commandData = new QueryProcessor.CommandData
						{
							Command = text2,
							Usage = null,
							Description = null,
							AliasOf = commandData2.Command,
							Hidden = commandData2.Hidden
						};
						list3.Add(commandData);
					}
				}
			}
			return list2.ToArray();
		}

		[TargetRpc]
		private void TargetUpdateCommandList(QueryProcessor.CommandData[] commands)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			global::Mirror.GeneratedNetworkCode._Write_RemoteAdmin.QueryProcessor/CommandData[](networkWriterPooled, commands);
			this.SendTargetRPCInternal(null, "System.Void RemoteAdmin.QueryProcessor::TargetUpdateCommandList(RemoteAdmin.QueryProcessor/CommandData[])", -1693762298, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		[Server]
		internal void SendToClient(string content, bool isSuccess, bool logInConsole, string overrideDisplay)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RemoteAdmin.QueryProcessor::SendToClient(System.String,System.Boolean,System.Boolean,System.String)' called when server was not active");
				return;
			}
			this._hub.encryptedChannelManager.TrySendMessageToClient(new RemoteAdminResponse(content, isSuccess, logInConsole, overrideDisplay).Serialize(), EncryptedChannelManager.EncryptedChannel.RemoteAdmin);
		}

		private static void ServerHandleCommandFromClient(ReferenceHub hub, EncryptedChannelManager.EncryptedMessage content, EncryptedChannelManager.SecurityLevel securityLevel)
		{
			if (hub.queryProcessor._commandRateLimit.CanExecute(true) && hub.serverRoles.RemoteAdmin)
			{
				CommandProcessor.ProcessQuery(content.ToString(), hub.queryProcessor._sender);
			}
		}

		private static void ServerHandleAdminChat(ReferenceHub hub, EncryptedChannelManager.EncryptedMessage content, EncryptedChannelManager.SecurityLevel securityLevel)
		{
			if (hub.queryProcessor._commandRateLimit.CanExecute(true))
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
				int length = text.Length;
				int num = 2;
				int num2 = length - num;
				content = text.Substring(num, num2);
				return;
			}
			monospaced = content.StartsWith("@", StringComparison.Ordinal);
			if (monospaced)
			{
				string text2 = content;
				int length2 = text2.Length;
				int num2 = 1;
				int num = length2 - num2;
				content = text2.Substring(num2, num);
			}
			time = ((content.Length > 400) ? 8 : 5);
			if (content.StartsWith("!", StringComparison.Ordinal) && content.Contains(" ", StringComparison.Ordinal))
			{
				int num3 = content.IndexOf(" ", StringComparison.Ordinal);
				if (ushort.TryParse(content.Substring(1, num3 - 1), out time))
				{
					string text3 = content;
					int length3 = text3.Length;
					int num = num3 + 1;
					int num2 = length3 - num;
					content = text3.Substring(num, num2);
				}
				ushort num4 = time;
				ushort num5;
				if (num4 <= 15)
				{
					if (num4 != 0)
					{
						num5 = time;
					}
					else
					{
						num5 = 5;
					}
				}
				else
				{
					num5 = 15;
				}
				time = num5;
			}
		}

		internal void ProcessGameConsoleQuery(string query)
		{
			PlayerCommandSender playerCommandSender = this._sender;
			string[] array = query.Trim().Split(QueryProcessor.SpaceArray, 512, StringSplitOptions.RemoveEmptyEntries);
			ArraySegment<string> arraySegment = array.Segment(1);
			ICommand command;
			bool flag = QueryProcessor.DotCommandHandler.TryGetCommand(array[0], out command);
			CommandExecutingEventArgs commandExecutingEventArgs = new CommandExecutingEventArgs(playerCommandSender, CommandType.Client, flag, command, arraySegment);
			ServerEvents.OnCommandExecuting(commandExecutingEventArgs);
			if (commandExecutingEventArgs.IsAllowed && commandExecutingEventArgs.CommandFound)
			{
				arraySegment = commandExecutingEventArgs.Arguments;
				playerCommandSender = commandExecutingEventArgs.Sender as PlayerCommandSender;
				command = commandExecutingEventArgs.Command;
				string text = string.Empty;
				bool flag2 = false;
				string text2 = "red";
				try
				{
					if (flag)
					{
						flag2 = command.Execute(arraySegment, playerCommandSender, out text);
						text = Misc.CloseAllRichTextTags(text);
						text2 = (flag2 ? "green" : "magenta");
					}
					else
					{
						flag2 = false;
						text = "Command not found.";
						text2 = "red";
					}
				}
				catch (Exception ex)
				{
					string text3 = "Command execution failed! Error: ";
					Exception ex2 = ex;
					text = text3 + ((ex2 != null) ? ex2.ToString() : null);
					flag2 = false;
					text2 = "magenta";
				}
				finally
				{
					this._hub.gameConsoleTransmission.SendToClient(text, text2);
					ServerEvents.OnCommandExecuted(new CommandExecutedEventArgs(playerCommandSender, CommandType.Client, command, arraySegment, flag2, text));
				}
				return;
			}
			if (!commandExecutingEventArgs.CommandFound)
			{
				this._hub.gameConsoleTransmission.SendToClient("Command not found.", "red");
				return;
			}
			this._hub.gameConsoleTransmission.SendToClient("Command execution failed! Reason: Forcefully cancelled by a plugin.", "magenta");
		}

		[TargetRpc]
		public void TargetSyncGameplayData(NetworkConnection conn, bool gd)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteBool(gd);
			this.SendTargetRPCInternal(conn, "System.Void RemoteAdmin.QueryProcessor::TargetSyncGameplayData(Mirror.NetworkConnection,System.Boolean)", 471852874, networkWriterPooled, 0);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private void OnDestroy()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			IOutput output;
			ServerConsole.ConsoleOutputs.TryRemove(this._sender.OutputId, out output);
			ServerLogs.LiveLogOutput.TryRemove(this._sender.OutputId, out output);
			if (base.isLocalPlayer && ServerStatic.IsDedicated)
			{
				return;
			}
			string text = string.Format("{0} disconnected from IP address {1}. Last class: {2}.", this._hub.LoggedNameFromRefHub(), this._ipAddress, this._hub.GetRoleId());
			ServerLogs.AddLog(ServerLogs.Modules.Networking, text, ServerLogs.ServerLogType.ConnectionUpdate, false);
			ServerConsole.AddLog(text, ConsoleColor.Gray, false);
		}

		static QueryProcessor()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(QueryProcessor), "System.Void RemoteAdmin.QueryProcessor::TargetUpdateCommandList(RemoteAdmin.QueryProcessor/CommandData[])", new RemoteCallDelegate(QueryProcessor.InvokeUserCode_TargetUpdateCommandList__CommandData[]));
			RemoteProcedureCalls.RegisterRpc(typeof(QueryProcessor), "System.Void RemoteAdmin.QueryProcessor::TargetSyncGameplayData(Mirror.NetworkConnection,System.Boolean)", new RemoteCallDelegate(QueryProcessor.InvokeUserCode_TargetSyncGameplayData__NetworkConnection__Boolean));
		}

		public override bool Weaved()
		{
			return true;
		}

		public bool NetworkOverridePasswordEnabled
		{
			get
			{
				return this.OverridePasswordEnabled;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this.OverridePasswordEnabled, 1UL, null);
			}
		}

		protected void UserCode_TargetUpdateCommandList__CommandData[](QueryProcessor.CommandData[] commands)
		{
		}

		protected static void InvokeUserCode_TargetUpdateCommandList__CommandData[](NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("TargetRPC TargetUpdateCommandList called on server.");
				return;
			}
			((QueryProcessor)obj).UserCode_TargetUpdateCommandList__CommandData[](global::Mirror.GeneratedNetworkCode._Read_RemoteAdmin.QueryProcessor/CommandData[](reader));
		}

		protected void UserCode_TargetSyncGameplayData__NetworkConnection__Boolean(NetworkConnection conn, bool gd)
		{
			this._gameplayData = gd;
		}

		protected static void InvokeUserCode_TargetSyncGameplayData__NetworkConnection__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("TargetRPC TargetSyncGameplayData called on server.");
				return;
			}
			((QueryProcessor)obj).UserCode_TargetSyncGameplayData__NetworkConnection__Boolean(null, reader.ReadBool());
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteBool(this.OverridePasswordEnabled);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteBool(this.OverridePasswordEnabled);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this.OverridePasswordEnabled, null, reader.ReadBool());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this.OverridePasswordEnabled, null, reader.ReadBool());
			}
		}

		private PlayerCommandSender _sender;

		private RateLimit _commandRateLimit;

		private static QueryProcessor.CommandData[] _commands;

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

		internal static readonly char[] SpaceArray = new char[] { ' ' };

		public static readonly ClientCommandHandler DotCommandHandler = ClientCommandHandler.Create();

		private string _ipAddress;

		public struct CommandData
		{
			public override bool Equals(object obj)
			{
				if (obj is QueryProcessor.CommandData)
				{
					QueryProcessor.CommandData commandData = (QueryProcessor.CommandData)obj;
					return this.Equals(commandData);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return new ValueTuple<string, string[], string, string, bool>(this.Command, this.Usage, this.Description, this.AliasOf, this.Hidden).GetHashCode();
			}

			public bool Equals(QueryProcessor.CommandData other)
			{
				return this.Command == other.Command && this.Usage == other.Usage && this.Description == other.Description && this.AliasOf == other.AliasOf && this.Hidden == other.Hidden;
			}

			public static bool operator ==(QueryProcessor.CommandData lhs, QueryProcessor.CommandData rhs)
			{
				return lhs.Equals(rhs);
			}

			public static bool operator !=(QueryProcessor.CommandData lhs, QueryProcessor.CommandData rhs)
			{
				return !lhs.Equals(rhs);
			}

			public string Command;

			public string[] Usage;

			public string Description;

			public string AliasOf;

			public bool Hidden;
		}
	}
}

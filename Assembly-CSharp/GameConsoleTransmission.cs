using System;
using GameCore;
using Mirror;
using Security;
using UnityEngine;

public class GameConsoleTransmission : NetworkBehaviour
{
	private void Start()
	{
		this._hub = ReferenceHub.GetHub(this);
		this._cmdRateLimit = this._hub.playerRateLimitHandler.RateLimits[1];
		if (base.isLocalPlayer)
		{
			EncryptedChannelManager.ReplaceClientHandler(EncryptedChannelManager.EncryptedChannel.GameConsole, new EncryptedChannelManager.EncryptedMessageClientHandler(GameConsoleTransmission.ClientHandleMessage));
			if (NetworkServer.active)
			{
				EncryptedChannelManager.ReplaceServerHandler(EncryptedChannelManager.EncryptedChannel.GameConsole, new EncryptedChannelManager.EncryptedMessageServerHandler(GameConsoleTransmission.ServerHandleCommand));
			}
		}
	}

	[Server]
	public void SendToClient(string text, string color)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void GameConsoleTransmission::SendToClient(System.String,System.String)' called when server was not active");
			return;
		}
		ReferenceHub hub = this._hub;
		if (hub == null)
		{
			return;
		}
		hub.encryptedChannelManager.TrySendMessageToClient(color + "#" + text, EncryptedChannelManager.EncryptedChannel.GameConsole);
	}

	private static void ClientHandleMessage(EncryptedChannelManager.EncryptedMessage content, EncryptedChannelManager.SecurityLevel securityLevel)
	{
		string text = content.ToString();
		int num = text.IndexOf("#", StringComparison.Ordinal);
		string text2 = text.Remove(num);
		text = text.Remove(0, num + 1);
		global::GameCore.Console.AddLog(((securityLevel == EncryptedChannelManager.SecurityLevel.EncryptedAndAuthenticated) ? "[FROM SERVER] " : "[UNENCRYPTED FROM SERVER] ") + text, GameConsoleTransmission.ProcessColor(text2), false, global::GameCore.Console.ConsoleLogType.Log);
	}

	[Client]
	internal void SendToServer(string command)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void GameConsoleTransmission::SendToServer(System.String)' called when client was not active");
			return;
		}
		this._hub.encryptedChannelManager.TrySendMessageToServer(command, EncryptedChannelManager.EncryptedChannel.GameConsole);
	}

	private static void ServerHandleCommand(ReferenceHub hub, EncryptedChannelManager.EncryptedMessage content, EncryptedChannelManager.SecurityLevel securityLevel)
	{
		if (hub.gameConsoleTransmission._cmdRateLimit.CanExecute(true))
		{
			hub.queryProcessor.ProcessGameConsoleQuery(content.ToString());
		}
	}

	private static Color ProcessColor(string name)
	{
		uint num = <PrivateImplementationDetails>.ComputeStringHash(name);
		if (num <= 1231115066U)
		{
			if (num <= 96429129U)
			{
				if (num != 18738364U)
				{
					if (num == 96429129U)
					{
						if (name == "yellow")
						{
							return Color.yellow;
						}
					}
				}
				else if (name == "green")
				{
					return Color.green;
				}
			}
			else if (num != 1089765596U)
			{
				if (num == 1231115066U)
				{
					if (name == "cyan")
					{
						return Color.cyan;
					}
				}
			}
			else if (name == "red")
			{
				return Color.red;
			}
		}
		else if (num <= 1676028392U)
		{
			if (num != 1452231588U)
			{
				if (num == 1676028392U)
				{
					if (name == "magenta")
					{
						return Color.magenta;
					}
				}
			}
			else if (name == "black")
			{
				return Color.black;
			}
		}
		else if (num != 2197550541U)
		{
			if (num == 3724674918U)
			{
				if (name == "white")
				{
					return Color.white;
				}
			}
		}
		else if (name == "blue")
		{
			return Color.blue;
		}
		return Color.grey;
	}

	public override bool Weaved()
	{
		return true;
	}

	private ReferenceHub _hub;

	private RateLimit _cmdRateLimit;
}

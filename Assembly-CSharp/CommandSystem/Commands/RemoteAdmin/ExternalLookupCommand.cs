using System;
using System.Collections.Generic;
using MEC;
using RemoteAdmin;
using UnityEngine.Networking;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ExternalLookupCommand : ICommand, IHiddenCommand
	{
		public string Command { get; } = "externallookup";

		public string[] Aliases { get; }

		public string Description { get; } = "Internal command for lookups in RA";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.BanningUpToDay | PlayerPermissions.LongTermBanning | PlayerPermissions.SetGroup | PlayerPermissions.PlayersManagement | PlayerPermissions.PermissionsManagement | PlayerPermissions.ViewHiddenBadges | PlayerPermissions.PlayerSensitiveDataAccess | PlayerPermissions.ViewHiddenGlobalBadges, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "This command can only be executed by players!";
				return false;
			}
			string text = string.Empty;
			if (arguments.Count >= 1)
			{
				int num;
				if (!int.TryParse(arguments.At(0), out num))
				{
					response = "Invalid player id!";
					return false;
				}
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetHub(num, out referenceHub))
				{
					response = "Invalid player id!";
					return false;
				}
				text = referenceHub.authManager.UserId;
			}
			string remoteAdminExternalPlayerLookupMode = ServerConfigSynchronizer.Singleton.RemoteAdminExternalPlayerLookupMode;
			if (remoteAdminExternalPlayerLookupMode == "fullauth")
			{
				Timing.RunCoroutine(this.AuthenticateWithExternalServer(playerCommandSender, text));
				response = "Initiated communication with external server.";
				return true;
			}
			if (!(remoteAdminExternalPlayerLookupMode == "urlonly"))
			{
				response = "Invalid mode or command disabled via config.";
				return false;
			}
			playerCommandSender.RaReply("%" + text + "%" + ServerConfigSynchronizer.Singleton.RemoteAdminExternalPlayerLookupURL, true, false, "");
			response = "Lookup success!";
			return true;
		}

		private IEnumerator<float> AuthenticateWithExternalServer(PlayerCommandSender ply, string tolookup)
		{
			using (UnityWebRequest www = UnityWebRequest.Get(string.Concat(new string[]
			{
				ServerConfigSynchronizer.Singleton.RemoteAdminExternalPlayerLookupURL,
				"?user-id=",
				ply.ReferenceHub.authManager.UserId,
				"&user-ip=",
				ply.ReferenceHub.characterClassManager.connectionToClient.address,
				"&token=",
				ServerConfigSynchronizer.Singleton.RemoteAdminExternalPlayerLookupToken
			})))
			{
				yield return Timing.WaitUntilDone(www.SendWebRequest());
				string text = (string.IsNullOrEmpty(www.error) ? www.downloadHandler.text : www.error);
				if (!text.StartsWith("https://") && !text.StartsWith("http://"))
				{
					ServerConsole.AddLog("Error while performing external player lookup - URL Invalid.", ConsoleColor.Red, false);
					ply.RaReply("Error while processing reply from server - URL Invalid.", false, true, "");
					yield break;
				}
				if (text.Length > 2000)
				{
					ServerConsole.AddLog("Error while performing external player lookup - URL Too Long.", ConsoleColor.Red, false);
					ply.RaReply("Error while processing reply from server - URL Too Long.", false, true, "");
					yield break;
				}
				ply.RaReply("%" + tolookup + "%" + text, true, false, "");
			}
			UnityWebRequest www = null;
			yield break;
			yield break;
		}
	}
}

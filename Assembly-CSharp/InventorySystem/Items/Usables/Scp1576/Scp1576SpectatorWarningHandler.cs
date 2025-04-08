using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Usables.Scp1576
{
	public static class Scp1576SpectatorWarningHandler
	{
		public static event Action OnStart;

		public static event Action OnStop;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				Scp1576SpectatorWarningHandler.CurrentlyUsed.Clear();
				Scp1576SpectatorWarningHandler._stopSoundScheduled = false;
				NetworkClient.ReplaceHandler<Scp1576SpectatorWarningHandler.SpectatorWarningMessage>(new Action<Scp1576SpectatorWarningHandler.SpectatorWarningMessage>(Scp1576SpectatorWarningHandler.HandleMessage), true);
			};
			StaticUnityMethods.OnUpdate += delegate
			{
				if (!Scp1576SpectatorWarningHandler._stopSoundScheduled || !NetworkServer.active)
				{
					return;
				}
				if (Scp1576SpectatorWarningHandler.CooldownTimer.Elapsed.TotalSeconds < 2.0)
				{
					return;
				}
				Scp1576SpectatorWarningHandler.SendMessage(true);
				Scp1576SpectatorWarningHandler._stopSoundScheduled = false;
			};
		}

		private static void SendMessage(bool isStop)
		{
			new Scp1576SpectatorWarningHandler.SpectatorWarningMessage
			{
				IsStop = isStop
			}.SendToHubsConditionally((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole, 0);
		}

		private static void HandleMessage(Scp1576SpectatorWarningHandler.SpectatorWarningMessage msg)
		{
			if (msg.IsStop)
			{
				Action onStop = Scp1576SpectatorWarningHandler.OnStop;
				if (onStop == null)
				{
					return;
				}
				onStop();
				return;
			}
			else
			{
				Action onStart = Scp1576SpectatorWarningHandler.OnStart;
				if (onStart == null)
				{
					return;
				}
				onStart();
				return;
			}
		}

		public static void TriggerStart(Scp1576Item item)
		{
			Scp1576SpectatorWarningHandler._stopSoundScheduled = false;
			if (Scp1576SpectatorWarningHandler.CurrentlyUsed.Count == 0)
			{
				Scp1576SpectatorWarningHandler.CooldownTimer.Restart();
				Scp1576SpectatorWarningHandler.SendMessage(false);
			}
			Scp1576SpectatorWarningHandler.CurrentlyUsed.Add(item.ItemSerial);
		}

		public static void TriggerStop(Scp1576Item item)
		{
			if (!Scp1576SpectatorWarningHandler.CurrentlyUsed.Remove(item.ItemSerial))
			{
				return;
			}
			Scp1576SpectatorWarningHandler._stopSoundScheduled = Scp1576SpectatorWarningHandler.CurrentlyUsed.Count == 0;
		}

		private static readonly Stopwatch CooldownTimer = Stopwatch.StartNew();

		private static readonly HashSet<ushort> CurrentlyUsed = new HashSet<ushort>();

		private static bool _stopSoundScheduled;

		public struct SpectatorWarningMessage : NetworkMessage
		{
			public bool IsStop;
		}
	}
}

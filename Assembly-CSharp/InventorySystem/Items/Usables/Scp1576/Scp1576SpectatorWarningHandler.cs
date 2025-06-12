using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Usables.Scp1576;

public static class Scp1576SpectatorWarningHandler
{
	public struct SpectatorWarningMessage : NetworkMessage
	{
		public bool IsStop;
	}

	private static readonly Stopwatch CooldownTimer = Stopwatch.StartNew();

	private static readonly HashSet<ushort> CurrentlyUsed = new HashSet<ushort>();

	private static bool _stopSoundScheduled;

	public static event Action OnStart;

	public static event Action OnStop;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			Scp1576SpectatorWarningHandler.CurrentlyUsed.Clear();
			Scp1576SpectatorWarningHandler._stopSoundScheduled = false;
			NetworkClient.ReplaceHandler<SpectatorWarningMessage>(HandleMessage);
		};
		StaticUnityMethods.OnUpdate += delegate
		{
			if (Scp1576SpectatorWarningHandler._stopSoundScheduled && NetworkServer.active && !(Scp1576SpectatorWarningHandler.CooldownTimer.Elapsed.TotalSeconds < 2.0))
			{
				Scp1576SpectatorWarningHandler.SendMessage(isStop: true);
				Scp1576SpectatorWarningHandler._stopSoundScheduled = false;
			}
		};
	}

	private static void SendMessage(bool isStop)
	{
		new SpectatorWarningMessage
		{
			IsStop = isStop
		}.SendToHubsConditionally((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole);
	}

	private static void HandleMessage(SpectatorWarningMessage msg)
	{
		if (msg.IsStop)
		{
			Scp1576SpectatorWarningHandler.OnStop?.Invoke();
		}
		else
		{
			Scp1576SpectatorWarningHandler.OnStart?.Invoke();
		}
	}

	public static void TriggerStart(Scp1576Item item)
	{
		Scp1576SpectatorWarningHandler._stopSoundScheduled = false;
		if (Scp1576SpectatorWarningHandler.CurrentlyUsed.Count == 0)
		{
			Scp1576SpectatorWarningHandler.CooldownTimer.Restart();
			Scp1576SpectatorWarningHandler.SendMessage(isStop: false);
		}
		Scp1576SpectatorWarningHandler.CurrentlyUsed.Add(item.ItemSerial);
	}

	public static void TriggerStop(Scp1576Item item)
	{
		if (Scp1576SpectatorWarningHandler.CurrentlyUsed.Remove(item.ItemSerial))
		{
			Scp1576SpectatorWarningHandler._stopSoundScheduled = Scp1576SpectatorWarningHandler.CurrentlyUsed.Count == 0;
		}
	}
}

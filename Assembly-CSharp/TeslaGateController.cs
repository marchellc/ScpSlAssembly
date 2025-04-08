using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using Subtitles;
using UnityEngine;

public class TeslaGateController : NetworkBehaviour
{
	public static TeslaGateController Singleton { get; private set; }

	private static void ServerReceiveMessage(NetworkConnection conn, TeslaHitMsg msg)
	{
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
		{
			return;
		}
		if (msg.Gate == null)
		{
			referenceHub.gameConsoleTransmission.SendToClient("Received non-existing tesla gate!", "red");
			return;
		}
		if (Vector3.Distance(msg.Gate.transform.position, referenceHub.transform.position) > msg.Gate.sizeOfTrigger * 2.2f)
		{
			referenceHub.gameConsoleTransmission.SendToClient("You are too far from a tesla gate!", "red");
			return;
		}
		DamageHandlerBase.CassieAnnouncement cassieAnnouncement = new DamageHandlerBase.CassieAnnouncement
		{
			Announcement = "SUCCESSFULLY TERMINATED BY AUTOMATIC SECURITY SYSTEM",
			SubtitleParts = new SubtitlePart[]
			{
				new SubtitlePart(SubtitleType.TerminatedBySecuritySystem, null)
			}
		};
		referenceHub.playerStats.DealDamage(new UniversalDamageHandler((float)global::UnityEngine.Random.Range(200, 300), DeathTranslations.Tesla, cassieAnnouncement));
	}

	private void Awake()
	{
		TeslaGateController.Singleton = this;
	}

	private void Start()
	{
		Timing.RunCoroutine(this.DelayedStopIdleParticles());
		NetworkServer.ReplaceHandler<TeslaHitMsg>(new Action<NetworkConnectionToClient, TeslaHitMsg>(TeslaGateController.ServerReceiveMessage), true);
	}

	private IEnumerator<float> DelayedStopIdleParticles()
	{
		int j;
		for (int i = 0; i < 15; i = j + 1)
		{
			yield return float.NegativeInfinity;
			j = i;
		}
		using (HashSet<TeslaGate>.Enumerator enumerator = TeslaGate.AllGates.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				TeslaGate teslaGate = enumerator.Current;
				if (!(teslaGate == null) && teslaGate.windupParticles != null)
				{
					foreach (ParticleSystem particleSystem in teslaGate.windupParticles)
					{
						if (!(particleSystem == null))
						{
							particleSystem.Stop();
						}
					}
				}
			}
			yield break;
		}
		yield break;
	}

	public void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			using (HashSet<TeslaGate>.Enumerator enumerator = TeslaGate.AllGates.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					TeslaGate teslaGate = enumerator.Current;
					if (teslaGate.isActiveAndEnabled)
					{
						if (teslaGate.InactiveTime > 0f)
						{
							teslaGate.NetworkInactiveTime = Mathf.Max(0f, teslaGate.InactiveTime - Time.fixedDeltaTime);
						}
						else
						{
							bool flag = false;
							bool flag2 = false;
							ReferenceHub referenceHub = null;
							ReferenceHub referenceHub2 = null;
							foreach (ReferenceHub referenceHub3 in ReferenceHub.AllHubs)
							{
								if (referenceHub3.IsAlive())
								{
									if (!flag)
									{
										flag = teslaGate.IsInIdleRange(referenceHub3);
										if (flag)
										{
											PlayerIdlingTeslaEventArgs playerIdlingTeslaEventArgs = new PlayerIdlingTeslaEventArgs(referenceHub3, teslaGate);
											PlayerEvents.OnIdlingTesla(playerIdlingTeslaEventArgs);
											if (!playerIdlingTeslaEventArgs.IsAllowed)
											{
												flag = false;
											}
											else
											{
												referenceHub = referenceHub3;
											}
										}
									}
									if (!flag2 && teslaGate.PlayerInRange(referenceHub3) && !teslaGate.InProgress)
									{
										PlayerTriggeringTeslaEventArgs playerTriggeringTeslaEventArgs = new PlayerTriggeringTeslaEventArgs(referenceHub3, teslaGate);
										PlayerEvents.OnTriggeringTesla(playerTriggeringTeslaEventArgs);
										if (playerTriggeringTeslaEventArgs.IsAllowed)
										{
											referenceHub2 = referenceHub3;
											flag2 = true;
										}
									}
								}
							}
							if (flag2)
							{
								teslaGate.ServerSideCode();
								PlayerEvents.OnTriggeredTesla(new PlayerTriggeredTeslaEventArgs(referenceHub2, teslaGate));
							}
							if (flag != teslaGate.isIdling)
							{
								teslaGate.ServerSideIdle(flag);
								if (flag)
								{
									PlayerEvents.OnIdledTesla(new PlayerIdledTeslaEventArgs(referenceHub, teslaGate));
								}
							}
						}
					}
				}
				return;
			}
		}
		foreach (TeslaGate teslaGate2 in TeslaGate.AllGates)
		{
			teslaGate2.ClientSideCode();
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}

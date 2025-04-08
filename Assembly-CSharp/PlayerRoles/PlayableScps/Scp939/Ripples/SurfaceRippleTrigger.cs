using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class SurfaceRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			this._lastRipples.Clear();
			if (base.Role.IsLocalPlayer)
			{
				RippleTriggerBase.OnPlayedRippleLocally += this.OnPlayerPlayedRipple;
			}
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._lastRipples.Clear();
			if (!base.Role.IsLocalPlayer)
			{
				RippleTriggerBase.OnPlayedRippleLocally -= this.OnPlayerPlayedRipple;
			}
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteReferenceHub(this._syncPlayer);
			writer.WriteRelativePosition(this._syncPos);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (!reader.TryReadReferenceHub(out this._syncPlayer))
			{
				return;
			}
			if (!HitboxIdentity.IsEnemy(base.Owner, this._syncPlayer))
			{
				return;
			}
			FpcStandardRoleBase fpcStandardRoleBase = this._syncPlayer.roleManager.CurrentRole as FpcStandardRoleBase;
			if (fpcStandardRoleBase == null)
			{
				return;
			}
			if (fpcStandardRoleBase.FpcModule.CharacterModelInstance.Fade > 0f)
			{
				return;
			}
			this._syncPos = reader.ReadRelativePosition();
			base.Player.Play(this._syncPos.Position, fpcStandardRoleBase.RoleColor);
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteReferenceHub(this._syncPlayer);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!reader.TryReadReferenceHub(out this._syncPlayer))
			{
				return;
			}
			this.ProcessRipple(this._syncPlayer);
		}

		public void ProcessRipple(ReferenceHub hub)
		{
			if (this._lastRipples.ContainsKey(hub.netId))
			{
				this._lastRipples[hub.netId] = SurfaceRippleTrigger.LastRippleInformation.Default;
				return;
			}
			this._lastRipples.Add(hub.netId, SurfaceRippleTrigger.LastRippleInformation.Default);
		}

		private void LateUpdate()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					Invisible invisible;
					if (fpcRole != null && (!referenceHub.playerEffectsController.TryGetEffect<Invisible>(out invisible) || !invisible.IsEnabled))
					{
						Vector3 position = fpcRole.FpcModule.Position;
						if (SurfaceRippleTrigger.IsOnSurface(fpcRole.FpcModule.Position))
						{
							SurfaceRippleTrigger.LastRippleInformation lastRippleInformation;
							if (this._lastRipples.TryGetValue(referenceHub.netId, out lastRippleInformation))
							{
								if (lastRippleInformation.IsNatural)
								{
									if (lastRippleInformation.Elapsed >= 20f)
									{
										goto IL_00DD;
									}
									goto IL_00D8;
								}
								else
								{
									if (lastRippleInformation.Elapsed < 10f)
									{
										goto IL_00D8;
									}
									goto IL_00DD;
								}
								IL_00E0:
								bool flag;
								if (!flag)
								{
									this._lastRipples[referenceHub.netId] = SurfaceRippleTrigger.LastRippleInformation.SurfaceDefault;
									this._syncPos = new RelativePosition(position);
									this._syncPlayer = referenceHub;
									base.ServerSendRpcToObservers();
									continue;
								}
								continue;
								IL_00DD:
								flag = false;
								goto IL_00E0;
								IL_00D8:
								flag = true;
								goto IL_00E0;
							}
							this._lastRipples.Add(referenceHub.netId, SurfaceRippleTrigger.LastRippleInformation.SurfaceDefault);
						}
					}
				}
			}
		}

		private static bool IsOnSurface(Vector3 position)
		{
			return position.y >= 900f;
		}

		private void OnPlayerPlayedRipple(ReferenceHub player)
		{
			this._syncPlayer = player;
			base.ClientSendCmd();
		}

		private const float TimeBetweenSurfaceRipples = 10f;

		private const float NaturalRippleCooldown = 20f;

		private readonly Dictionary<uint, SurfaceRippleTrigger.LastRippleInformation> _lastRipples = new Dictionary<uint, SurfaceRippleTrigger.LastRippleInformation>();

		private ReferenceHub _syncPlayer;

		private RelativePosition _syncPos;

		private struct LastRippleInformation
		{
			public static SurfaceRippleTrigger.LastRippleInformation Default
			{
				get
				{
					return new SurfaceRippleTrigger.LastRippleInformation
					{
						IsNatural = true,
						_time = NetworkTime.time
					};
				}
			}

			public static SurfaceRippleTrigger.LastRippleInformation SurfaceDefault
			{
				get
				{
					return new SurfaceRippleTrigger.LastRippleInformation
					{
						IsNatural = false,
						_time = NetworkTime.time
					};
				}
			}

			public float Elapsed
			{
				get
				{
					return (float)(NetworkTime.time - this._time);
				}
			}

			public bool IsNatural;

			private double _time;
		}
	}
}

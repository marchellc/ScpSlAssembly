using System;
using System.Collections.Generic;
using System.Diagnostics;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106StalkVisibilityController : StandardSubroutine<Scp106Role>
	{
		private void UpdateAll()
		{
			if (base.Owner.isLocalPlayer)
			{
				this.UpdateClient();
			}
			else if (base.Owner.IsLocallySpectated())
			{
				this.UpdateSpectator();
			}
			else if (this._anyFaded)
			{
				this.CleanupFade();
			}
			if (NetworkServer.active)
			{
				this.UpdateServer();
			}
		}

		private void UpdateClient()
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (HitboxIdentity.IsEnemy(base.Owner, referenceHub) && !(base.Owner == referenceHub))
				{
					FpcStandardRoleBase fpcStandardRoleBase = referenceHub.roleManager.CurrentRole as FpcStandardRoleBase;
					if (fpcStandardRoleBase != null)
					{
						float num = (this.GetVisibilityForPlayer(referenceHub, fpcStandardRoleBase) ? 11.5f : (-11.5f));
						FirstPersonMovementModule fpcModule = fpcStandardRoleBase.FpcModule;
						CharacterModel characterModelInstance = fpcModule.CharacterModelInstance;
						bool flag = characterModelInstance.Fade == 0f;
						characterModelInstance.Fade += num * Time.deltaTime;
						if (characterModelInstance.Fade < 1f)
						{
							this._affectedModels.Add(characterModelInstance);
						}
						else
						{
							this._affectedModels.Remove(characterModelInstance);
						}
						this._anyFaded = true;
						if (!NetworkServer.active)
						{
							if (characterModelInstance.Fade == 0f)
							{
								fpcModule.Position = Vector3.up * 8000f;
							}
							else if (flag || fpcModule.Motor.IsInvisible)
							{
								fpcModule.Position = fpcModule.Motor.ReceivedPosition.Position;
							}
						}
					}
				}
			}
		}

		private void UpdateSpectator()
		{
			this.RefreshDamageDictionary();
			this.UpdateClient();
		}

		private void CleanupFade()
		{
			this._affectedModels.ForEach(delegate(CharacterModel x)
			{
				x.Fade = 1f;
			});
			this._affectedModels.Clear();
			this._anyFaded = false;
		}

		private void UpdateServer()
		{
			if (!this._stalk.StalkActive)
			{
				return;
			}
			this.RefreshDamageDictionary();
			if (this._sendStopwatch.Elapsed.TotalSeconds < 0.07999999821186066)
			{
				return;
			}
			this._sendStopwatch.Restart();
			base.ServerSendRpc(false);
		}

		private bool GetVisibilityForPlayer(ReferenceHub hub, IFpcRole role)
		{
			FpcMotor motor = role.FpcModule.Motor;
			if (motor.IsInvisible)
			{
				return false;
			}
			if (!this._stalk.StalkActive || base.CastRole.Sinkhole.SubmergeProgress < 0.8f)
			{
				return true;
			}
			if (hub.playerEffectsController.GetEffect<Invigorated>().IsEnabled)
			{
				return false;
			}
			if (hub.playerEffectsController.GetEffect<Traumatized>().IsEnabled)
			{
				return true;
			}
			byte b;
			if (!this.SyncDamage.TryGetValue(hub.PlayerId, out b))
			{
				b = 0;
			}
			return Vector3.Distance(base.CastRole.FpcModule.Position, motor.ReceivedPosition.Position) < (float)b * 0.3f + 4f;
		}

		private void RefreshDamageDictionary()
		{
			this.SyncDamage.Clear();
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null)
					{
						if (referenceHub.playerEffectsController.GetEffect<Traumatized>().IsEnabled)
						{
							this.SyncDamage[referenceHub.PlayerId] = 0;
						}
						else
						{
							HealthStat module = referenceHub.playerStats.GetModule<HealthStat>();
							int num = Mathf.FloorToInt(module.MaxValue - module.CurValue);
							if (num != 0 && Vector3.Distance(fpcRole.FpcModule.Position, base.CastRole.FpcModule.Position) - 5f <= (float)num * 0.3f + 4f)
							{
								this.SyncDamage[referenceHub.PlayerId] = (byte)Mathf.Clamp(num, 0, 255);
							}
						}
					}
				}
			}
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp106StalkAbility>(out this._stalk);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteByte((byte)this.SyncDamage.Count);
			foreach (KeyValuePair<int, byte> keyValuePair in this.SyncDamage)
			{
				writer.WriteRecyclablePlayerId(new RecyclablePlayerId(keyValuePair.Key));
				writer.WriteByte(keyValuePair.Value);
			}
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this.SyncDamage.Clear();
			byte b = reader.ReadByte();
			for (int i = 0; i < (int)b; i++)
			{
				int value = reader.ReadRecyclablePlayerId().Value;
				this.SyncDamage[value] = reader.ReadByte();
			}
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			FirstPersonMovementModule.OnPositionUpdated += this.UpdateAll;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.CleanupFade();
			FirstPersonMovementModule.OnPositionUpdated -= this.UpdateAll;
		}

		private const float AbsoluteDistance = 4f;

		private const float HealthToDistance = 0.3f;

		private const float InvisibleHeight = 8000f;

		private const float TransitionSpeed = 11.5f;

		private const float ServerTolerance = 5f;

		private const float SendCooldown = 0.08f;

		private const float SubmergeTolerance = 0.8f;

		private Scp106StalkAbility _stalk;

		private bool _anyFaded;

		private readonly Stopwatch _sendStopwatch = Stopwatch.StartNew();

		private readonly HashSet<CharacterModel> _affectedModels = new HashSet<CharacterModel>();

		public readonly Dictionary<int, byte> SyncDamage = new Dictionary<int, byte>();
	}
}

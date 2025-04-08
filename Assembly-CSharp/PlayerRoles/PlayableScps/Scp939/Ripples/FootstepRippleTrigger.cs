using System;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps.Scp1507;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class FootstepRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Combine(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(this.OnFootstepPlayed));
			Scp1507Model.OnFootstepPlayed += this.OnFlamingoStep;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Remove(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(this.OnFootstepPlayed));
			Scp1507Model.OnFootstepPlayed -= this.OnFlamingoStep;
		}

		private void OnFlamingoStep(Scp1507Model flamingoModel)
		{
			this.OnFootstepPlayed(flamingoModel, 30f);
		}

		private void OnFootstepPlayed(CharacterModel model, float dis)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (!HitboxIdentity.IsEnemy(base.Owner, model.OwnerHub))
			{
				return;
			}
			IFpcRole fpcRole = model.OwnerHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			Invisible invisible;
			if (model.OwnerHub.playerEffectsController.TryGetEffect<Invisible>(out invisible) && invisible.IsEnabled)
			{
				return;
			}
			Vector3 position = base.CastRole.FpcModule.Position;
			Vector3 position2 = fpcRole.FpcModule.Position;
			if ((position - position2).sqrMagnitude > dis * dis)
			{
				return;
			}
			if (base.CheckVisibility(model.OwnerHub))
			{
				return;
			}
			this._syncPlayer = model.OwnerHub;
			this._syncPos = new RelativePosition(position2);
			this._syncDistance = (byte)Mathf.Min(dis, 255f);
			base.ServerSendRpcToObservers();
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteReferenceHub(this._syncPlayer);
			writer.WriteRelativePosition(this._syncPos);
			writer.WriteByte(this._syncDistance);
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
			this._syncPos = reader.ReadRelativePosition();
			this._syncDistance = reader.ReadByte();
			base.Player.Play(this._syncPos.Position, fpcStandardRoleBase.RoleColor);
			base.OnPlayedRipple(this._syncPlayer);
			AnimatedCharacterModel animatedCharacterModel = fpcStandardRoleBase.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
			if (animatedCharacterModel == null)
			{
				return;
			}
			AudioSourcePoolManager.PlayAtPosition(animatedCharacterModel.RandomFootstep, this._syncPos.Position, (float)this._syncDistance, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		private ReferenceHub _syncPlayer;

		private RelativePosition _syncPos;

		private byte _syncDistance;
	}
}

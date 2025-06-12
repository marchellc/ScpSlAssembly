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

namespace PlayerRoles.PlayableScps.Scp939.Ripples;

public class FootstepRippleTrigger : RippleTriggerBase
{
	private ReferenceHub _syncPlayer;

	private RelativePosition _syncPos;

	private byte _syncDistance;

	public override void SpawnObject()
	{
		base.SpawnObject();
		AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Combine(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(OnFootstepPlayed));
		Scp1507Model.OnFootstepPlayed += OnFlamingoStep;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		AnimatedCharacterModel.OnFootstepPlayed = (Action<AnimatedCharacterModel, float>)Delegate.Remove(AnimatedCharacterModel.OnFootstepPlayed, new Action<AnimatedCharacterModel, float>(OnFootstepPlayed));
		Scp1507Model.OnFootstepPlayed -= OnFlamingoStep;
	}

	private void OnFlamingoStep(Scp1507Model flamingoModel)
	{
		this.OnFootstepPlayed(flamingoModel, 30f);
	}

	private void OnFootstepPlayed(CharacterModel model, float dis)
	{
		if (NetworkServer.active && HitboxIdentity.IsEnemy(base.Owner, model.OwnerHub) && model.OwnerHub.roleManager.CurrentRole is IFpcRole fpcRole && (!model.OwnerHub.playerEffectsController.TryGetEffect<Invisible>(out var playerEffect) || !playerEffect.IsEnabled))
		{
			Vector3 position = base.CastRole.FpcModule.Position;
			Vector3 position2 = fpcRole.FpcModule.Position;
			if (!((position - position2).sqrMagnitude > dis * dis) && !base.CheckVisibility(model.OwnerHub))
			{
				this._syncPlayer = model.OwnerHub;
				this._syncPos = new RelativePosition(position2);
				this._syncDistance = (byte)Mathf.Min(dis, 255f);
				base.ServerSendRpcToObservers();
			}
		}
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
		if (reader.TryReadReferenceHub(out this._syncPlayer) && HitboxIdentity.IsEnemy(base.Owner, this._syncPlayer) && this._syncPlayer.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase)
		{
			this._syncPos = reader.ReadRelativePosition();
			this._syncDistance = reader.ReadByte();
			base.Player.Play(this._syncPos.Position, fpcStandardRoleBase.RoleColor);
			base.OnPlayedRipple(this._syncPlayer);
			if (fpcStandardRoleBase.FpcModule.CharacterModelInstance is AnimatedCharacterModel animatedCharacterModel)
			{
				AudioSourcePoolManager.PlayAtPosition(animatedCharacterModel.RandomFootstep, this._syncPos.Position, (int)this._syncDistance);
			}
		}
	}
}

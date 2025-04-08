using System;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507VocalizeAbility : KeySubroutine<Scp1507Role>
	{
		public static event Action<ReferenceHub> OnServerVocalize;

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.ToggleFlashlight;
			}
		}

		public void ServerScream()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Vector3 position = base.CastRole.FpcModule.Position;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null && fpcRole.SqrDistanceTo(position) <= 9f)
					{
						referenceHub.playerEffectsController.EnableEffect<Concussed>(2f, true);
					}
				}
			}
			Action<ReferenceHub> onServerVocalize = Scp1507VocalizeAbility.OnServerVocalize;
			if (onServerVocalize != null)
			{
				onServerVocalize(base.Owner);
			}
			base.ServerSendRpc(true);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this.Cooldown.IsReady)
			{
				return;
			}
			this.Cooldown.Trigger(5.0);
			this.ServerScream();
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			this.Cooldown.WriteCooldown(writer);
			writer.WriteRelativePosition(new RelativePosition(base.CastRole.FpcModule.Position));
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this.Cooldown.ReadCooldown(reader);
			AudioClip audioClip = ((base.Role.RoleTypeId == RoleTypeId.AlphaFlamingo) ? this._alphaSounds : this._regularSounds).RandomItem<AudioClip>();
			RelativePosition relativePosition = reader.ReadRelativePosition();
			if ((relativePosition.Position - base.CastRole.FpcModule.Position).sqrMagnitude <= 250f)
			{
				AudioSourcePoolManager.PlayOnTransform(audioClip, base.transform, 45f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			}
			else
			{
				AudioSourcePoolManager.PlayAtPosition(audioClip, relativePosition, 45f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			}
			Action onVocalized = this.OnVocalized;
			if (onVocalized == null)
			{
				return;
			}
			onVocalized();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.Cooldown.Clear();
		}

		protected override void OnKeyUp()
		{
			base.OnKeyUp();
			if (!this.Cooldown.IsReady)
			{
				return;
			}
			base.ClientSendCmd();
		}

		private const float ConcussionDuration = 2f;

		private const float ConcussionMaxDistanceSqr = 9f;

		private const float BaseCooldown = 5f;

		private const float HearableRange = 45f;

		private const float TrackingRangeSqr = 250f;

		public readonly AbilityCooldown Cooldown = new AbilityCooldown();

		public Action OnVocalized;

		[SerializeField]
		private AudioClip[] _alphaSounds;

		[SerializeField]
		private AudioClip[] _regularSounds;
	}
}

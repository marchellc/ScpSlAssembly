using System;
using System.Collections.Generic;
using PlayerRoles.Subroutines;
using PlayerRoles.Voice;
using UnityEngine;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class VoiceChatRippleTrigger : RippleTriggerBase
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			this._cooldowns.Clear();
			this._prevLoudness.Clear();
			this._radioCooldown.Clear();
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
		}

		private void Update()
		{
			if (!base.IsLocalOrSpectated)
			{
				return;
			}
			PlayerRolesUtils.ForEachRole<HumanRole>(new Action<HumanRole>(this.UpdateHuman));
			if (!this._radioCooldown.IsReady)
			{
				return;
			}
			this._radioCooldown.Trigger((double)this._radioCooldownDuration);
			foreach (SpatializedRadioPlaybackBase spatializedRadioPlaybackBase in SpatializedRadioPlaybackBase.AllInstances)
			{
				if (!spatializedRadioPlaybackBase.NoiseSource.mute)
				{
					base.PlayInRange(spatializedRadioPlaybackBase.LastPosition, spatializedRadioPlaybackBase.Source.maxDistance, Color.red);
				}
			}
		}

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			HumanRole humanRole = newRole as HumanRole;
			if (humanRole == null)
			{
				return;
			}
			this._prevLoudness[humanRole] = 0f;
		}

		private void UpdateHuman(HumanRole human)
		{
			HumanVoiceModule humanVoiceModule = human.VoiceModule as HumanVoiceModule;
			float num2;
			float num = (this._prevLoudness.TryGetValue(human, out num2) ? num2 : 0f);
			num = Mathf.Max(num, humanVoiceModule.ProximityPlayback.Loudness);
			if (num > this._minLoudness)
			{
				AbilityCooldown orAdd = this._cooldowns.GetOrAdd(human, () => new AbilityCooldown());
				float maxDistance = humanVoiceModule.ProximityPlayback.Source.maxDistance;
				Vector3 position = human.FpcModule.Position;
				if (orAdd.IsReady && (base.CastRole.FpcModule.Position - position).sqrMagnitude < maxDistance * maxDistance)
				{
					ReferenceHub referenceHub;
					if (!human.TryGetOwner(out referenceHub))
					{
						return;
					}
					if (base.CheckVisibility(referenceHub))
					{
						return;
					}
					base.Player.Play(human);
					ReferenceHub referenceHub2;
					if (human.TryGetOwner(out referenceHub2))
					{
						base.OnPlayedRipple(referenceHub2);
					}
					orAdd.Trigger((double)this._cooldownPerLoudness.Evaluate(num));
				}
			}
			num -= Time.deltaTime * this._loudnessDecayRate;
			this._prevLoudness[human] = Mathf.Max(0f, num);
		}

		private readonly AbilityCooldown _radioCooldown = new AbilityCooldown();

		private readonly Dictionary<HumanRole, AbilityCooldown> _cooldowns = new Dictionary<HumanRole, AbilityCooldown>();

		private readonly Dictionary<HumanRole, float> _prevLoudness = new Dictionary<HumanRole, float>();

		[SerializeField]
		private AnimationCurve _cooldownPerLoudness;

		[SerializeField]
		private float _minLoudness;

		[SerializeField]
		private float _radioCooldownDuration;

		[SerializeField]
		private float _loudnessDecayRate;
	}
}

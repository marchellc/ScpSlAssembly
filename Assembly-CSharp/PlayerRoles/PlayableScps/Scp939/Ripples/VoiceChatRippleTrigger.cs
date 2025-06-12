using System.Collections.Generic;
using PlayerRoles.Subroutines;
using PlayerRoles.Voice;
using UnityEngine;
using VoiceChat.Playbacks;

namespace PlayerRoles.PlayableScps.Scp939.Ripples;

public class VoiceChatRippleTrigger : RippleTriggerBase
{
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

	public override void SpawnObject()
	{
		base.SpawnObject();
		this._cooldowns.Clear();
		this._prevLoudness.Clear();
		this._radioCooldown.Clear();
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
	}

	private void Update()
	{
		if (!base.IsLocalOrSpectated)
		{
			return;
		}
		PlayerRolesUtils.ForEachRole<HumanRole>(UpdateHuman);
		if (!this._radioCooldown.IsReady)
		{
			return;
		}
		this._radioCooldown.Trigger(this._radioCooldownDuration);
		foreach (SpatializedRadioPlaybackBase allInstance in SpatializedRadioPlaybackBase.AllInstances)
		{
			if (!allInstance.NoiseSource.mute)
			{
				base.PlayInRange(allInstance.LastPosition, allInstance.Source.maxDistance, Color.red);
			}
		}
	}

	private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (newRole is HumanRole key)
		{
			this._prevLoudness[key] = 0f;
		}
	}

	private void UpdateHuman(HumanRole human)
	{
		HumanVoiceModule humanVoiceModule = human.VoiceModule as HumanVoiceModule;
		float a = (this._prevLoudness.TryGetValue(human, out var value) ? value : 0f);
		a = Mathf.Max(a, humanVoiceModule.FirstProxPlayback.Loudness);
		if (a > this._minLoudness)
		{
			AbilityCooldown orAdd = this._cooldowns.GetOrAdd(human, () => new AbilityCooldown());
			float maxDistance = humanVoiceModule.FirstProxPlayback.Source.maxDistance;
			Vector3 position = human.FpcModule.Position;
			if (orAdd.IsReady && (base.CastRole.FpcModule.Position - position).sqrMagnitude < maxDistance * maxDistance)
			{
				if (!human.TryGetOwner(out var hub) || base.CheckVisibility(hub))
				{
					return;
				}
				base.Player.Play(human);
				if (human.TryGetOwner(out var hub2))
				{
					base.OnPlayedRipple(hub2);
				}
				orAdd.Trigger(this._cooldownPerLoudness.Evaluate(a));
			}
		}
		a -= Time.deltaTime * this._loudnessDecayRate;
		this._prevLoudness[human] = Mathf.Max(0f, a);
	}
}

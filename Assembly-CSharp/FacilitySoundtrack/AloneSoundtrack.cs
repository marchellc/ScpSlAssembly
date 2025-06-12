using PlayerRoles;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace FacilitySoundtrack;

public class AloneSoundtrack : SoundtrackLayerBase
{
	private const float AloneTime = 120f;

	private const float SCPBufferTime = 30f;

	private const float MaxVisionDistance = 60f;

	private const float CharacterRadius = 0.25f;

	[SerializeField]
	private float _fadeInSpeed;

	[SerializeField]
	private float _fadeOutSpeed;

	[SerializeField]
	private float _maxVolume;

	[SerializeField]
	private AudioSource _aloneSource;

	private float _weight;

	private float _aloneTime;

	private float _oldAloneTime;

	private bool _hasFoundSCP;

	public override bool Additive => false;

	public override float Weight => this._weight;

	public override void UpdateVolume(float masterScale)
	{
		if (ReferenceHub.TryGetLocalHub(out var hub) && (!hub.IsHuman() || hub.GetRoleId() == RoleTypeId.Tutorial))
		{
			this._weight = 0f;
			this.SetVolume(this._weight);
			return;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.isLocalPlayer || !allHub.IsAlive() || allHub.GetRoleId() == RoleTypeId.Tutorial)
			{
				continue;
			}
			flag3 |= HitboxIdentity.IsEnemy(hub, allHub);
			if (VisionInformation.GetVisionInformation(hub, hub.PlayerCameraReference, allHub.PlayerCameraReference.position, 0.25f, 60f).IsLooking)
			{
				if (!allHub.IsSCP())
				{
					flag = true;
					break;
				}
				flag2 = true;
			}
		}
		if (!flag && this._hasFoundSCP != flag2)
		{
			this._hasFoundSCP = flag2;
			if (this._oldAloneTime != 0f)
			{
				this._aloneTime = ((90f < this._aloneTime) ? 90f : this._oldAloneTime);
			}
			this._oldAloneTime = (flag2 ? this._aloneTime : 0f);
		}
		this._aloneTime = (flag ? 0f : (this._aloneTime + Time.deltaTime));
		float num = ((!base.IsPovMuted && this._oldAloneTime == 0f && this._aloneTime > 120f && flag3) ? this._fadeInSpeed : (0f - this._fadeOutSpeed));
		this._weight = Mathf.Clamp01(this._weight + num * Time.deltaTime);
		this.SetVolume(this._weight * masterScale * this._maxVolume);
	}

	private void SetVolume(float volume)
	{
		this._aloneSource.volume = volume;
	}

	private void Start()
	{
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private void OnDestroy()
	{
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (userHub.isLocalPlayer && newRole.Team != Team.Dead)
		{
			this._weight = 0f;
			this._aloneTime = 0f;
			this._oldAloneTime = 0f;
			this._hasFoundSCP = false;
		}
	}
}

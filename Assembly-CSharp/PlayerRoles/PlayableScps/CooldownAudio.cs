using AudioPooling;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps;

public class CooldownAudio : MonoBehaviour
{
	public double Cooldown;

	[SerializeField]
	private AudioClip _cooldownAudio;

	[SerializeField]
	private PlayerRoleBase _player;

	private double _lastTime;

	public void PlayAudio()
	{
		if (!(NetworkTime.time < this._lastTime))
		{
			this._lastTime = NetworkTime.time + this.Cooldown;
			AudioSourcePoolManager.Play2DWithParent(this._cooldownAudio, this._player.transform);
		}
	}
}

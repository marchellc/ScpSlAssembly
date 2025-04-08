using System;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173TeleportIndicator : MonoBehaviour
	{
		private void Awake()
		{
			this._subroutineManager.TryGetSubroutine<Scp173TeleportAbility>(out this._teleportAbility);
			this._subroutineManager.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out this._breakneckSpeedsAbility);
		}

		private void Update()
		{
			this._soundSource.volume = Mathf.MoveTowards(this._soundSource.volume, this._targetVolume, this._volumeAdjustmentSpeed * Time.deltaTime);
		}

		private void SetupVisiblity(bool normal = false, bool kill = false, bool neutral = false)
		{
			this._normalIndicator.SetActive(normal);
			this._killIndicator.SetActive(kill);
			this._neutralIndicator.SetActive(neutral);
		}

		public void UpdateVisibility(bool isVisible)
		{
			this._targetVolume = (float)(isVisible ? 1 : 0);
			if (!isVisible)
			{
				this.SetupVisiblity(false, false, false);
				return;
			}
			if (this._breakneckSpeedsAbility.IsActive)
			{
				this.SetupVisiblity(false, false, true);
				return;
			}
			bool flag = this._teleportAbility.BestTarget != null;
			this.SetupVisiblity(!flag, flag, false);
		}

		[SerializeField]
		private float _volumeAdjustmentSpeed;

		[SerializeField]
		private AudioSource _soundSource;

		[SerializeField]
		private GameObject _normalIndicator;

		[SerializeField]
		private GameObject _killIndicator;

		[SerializeField]
		private GameObject _neutralIndicator;

		[SerializeField]
		private SubroutineManagerModule _subroutineManager;

		private float _targetVolume;

		private Scp173TeleportAbility _teleportAbility;

		private Scp173BreakneckSpeedsAbility _breakneckSpeedsAbility;
	}
}

using System;
using Mirror;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939BreathController : StandardSubroutine<Scp939Role>
	{
		public override void SpawnObject()
		{
			base.SpawnObject();
			this.RefreshPerspective();
			this.ForEachLoop(delegate(Scp939BreathController.IdleLoop939 x)
			{
				x.SetVolume(0f, 1f);
			});
			this._stamina = base.Owner.playerStats.GetModule<StaminaStat>();
			SpectatorTargetTracker.OnTargetChanged += this.RefreshPerspective;
			if (!NetworkServer.active)
			{
				return;
			}
			this._stamina.ChangeSyncMode(SyncedStatBase.SyncMode.Public);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._curExhaustion = 0f;
			SpectatorTargetTracker.OnTargetChanged -= this.RefreshPerspective;
		}

		protected override void Awake()
		{
			base.Awake();
			base.GetSubroutine<Scp939FocusAbility>(out this._focus);
		}

		private void RefreshPerspective()
		{
			bool isLocal = base.Owner.IsLocallySpectated() || base.Owner.isLocalPlayer;
			this.ForEachLoop(delegate(Scp939BreathController.IdleLoop939 x)
			{
				x.SetOwner(isLocal);
			});
		}

		private void ForEachLoop(Action<Scp939BreathController.IdleLoop939> action)
		{
			action(this._focusLoop);
			action(this._breathLoop);
			action(this._exhaustionLoop);
			action(this._focusGrowlLoop);
		}

		private void Update()
		{
			float num = Mathf.Clamp01(1f - this._stamina.CurValue);
			this._curExhaustion = Mathf.Lerp(this._curExhaustion, num, Time.deltaTime * ((num > this._curExhaustion) ? this._exhaustionGainLerp : this._exhaustionDropLerp));
			this._exhaustionLoop.SetVolume(this._exhaustionVolume.Evaluate(this._curExhaustion), 1f);
			bool flag = this._curExhaustion > this._exhaustionMuteLoopsThreshold;
			bool flag2 = !flag && this._focus.TargetState;
			if (this._focus.TargetState)
			{
				this._timeFromLastFocus = 0f;
			}
			else
			{
				this._timeFromLastFocus += Time.deltaTime;
			}
			if (this._timeFromLastFocus == 0f || this._timeFromLastFocus > this._dropFocusAfter)
			{
				this._focusGrowlLoop.SetVolume(flag2, Time.deltaTime * (flag2 ? this._focusGrowlGainLerp : this._focusGrowlDropLerp));
			}
			this._focusLoop.SetVolume(flag2, Time.deltaTime * this._breathLerp);
			this._breathLoop.SetVolume(!flag && !this._focus.TargetState, Time.deltaTime * this._breathLerp);
		}

		[SerializeField]
		private float _exhaustionGainLerp;

		[SerializeField]
		private float _exhaustionDropLerp;

		[SerializeField]
		private float _exhaustionMuteLoopsThreshold;

		[SerializeField]
		private AnimationCurve _exhaustionVolume;

		[SerializeField]
		private float _breathLerp;

		[SerializeField]
		private float _focusGrowlGainLerp;

		[SerializeField]
		private float _focusGrowlDropLerp;

		private float _timeFromLastFocus;

		[SerializeField]
		private float _dropFocusAfter = 5f;

		[SerializeField]
		private Scp939BreathController.IdleLoop939 _focusLoop;

		[SerializeField]
		private Scp939BreathController.IdleLoop939 _breathLoop;

		[SerializeField]
		private Scp939BreathController.IdleLoop939 _exhaustionLoop;

		[SerializeField]
		private Scp939BreathController.IdleLoop939 _focusGrowlLoop;

		private float _curExhaustion;

		private StaminaStat _stamina;

		private Scp939FocusAbility _focus;

		[Serializable]
		private class IdleLoop939
		{
			public float CurVolume { get; private set; }

			public void SetVolume(bool isOn, float lerp)
			{
				this.SetVolume((float)(isOn ? 1 : 0), lerp);
			}

			public void SetOwner(bool isLocalPlayer)
			{
				this._local = isLocalPlayer;
				this.SetVolume(this.CurVolume, 1f);
			}

			public void SetVolume(float vol, float lerp = 1f)
			{
				if (!this._cacheSet)
				{
					this._has3rd = this._thirdperson != null;
					this._has1st = this._firstperson != null;
					this._cacheSet = true;
				}
				this.CurVolume = Mathf.Lerp(this.CurVolume, vol, lerp);
				if (this._has3rd)
				{
					this._thirdperson.volume = (this._local ? 0f : (this.CurVolume * this._thirdpersonVolume));
				}
				if (this._has1st)
				{
					this._firstperson.volume = (this._local ? (this.CurVolume * this._firstPersonVolume) : 0f);
				}
			}

			[SerializeField]
			private AudioSource _thirdperson;

			[SerializeField]
			[Range(0f, 1f)]
			private float _thirdpersonVolume = 1f;

			[SerializeField]
			private AudioSource _firstperson;

			[SerializeField]
			[Range(0f, 1f)]
			private float _firstPersonVolume = 1f;

			private bool _cacheSet;

			private bool _has3rd;

			private bool _has1st;

			private bool _local;
		}
	}
}

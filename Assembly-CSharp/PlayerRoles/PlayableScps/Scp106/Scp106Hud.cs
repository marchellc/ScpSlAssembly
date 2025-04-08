using System;
using System.Diagnostics;
using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106Hud : ScpHudBase
	{
		private static float CurTime
		{
			get
			{
				return Time.timeSinceLevelLoad;
			}
		}

		private void LateUpdate()
		{
			this._sinkholeCooldown.Update(false);
			this._attackCooldownElement.Update(false);
		}

		private void UpdateFlash(Graphic targetGraphic, Stopwatch sw, Color normalColor, ref float idleTime)
		{
			Color color;
			if (sw.IsRunning && sw.Elapsed.TotalSeconds < (double)this._flashDuration)
			{
				float num = Mathf.Sin((Scp106Hud.CurTime - idleTime) * this._flashSpeed * 3.1415927f);
				color = Color.Lerp(normalColor, Color.red, Mathf.Abs(num));
			}
			else
			{
				color = Color.Lerp(targetGraphic.color, normalColor, Time.deltaTime * this._flashSpeed);
				idleTime = Scp106Hud.CurTime;
			}
			targetGraphic.color = new Color(color.r, color.g, color.b, targetGraphic.color.a);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (this != Scp106Hud._singleton)
			{
				return;
			}
			Scp106Hud._singletonSet = false;
		}

		internal override void OnDied()
		{
			base.enabled = false;
			this._diedRoot.SetActive(false);
		}

		internal override void Init(ReferenceHub hub)
		{
			base.Init(hub);
			this._role = hub.roleManager.CurrentRole as Scp106Role;
			this._fpc = this._role.FpcModule as Scp106MovementModule;
			this._role.SubroutineModule.TryGetSubroutine<Scp106SinkholeController>(out this._sinkholeController);
			this._attackCooldownElement.Setup(this._attackCooldown, null);
			this._sinkholeCooldown.Setup(this._sinkholeController.ReadonlyCooldown, null);
			Scp106Hud._singleton = this;
			Scp106Hud._singletonSet = true;
		}

		public static void PlayCooldownAnimation(double nextTime)
		{
			if (!Scp106Hud._singletonSet)
			{
				return;
			}
			float num = (float)(nextTime - NetworkTime.time);
			Scp106Hud._singleton._attackCooldown.Trigger((double)num);
		}

		public static void PlayFlash(bool vigor)
		{
			if (!Scp106Hud._singletonSet)
			{
				return;
			}
			(vigor ? Scp106Hud._singleton._vigorFlashSw : Scp106Hud._singleton._cooldownFlashSw).Restart();
		}

		public static void SetDissolveAnimation(float amt)
		{
		}

		private readonly Stopwatch _vigorFlashSw = new Stopwatch();

		private readonly Stopwatch _cooldownFlashSw = new Stopwatch();

		[SerializeField]
		private AbilityHud _sinkholeCooldown;

		[SerializeField]
		private Graphic _cooldownFlasher;

		[SerializeField]
		private AbilityHud _attackCooldownElement;

		[SerializeField]
		private Color _normalColor;

		[SerializeField]
		private float _flashSpeed;

		[SerializeField]
		private float _flashDuration;

		[SerializeField]
		private GameObject _diedRoot;

		private Scp106Role _role;

		private Scp106MovementModule _fpc;

		private Scp106SinkholeController _sinkholeController;

		private float _vigorIdleTime;

		private float _cooldownIdleTime;

		private readonly AbilityCooldown _attackCooldown = new AbilityCooldown();

		private static Scp106Hud _singleton;

		private static bool _singletonSet;
	}
}

using System;
using System.Diagnostics;
using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp106;

public class Scp106Hud : ScpHudBase
{
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

	private static float CurTime => Time.timeSinceLevelLoad;

	private void LateUpdate()
	{
		_sinkholeCooldown.Update();
		_attackCooldownElement.Update();
	}

	private void UpdateFlash(Graphic targetGraphic, Stopwatch sw, Color normalColor, ref float idleTime)
	{
		Color color;
		if (sw.IsRunning && sw.Elapsed.TotalSeconds < (double)_flashDuration)
		{
			float f = Mathf.Sin((CurTime - idleTime) * _flashSpeed * MathF.PI);
			color = Color.Lerp(normalColor, Color.red, Mathf.Abs(f));
		}
		else
		{
			color = Color.Lerp(targetGraphic.color, normalColor, Time.deltaTime * _flashSpeed);
			idleTime = CurTime;
		}
		targetGraphic.color = new Color(color.r, color.g, color.b, targetGraphic.color.a);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (!(this != _singleton))
		{
			_singletonSet = false;
		}
	}

	internal override void OnDied()
	{
		base.enabled = false;
		_diedRoot.SetActive(value: false);
	}

	internal override void Init(ReferenceHub hub)
	{
		base.Init(hub);
		_role = hub.roleManager.CurrentRole as Scp106Role;
		_fpc = _role.FpcModule as Scp106MovementModule;
		_role.SubroutineModule.TryGetSubroutine<Scp106SinkholeController>(out _sinkholeController);
		_attackCooldownElement.Setup(_attackCooldown, null);
		_sinkholeCooldown.Setup(_sinkholeController.ReadonlyCooldown, null);
		_singleton = this;
		_singletonSet = true;
	}

	public static void PlayCooldownAnimation(double nextTime)
	{
		if (_singletonSet)
		{
			float num = (float)(nextTime - NetworkTime.time);
			_singleton._attackCooldown.Trigger(num);
		}
	}

	public static void PlayFlash(bool vigor)
	{
		if (_singletonSet)
		{
			(vigor ? _singleton._vigorFlashSw : _singleton._cooldownFlashSw).Restart();
		}
	}

	public static void SetDissolveAnimation(float amt)
	{
	}
}

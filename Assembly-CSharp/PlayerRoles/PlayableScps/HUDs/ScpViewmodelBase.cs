using System;
using CameraShaking;
using InventorySystem.Items;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HUDs;

public abstract class ScpViewmodelBase : MonoBehaviour
{
	private bool _destroyed;

	private GoopSway _sway;

	[SerializeField]
	private Vector3 _localRotation;

	[SerializeField]
	private Vector3 _trackerOffset;

	[SerializeField]
	private Transform _cameraBone;

	[SerializeField]
	private GoopSway.GoopSwaySettings _swaySettings;

	public abstract float CamFOV { get; }

	protected ScpHudBase Hud { get; private set; }

	protected ReferenceHub Owner { get; private set; }

	protected PlayerRoleBase Role { get; private set; }

	[field: SerializeField]
	protected Animator Anim { get; private set; }

	protected virtual void Start()
	{
		ScpHudBase component = null;
		Transform parent = base.transform;
		while (!parent.TryGetComponent<ScpHudBase>(out component))
		{
			parent = parent.parent;
			if (parent == null)
			{
				throw new NullReferenceException(string.Format("{0} failed to get component {1} for game object {2}", this, "ScpHudBase", base.name));
			}
		}
		this.Hud = component;
		this.Owner = this.Hud.Hub;
		this.Role = this.Owner.roleManager.CurrentRole;
		Transform obj = base.transform;
		Transform transform = SharedHandsController.Singleton.transform;
		obj.SetParent(transform);
		obj.localScale = Vector3.one;
		obj.localEulerAngles = this._localRotation;
		obj.position += transform.position - this._cameraBone.position;
		this.Hud.OnDestroyed += DestroySelf;
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
		this._sway = new GoopSway(this._swaySettings, this.Owner);
		CameraShakeController.AddEffect(new TrackerShake(this._cameraBone, this._trackerOffset));
	}

	protected virtual void SkipAnimations(float totalTime, int steps = 3)
	{
		totalTime -= Time.deltaTime;
		this.Anim.Update(Time.deltaTime);
		this.UpdateAnimations();
		totalTime /= (float)steps;
		for (int i = 0; i < steps; i++)
		{
			this.Anim.Update(totalTime);
		}
	}

	protected virtual void OnDestroy()
	{
		this._destroyed = true;
		if (this.Hud != null)
		{
			this.Hud.OnDestroyed -= DestroySelf;
		}
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
	}

	protected virtual void LateUpdate()
	{
		this.UpdateAnimations();
		this._sway.UpdateSway();
	}

	protected abstract void UpdateAnimations();

	private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
	{
		if (!(hub != this.Owner))
		{
			this.DestroySelf();
		}
	}

	private void DestroySelf()
	{
		if (!this._destroyed)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			this._destroyed = true;
		}
	}
}

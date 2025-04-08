using System;
using CameraShaking;
using InventorySystem.Items;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HUDs
{
	public abstract class ScpViewmodelBase : MonoBehaviour
	{
		public abstract float CamFOV { get; }

		private protected ScpHudBase Hud { protected get; private set; }

		private protected ReferenceHub Owner { protected get; private set; }

		private protected PlayerRoleBase Role { protected get; private set; }

		private protected Animator Anim { protected get; private set; }

		protected virtual void Start()
		{
			ScpHudBase scpHudBase = null;
			Transform transform = base.transform;
			while (!transform.TryGetComponent<ScpHudBase>(out scpHudBase))
			{
				transform = transform.parent;
				if (transform == null)
				{
					throw new NullReferenceException(string.Format("{0} failed to get component {1} for game object {2}", this, "ScpHudBase", base.name));
				}
			}
			this.Hud = scpHudBase;
			this.Owner = this.Hud.Hub;
			this.Role = this.Owner.roleManager.CurrentRole;
			Transform transform2 = base.transform;
			Transform transform3 = SharedHandsController.Singleton.transform;
			transform2.SetParent(transform3);
			transform2.localScale = Vector3.one;
			transform2.localEulerAngles = this._localRotation;
			transform2.position += transform3.position - this._cameraBone.position;
			this.Hud.OnDestroyed += this.DestroySelf;
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			this._sway = new GoopSway(this._swaySettings, this.Owner);
			CameraShakeController.AddEffect(new TrackerShake(this._cameraBone, this._trackerOffset, 1f));
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
				this.Hud.OnDestroyed -= this.DestroySelf;
			}
			PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
		}

		protected virtual void LateUpdate()
		{
			this.UpdateAnimations();
			this._sway.UpdateSway();
		}

		protected abstract void UpdateAnimations();

		private void OnRoleChanged(ReferenceHub hub, PlayerRoleBase oldRole, PlayerRoleBase newRole)
		{
			if (hub != this.Owner)
			{
				return;
			}
			this.DestroySelf();
		}

		private void DestroySelf()
		{
			if (this._destroyed)
			{
				return;
			}
			global::UnityEngine.Object.Destroy(base.gameObject);
			this._destroyed = true;
		}

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
	}
}

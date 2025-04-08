using System;
using Mirror;
using PlayerRoles;
using RemoteAdmin.Interfaces;

namespace CustomPlayerEffects
{
	public class InsufficientLighting : StatusEffectBase, ICustomRADisplay
	{
		private PlayerRoleBase CurRole
		{
			get
			{
				return base.Hub.roleManager.CurrentRole;
			}
		}

		public string DisplayName { get; }

		public bool CanBeDisplayed { get; }

		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Technical;
			}
		}

		public static float DefaultIntensity
		{
			get
			{
				return 0f;
			}
		}

		internal override void OnRoleChanged(PlayerRoleBase previousRole, PlayerRoleBase newRole)
		{
			base.OnRoleChanged(previousRole, newRole);
			this._prevTarget = false;
		}

		protected override void Start()
		{
			base.Start();
			StaticUnityMethods.OnUpdate += this.AlwaysUpdate;
		}

		private void OnDestroy()
		{
			StaticUnityMethods.OnUpdate -= this.AlwaysUpdate;
		}

		private void AlwaysUpdate()
		{
			if (NetworkServer.active)
			{
				this.UpdateServer();
			}
		}

		private void UpdateServer()
		{
			IAmbientLightRole ambientLightRole = this.CurRole as IAmbientLightRole;
			bool flag = ambientLightRole != null && ambientLightRole.InsufficientLight;
			if (flag == this._prevTarget)
			{
				return;
			}
			base.Intensity = (flag ? 1 : 0);
			this._prevTarget = flag;
		}

		private bool _prevTarget;

		private const float NoLightsAmbient = 0.03f;
	}
}

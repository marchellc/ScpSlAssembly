using System;
using System.Collections.Generic;
using AudioPooling;
using InventorySystem.Items.Usables.Scp1344;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using PlayerRoles.PlayableScps;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Scp939;
using PlayerRoles.Visibility;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace CustomPlayerEffects
{
	public class Scp1344 : StatusEffectBase, ICustomRADisplay
	{
		public static event Action<ReferenceHub, ReferenceHub> OnPlayerSeen;

		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Mixed;
			}
		}

		public string DisplayName
		{
			get
			{
				return "SCP-1344";
			}
		}

		public bool CanBeDisplayed
		{
			get
			{
				return true;
			}
		}

		private static bool IsOnSurface(Vector3 position)
		{
			return position.y >= 900f;
		}

		protected override void Start()
		{
			this.DisableEffect();
		}

		protected override void Enabled()
		{
			if (NetworkServer.active)
			{
				base.Hub.EnableWearables(WearableElements.Scp1344Goggles);
			}
			this._wasEverActive = true;
			this.PlaySound();
			if (!base.IsLocalPlayer && !base.IsSpectated)
			{
				return;
			}
			this.SetVisionStatus(true);
		}

		protected override void Disabled()
		{
			if (NetworkServer.active)
			{
				base.Hub.DisableWearables(WearableElements.Scp1344Goggles);
			}
			if (!this._wasEverActive || (!base.IsLocalPlayer && !base.IsSpectated))
			{
				return;
			}
			this.SetVisionStatus(false);
		}

		public override void OnBeginSpectating()
		{
			base.OnBeginSpectating();
			this.SetVisionStatus(true);
		}

		public override void OnStopSpectating()
		{
			base.OnStopSpectating();
			this.SetVisionStatus(false);
			this.CloseSessions();
		}

		public void PlayDetectionSound()
		{
			AudioSourcePoolManager.Play2D(this._sfxDetected, 1f, MixerChannel.NoDucking, 1f);
		}

		public void PlayBuildupSound()
		{
			AudioSourcePoolManager.PlayOnTransform(this._sfxBuildupDiegetic, base.Hub.transform, 10f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		private void CloseSessions()
		{
			if (!this._enableSoundSession.SameSession)
			{
				return;
			}
			this._enableSoundSession.Source.Stop();
		}

		private void PlaySound()
		{
			AudioSourcePoolManager.PlayOnTransform(this._sfxEnable, base.Hub.transform, 10f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			if (!base.IsLocalPlayer && !base.IsSpectated)
			{
				return;
			}
			this._enableSoundSession = new AudioPoolSession(AudioSourcePoolManager.Play2D(this._sfxEnableNonDiegetic, 1f, MixerChannel.NoDucking, 1f));
		}

		protected override void OnEffectUpdate()
		{
			base.OnEffectUpdate();
			if (!Scp1344._visionEnabled)
			{
				return;
			}
			if (!base.IsLocalPlayer && !base.IsSpectated)
			{
				return;
			}
			this.UpdateEnabled();
		}

		private void SetVisionStatus(bool isEnabled)
		{
			if (Scp1344._visionEnabled == isEnabled)
			{
				return;
			}
			Scp1344._visionEnabled = isEnabled;
			if (!isEnabled)
			{
				PlayerRoleManager.OnRoleChanged -= this.OnRoleChanged;
				ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerRemoved));
				this.CleanupAll();
				return;
			}
			FpcStandardRoleBase fpcStandardRoleBase = base.Hub.roleManager.CurrentRole as FpcStandardRoleBase;
			if (fpcStandardRoleBase == null)
			{
				Scp1344._visionEnabled = false;
				return;
			}
			Scp1344._currentRole = fpcStandardRoleBase;
			PlayerRoleManager.OnRoleChanged += this.OnRoleChanged;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerRemoved));
			this.SetupAll();
		}

		private bool IsInVision(ReferenceHub target, FpcStandardRoleBase fpcRole, Vector3 targetPosition, float maxDistance, out bool inSight, out float targetDistance)
		{
			VisionInformation visionInformation = VisionInformation.GetVisionInformation(base.Hub, base.Hub.PlayerCameraReference, targetPosition, fpcRole.FpcModule.CharacterControllerSettings.Radius, 0f, false, true, 0, true);
			targetDistance = visionInformation.Distance;
			inSight = visionInformation.IsLooking;
			if (inSight)
			{
				if (base.IsLocalPlayer && !Scp1344.LastSeen.ContainsKey(target.netId))
				{
					NetworkClient.Send<Scp1344DetectionMessage>(new Scp1344DetectionMessage(target.netId), 0);
				}
				Scp1344.LastSeen[target.netId] = Time.time;
				return true;
			}
			if (targetDistance < maxDistance)
			{
				return true;
			}
			float num;
			if (!Scp1344.LastSeen.TryGetValue(target.netId, out num))
			{
				return false;
			}
			if (Time.time - num < 23f)
			{
				inSight = true;
				return true;
			}
			Scp1344.LastSeen.Remove(target.netId);
			return false;
		}

		private Color GetParticleColor(ReferenceHub hub, FpcStandardRoleBase fpc)
		{
			Invisible invisible;
			if (hub.playerEffectsController.TryGetEffect<Invisible>(out invisible) && invisible.IsEnabled)
			{
				return this._invisiblePlayerColor;
			}
			return fpc.RoleColor;
		}

		private float GetParticleScale(FpcStandardRoleBase role)
		{
			if (role.Team == Team.SCPs && role.RoleTypeId != RoleTypeId.Scp0492)
			{
				return 2.5f;
			}
			if (Scp1344._currentRole.Team == role.Team)
			{
				return 1f;
			}
			return 2f;
		}

		private Vector3 GetParticlePosition(FpcStandardRoleBase role, Vector3 targetPosition)
		{
			Scp106Role scp106Role = role as Scp106Role;
			if (scp106Role != null && scp106Role.IsStalking)
			{
				return targetPosition - new Vector3(0f, 0.8f);
			}
			return targetPosition + new Vector3(0f, 0.7f);
		}

		private void UpdateEnabled()
		{
			PlayerStats targetStats = Scp1344._currentRole.TargetStats;
			HealthStat healthStat;
			if (!targetStats || !targetStats.TryGetModule<HealthStat>(out healthStat))
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (!referenceHub.isLocalPlayer)
				{
					FpcStandardRoleBase fpcStandardRoleBase = referenceHub.roleManager.CurrentRole as FpcStandardRoleBase;
					ParticleSystem particleSystem;
					if (fpcStandardRoleBase != null && Scp1344.Trackers.TryGetValue(fpcStandardRoleBase, out particleSystem))
					{
						ParticleSystem.EmissionModule emission = particleSystem.emission;
						bool isInvisible = fpcStandardRoleBase.FpcModule.Motor.IsInvisible;
						Scp939Model scp939Model = fpcStandardRoleBase.FpcModule.CharacterModelInstance as Scp939Model;
						bool flag = scp939Model != null && scp939Model.IsInHiddenPosition;
						Vector3 vector = ((isInvisible || flag) ? fpcStandardRoleBase.FpcModule.Motor.ReceivedPosition.Position : fpcStandardRoleBase.FpcModule.Position);
						float num = (Scp1344.IsOnSurface(Scp1344._currentRole.FpcModule.Position) ? 36f : 18f);
						bool flag2;
						float num2;
						if (this.IsInVision(referenceHub, fpcStandardRoleBase, vector, num + 5f, out flag2, out num2))
						{
							Action<ReferenceHub, ReferenceHub> onPlayerSeen = Scp1344.OnPlayerSeen;
							if (onPlayerSeen != null)
							{
								onPlayerSeen(base.Hub, referenceHub);
							}
							emission.enabled = true;
							Transform transform = particleSystem.transform;
							transform.position = this.GetParticlePosition(fpcStandardRoleBase, vector);
							transform.localScale = this.GetParticleScale(fpcStandardRoleBase) * Vector3.one;
							ParticleSystem.MainModule main = particleSystem.main;
							Color color = Mathf.Clamp(healthStat.NormalizedValue, 0.2f, 1f) * this.GetParticleColor(referenceHub, fpcStandardRoleBase);
							if (!flag2 && num < num2)
							{
								color.a = Mathf.Clamp01((num + 5f - num2) / 5f);
							}
							main.startColor = color;
						}
						else
						{
							emission.enabled = false;
						}
					}
				}
			}
		}

		private void SetupAll()
		{
			PlayerRolesUtils.ForEachRole<PlayerRoleBase>(new Action<PlayerRoleBase>(this.RegisterRole));
		}

		private void CleanupAll()
		{
			Scp1344.Trackers.ForEachValue(delegate(ParticleSystem x)
			{
				global::UnityEngine.Object.Destroy(x.gameObject);
			});
			Scp1344.Trackers.Clear();
			Scp1344.LastSeen.Clear();
		}

		private void RegisterRole(PlayerRoleBase prb)
		{
			ReferenceHub referenceHub;
			if (prb.TryGetOwner(out referenceHub) && (referenceHub.isLocalPlayer || referenceHub == base.Hub))
			{
				return;
			}
			IFpcRole fpcRole = prb as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			if (Scp1344.Trackers.ContainsKey(fpcRole))
			{
				return;
			}
			Scp1344.Trackers.Add(fpcRole, global::UnityEngine.Object.Instantiate<ParticleSystem>(this._particleTemplate));
		}

		private void UnregisterRole(PlayerRoleBase prb)
		{
			IFpcRole fpcRole = prb as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			ParticleSystem particleSystem;
			if (!Scp1344.Trackers.Remove(fpcRole, out particleSystem))
			{
				return;
			}
			global::UnityEngine.Object.Destroy(particleSystem.gameObject);
		}

		private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			this.UnregisterRole(prevRole);
			this.RegisterRole(newRole);
		}

		private void OnPlayerRemoved(ReferenceHub user)
		{
			this.UnregisterRole(user.roleManager.CurrentRole);
		}

		private const float VisionDistance = 18f;

		private const float SurfaceVisionDistance = 36f;

		private const float VisionTranslucentDistance = 5f;

		private const float EnemyParticleScale = 2f;

		private const float AllyParticleScale = 1f;

		private const float ScpParticleScale = 2.5f;

		private const float MinHealthScaleMultiplier = 0.2f;

		private const float HeadOffset = 0.7f;

		private const float Scp106SubmergedOffset = 0.8f;

		private const float DirectSightCooldown = 23f;

		public const InvisibilityFlags BypassFlags = (InvisibilityFlags)7U;

		private static readonly Dictionary<IFpcRole, ParticleSystem> Trackers = new Dictionary<IFpcRole, ParticleSystem>();

		private static readonly Dictionary<uint, float> LastSeen = new Dictionary<uint, float>();

		private static bool _visionEnabled;

		private static FpcStandardRoleBase _currentRole;

		[SerializeField]
		private AudioClip _sfxEnable;

		[SerializeField]
		private AudioClip _sfxEnableNonDiegetic;

		[SerializeField]
		private AudioClip _sfxBuildupDiegetic;

		[SerializeField]
		private AudioClip _sfxDetected;

		[SerializeField]
		private ParticleSystem _particleTemplate;

		[SerializeField]
		private Color _invisiblePlayerColor;

		private bool _wasEverActive;

		private AudioPoolSession _enableSoundSession;
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AudioPooling;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Subroutines
{
	public abstract class ScpAttackAbilityBase<T> : KeySubroutine<T> where T : PlayerRoleBase, IFpcRole
	{
		public event Action<AttackResult> OnAttacked;

		public event Action OnTriggered;

		public TolerantAbilityCooldown Cooldown
		{
			get
			{
				if (!base.Owner.isLocalPlayer && NetworkServer.active)
				{
					return this._serverCooldown;
				}
				return this._clientCooldown;
			}
		}

		public bool AttackTriggered { get; private set; }

		public AttackResult LastAttackResult { get; protected set; }

		public abstract float DamageAmount { get; }

		protected abstract DamageHandlerBase DamageHandler(float damage);

		protected virtual float SoundRange
		{
			get
			{
				return 13f;
			}
		}

		protected virtual float AttackDelay
		{
			get
			{
				return 0f;
			}
		}

		protected virtual float BaseCooldown
		{
			get
			{
				return 1f;
			}
		}

		protected virtual bool SelfRepeating
		{
			get
			{
				return true;
			}
		}

		protected virtual bool CanTriggerAbility
		{
			get
			{
				return this._clientCooldown.IsReady;
			}
		}

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		private Transform PlyCam
		{
			get
			{
				return base.Owner.PlayerCameraReference;
			}
		}

		private Vector3 OverlapSphereOrigin
		{
			get
			{
				return this.PlyCam.position + this.PlyCam.forward * this._detectionOffset;
			}
		}

		public static ArraySegment<IDestructible> DetectDestructibles(ReferenceHub detector, float offset, float radius, bool losTest = true)
		{
			ScpAttackAbilityBase<T>.<>c__DisplayClass49_0 CS$<>8__locals1;
			CS$<>8__locals1.cameraPos = detector.PlayerCameraReference.position;
			int num = Physics.OverlapSphereNonAlloc(detector.PlayerCameraReference.TransformPoint(Vector3.forward * offset), radius, ScpAttackAbilityBase<T>.DetectionsNonAlloc, ScpAttackAbilityBase<T>.DetectionMask);
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				Collider collider = ScpAttackAbilityBase<T>.DetectionsNonAlloc[i];
				IDestructible destructible;
				if (collider.TryGetComponent<IDestructible>(out destructible) && destructible.NetworkId != detector.netId && (!losTest || ScpAttackAbilityBase<T>.<DetectDestructibles>g__CheckLineOfSight|49_0(collider, destructible.CenterOfMass, ref CS$<>8__locals1)))
				{
					ScpAttackAbilityBase<T>.DestDetectionsNonAlloc[num2++] = destructible;
				}
			}
			return new ArraySegment<IDestructible>(ScpAttackAbilityBase<T>.DestDetectionsNonAlloc, 0, num2);
		}

		private void ServerPerformAttack()
		{
			this.LastAttackResult = AttackResult.None;
			foreach (IDestructible destructible in ScpAttackAbilityBase<T>.DetectDestructibles(base.Owner, this._detectionOffset, this._detectionRadius, true))
			{
				HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
				if (hitboxIdentity == null)
				{
					this.DamageDestructible(destructible);
					this.LastAttackResult |= AttackResult.AttackedObject;
				}
				else if (HitboxIdentity.IsEnemy(base.Owner, hitboxIdentity.TargetHub))
				{
					this.DetectedPlayers.Add(hitboxIdentity.TargetHub);
				}
			}
			this.DamagePlayers();
			base.ServerSendRpc(true);
		}

		protected virtual void DamagePlayers()
		{
			foreach (ReferenceHub referenceHub in this.DetectedPlayers)
			{
				this.DamagePlayer(referenceHub, this.DamageAmount);
			}
		}

		protected virtual void DamagePlayer(ReferenceHub hub, float damage)
		{
			PlayerStats playerStats = hub.playerStats;
			if (!playerStats.DealDamage(this.DamageHandler(damage)))
			{
				return;
			}
			this.LastAttackResult |= AttackResult.AttackedPlayer;
			if (playerStats.GetModule<HealthStat>().CurValue > 0f)
			{
				return;
			}
			this.LastAttackResult |= AttackResult.KilledPlayer;
		}

		protected virtual void DamageDestructible(IDestructible dest)
		{
			dest.Damage(this.DamageAmount, this.DamageHandler(this.DamageAmount), dest.CenterOfMass);
		}

		protected bool HasAttackResultFlag(AttackResult flag)
		{
			return (this.LastAttackResult & flag) == flag;
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			if (this.AttackTriggered)
			{
				writer.WriteRelativePosition(default(RelativePosition));
				return;
			}
			Vector3 position = base.CastRole.FpcModule.Position;
			float num = this._detectionOffset + this._detectionRadius;
			float num2 = num * num;
			writer.WriteRelativePosition(new RelativePosition(position));
			writer.WriteLowPrecisionQuaternion(new LowPrecisionQuaternion(this.PlyCam.rotation));
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null)
					{
						Vector3 position2 = fpcRole.FpcModule.Position;
						if ((position2 - position).sqrMagnitude <= num2)
						{
							writer.WriteReferenceHub(referenceHub);
							writer.WriteRelativePosition(new RelativePosition(position2));
						}
					}
				}
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			RelativePosition relativePosition = reader.ReadRelativePosition();
			if (relativePosition.WaypointId == 0)
			{
				this.AttackTriggered = true;
				base.ServerSendRpc(true);
				return;
			}
			if (!this._serverCooldown.TolerantIsReady && !base.Owner.isLocalPlayer)
			{
				return;
			}
			this.AttackTriggered = false;
			Vector3 position = relativePosition.Position;
			Quaternion value = reader.ReadLowPrecisionQuaternion().Value;
			ScpAttackAbilityBase<T>.BacktrackedPlayers.Add(new FpcBacktracker(base.Owner, position, value, 0.1f, 0.15f));
			List<ReferenceHub> list = new List<ReferenceHub>();
			while (reader.Position < reader.Capacity)
			{
				ReferenceHub referenceHub = reader.ReadReferenceHub();
				list.Add(referenceHub);
				RelativePosition relativePosition2 = reader.ReadRelativePosition();
				if (!(referenceHub == null) && HitboxIdentity.IsEnemy(base.Owner, referenceHub))
				{
					ScpAttackAbilityBase<T>.BacktrackedPlayers.Add(new FpcBacktracker(referenceHub, relativePosition2.Position, 0.4f));
				}
			}
			this.ServerPerformAttack();
			ScpAttackAbilityBase<T>.BacktrackedPlayers.ForEach(delegate(FpcBacktracker x)
			{
				x.RestorePosition();
			});
			this._serverCooldown.Trigger((double)this.BaseCooldown);
			this.DetectedPlayers.Clear();
			ScpAttackAbilityBase<T>.BacktrackedPlayers.Clear();
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			if (this.AttackTriggered)
			{
				return;
			}
			writer.WriteByte((byte)this.LastAttackResult);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (reader.Position >= reader.Capacity)
			{
				if (base.Owner.isLocalPlayer)
				{
					return;
				}
				this._clientCooldown.Trigger((double)this.BaseCooldown);
				Action onTriggered = this.OnTriggered;
				if (onTriggered == null)
				{
					return;
				}
				onTriggered();
				return;
			}
			else
			{
				this.LastAttackResult = (AttackResult)reader.ReadByte();
				Action<AttackResult> onAttacked = this.OnAttacked;
				if (onAttacked != null)
				{
					onAttacked(this.LastAttackResult);
				}
				if (this.LastAttackResult != AttackResult.None && (base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated()))
				{
					Hitmarker.PlayHitmarker(1f, true);
				}
				if (this.HasAttackResultFlag(AttackResult.KilledPlayer) && this._killSound != null)
				{
					AudioSourcePoolManager.PlayOnTransform(this._killSound, base.transform, this.SoundRange, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
					return;
				}
				if (this.HasAttackResultFlag(AttackResult.AttackedPlayer) && this._hitClipsHuman.Length != 0)
				{
					AudioSourcePoolManager.PlayOnTransform(this._hitClipsHuman.RandomItem<AudioClip>(), base.transform, this.SoundRange, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
					return;
				}
				if (this.HasAttackResultFlag(AttackResult.AttackedObject) && this._hitClipsObjects.Length != 0)
				{
					AudioSourcePoolManager.PlayOnTransform(this._hitClipsObjects.RandomItem<AudioClip>(), base.transform, this.SoundRange, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
				}
				return;
			}
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.AttackTriggered = false;
			this._delaySw.Reset();
			this._clientCooldown.Clear();
			this._serverCooldown.Clear();
			this.DetectedPlayers.Clear();
			ScpAttackAbilityBase<T>.BacktrackedPlayers.Clear();
		}

		protected override void Update()
		{
			base.Update();
		}

		protected virtual void OnClientUpdate()
		{
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (this.AttackTriggered || this.SelfRepeating || !this.CanTriggerAbility)
			{
				return;
			}
			this.ClientPerformAttack(true);
		}

		protected virtual void ClientPerformAttack(bool attackTriggered = true)
		{
		}

		private void OnDrawGizmosSelected()
		{
			if (base.Owner == null)
			{
				return;
			}
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(this.OverlapSphereOrigin, this._detectionRadius);
		}

		[CompilerGenerated]
		internal static bool <DetectDestructibles>g__CheckLineOfSight|49_0(Collider hitColldier, Vector3 hitCenterOfMass, ref ScpAttackAbilityBase<T>.<>c__DisplayClass49_0 A_2)
		{
			RaycastHit raycastHit;
			return !Physics.Linecast(A_2.cameraPos, hitCenterOfMass, out raycastHit, PlayerRolesUtils.BlockerMask) || raycastHit.colliderInstanceID == hitColldier.GetInstanceID();
		}

		[SerializeField]
		private float _detectionRadius;

		[SerializeField]
		private float _detectionOffset;

		[SerializeField]
		private AudioClip _killSound;

		[SerializeField]
		private AudioClip[] _hitClipsHuman;

		[SerializeField]
		private AudioClip[] _hitClipsObjects;

		private readonly Stopwatch _delaySw = new Stopwatch();

		private readonly TolerantAbilityCooldown _clientCooldown = new TolerantAbilityCooldown(0.2f);

		private readonly TolerantAbilityCooldown _serverCooldown = new TolerantAbilityCooldown(0.2f);

		private static readonly HashSet<FpcBacktracker> BacktrackedPlayers = new HashSet<FpcBacktracker>();

		private static readonly IDestructible[] DestDetectionsNonAlloc = new IDestructible[128];

		private static readonly Collider[] DetectionsNonAlloc = new Collider[128];

		private static readonly CachedLayerMask DetectionMask = new CachedLayerMask(new string[] { "Hitbox", "Glass" });

		private const int DetectionsNumber = 128;

		protected readonly HashSet<ReferenceHub> DetectedPlayers = new HashSet<ReferenceHub>();
	}
}

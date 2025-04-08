using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096TargetsTracker : StandardSubroutine<Scp096Role>
	{
		public static event Action<ReferenceHub, ReferenceHub> OnTargetAdded;

		public event Action<ReferenceHub> OnTargetAttacked;

		public static event Action<ReferenceHub, ReferenceHub> OnTargetRemoved;

		public bool CanReceiveTargets
		{
			get
			{
				return !base.CastRole.IsRageState(Scp096RageState.Calming);
			}
		}

		public bool AddTarget(ReferenceHub target, bool isLooking)
		{
			if (target == null || this.Targets.Contains(target))
			{
				return false;
			}
			Scp096AddingTargetEventArgs scp096AddingTargetEventArgs = new Scp096AddingTargetEventArgs(base.Owner, target, isLooking);
			Scp096Events.OnAddingTarget(scp096AddingTargetEventArgs);
			if (!scp096AddingTargetEventArgs.IsAllowed)
			{
				return false;
			}
			this.Targets.Add(target);
			if (!NetworkServer.active && !this._markers.ContainsKey(target))
			{
				this._markers.Add(target, global::UnityEngine.Object.Instantiate<GameObject>(this.TargetMarker, target.transform));
			}
			this._sendTargetsNextFrame = true;
			Action<ReferenceHub, ReferenceHub> onTargetAdded = Scp096TargetsTracker.OnTargetAdded;
			if (onTargetAdded != null)
			{
				onTargetAdded(base.Owner, target);
			}
			Scp096Events.OnAddedTarget(new Scp096AddedTargetEventArgs(base.Owner, target, isLooking));
			return true;
		}

		public bool RemoveTarget(ReferenceHub target)
		{
			if (target == null || !this.Targets.Remove(target))
			{
				return false;
			}
			GameObject gameObject;
			if (this._markers.TryGetValue(target, out gameObject))
			{
				this._markers.Remove(target);
				global::UnityEngine.Object.Destroy(gameObject);
			}
			this._sendTargetsNextFrame = true;
			Action<ReferenceHub, ReferenceHub> onTargetRemoved = Scp096TargetsTracker.OnTargetRemoved;
			if (onTargetRemoved != null)
			{
				onTargetRemoved(base.Owner, target);
			}
			return true;
		}

		public void ClearAllTargets()
		{
			foreach (ReferenceHub referenceHub in this.Targets)
			{
				GameObject gameObject;
				if (this._markers.TryGetValue(referenceHub, out gameObject))
				{
					this._markers.Remove(referenceHub);
					global::UnityEngine.Object.Destroy(gameObject);
				}
				Action<ReferenceHub, ReferenceHub> onTargetRemoved = Scp096TargetsTracker.OnTargetRemoved;
				if (onTargetRemoved != null)
				{
					onTargetRemoved(base.Owner, referenceHub);
				}
			}
			this._sendTargetsNextFrame = true;
			this.Targets.Clear();
		}

		public bool IsObservedBy(ReferenceHub target)
		{
			Vector3 position = (base.CastRole.FpcModule.CharacterModelInstance as Scp096CharacterModel).Head.position;
			return Vector3.Dot((target.PlayerCameraReference.position - position).normalized, base.Owner.PlayerCameraReference.forward) >= 0.1f && VisionInformation.GetVisionInformation(target, target.PlayerCameraReference, position, 0.12f, 60f, true, true, 0, true).IsLooking;
		}

		public bool HasTarget(ReferenceHub target)
		{
			return this.Targets.Contains(target);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			this.Targets.ForEach(new Action<ReferenceHub>(writer.WriteReferenceHub));
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._unvalidatedTargets.UnionWith(this.Targets);
			while (reader.Position < reader.Capacity)
			{
				ReferenceHub referenceHub;
				if (reader.TryReadReferenceHub(out referenceHub) && !this._unvalidatedTargets.Remove(referenceHub))
				{
					this.AddTarget(referenceHub, false);
				}
			}
			this._unvalidatedTargets.ForEach(delegate(ReferenceHub x)
			{
				this.RemoveTarget(x);
			});
			this._unvalidatedTargets.Clear();
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._eventsAssigned = true;
			base.Owner.playerStats.OnThisPlayerDamaged += this.AddTargetOnDamage;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.ClearAllTargets();
			if (!this._eventsAssigned)
			{
				return;
			}
			this._eventsAssigned = false;
			base.Owner.playerStats.OnThisPlayerDamaged -= this.AddTargetOnDamage;
		}

		protected override void Awake()
		{
			base.Awake();
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.CheckRemovedPlayer));
			base.CastRole.StateController.OnRageUpdate += delegate(Scp096RageState state)
			{
				if (state != Scp096RageState.Calming)
				{
					return;
				}
				this._postRageCooldown.Trigger(10.0);
				this.ClearAllTargets();
			};
		}

		private void AddTargetOnDamage(DamageHandlerBase obj)
		{
			AttackerDamageHandler attackerDamageHandler = obj as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (!this.CanReceiveTargets || hub == null)
			{
				return;
			}
			this.AddTarget(hub, false);
			Action<ReferenceHub> onTargetAttacked = this.OnTargetAttacked;
			if (onTargetAttacked == null)
			{
				return;
			}
			onTargetAttacked(hub);
		}

		private void OnDestroy()
		{
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.CheckRemovedPlayer));
		}

		private void Update()
		{
			bool visible = base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated();
			this._markers.ForEachValue(delegate(GameObject x)
			{
				x.SetActive(visible);
			});
			if (!NetworkServer.active)
			{
				return;
			}
			this.ServerCheckTargets();
		}

		private void CheckRemovedPlayer(ReferenceHub ply)
		{
			this.RemoveTarget(ply);
		}

		private void ServerCheckTargets()
		{
			if (base.CastRole.IsRageState(Scp096RageState.Calming))
			{
				return;
			}
			if (!this._postRageCooldown.IsReady)
			{
				return;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				this.UpdateTarget(referenceHub);
			}
			if (!this._sendTargetsNextFrame)
			{
				return;
			}
			this._sendTargetsNextFrame = false;
			base.ServerSendRpc(true);
		}

		private void UpdateTarget(ReferenceHub target)
		{
			if (!HitboxIdentity.IsEnemy(base.Owner, target))
			{
				this.RemoveTarget(target);
				return;
			}
			if (base.CastRole.IsAbilityState(Scp096AbilityState.Charging))
			{
				return;
			}
			if (!this.IsObservedBy(target))
			{
				return;
			}
			this.AddTarget(target, true);
		}

		private const float Vision096InnerAngle = 0.1f;

		private const float VisionTriggerDistance = 60f;

		private const float HeadSize = 0.12f;

		private const float PostRageCooldownDuration = 10f;

		public GameObject TargetMarker;

		public readonly HashSet<ReferenceHub> Targets = new HashSet<ReferenceHub>();

		private readonly AbilityCooldown _postRageCooldown = new AbilityCooldown();

		private readonly Dictionary<ReferenceHub, GameObject> _markers = new Dictionary<ReferenceHub, GameObject>();

		private readonly HashSet<ReferenceHub> _unvalidatedTargets = new HashSet<ReferenceHub>();

		[SerializeField]
		private AudioClip _targetSound;

		private bool _sendTargetsNextFrame;

		private bool _eventsAssigned;
	}
}

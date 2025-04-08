using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem.Items.Autosync;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Jailbird
{
	[Serializable]
	public class JailbirdHitreg
	{
		public float TotalMeleeDamageDealt { get; internal set; }

		public bool AnyDetected
		{
			get
			{
				this.DetectDestructibles();
				return JailbirdHitreg._detectionsLen > 0;
			}
		}

		public void Setup(JailbirdItem target)
		{
			this._item = target;
		}

		public bool ClientTryAttack()
		{
			IFpcRole fpcRole = this._item.Owner.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			NetworkWriter networkWriter;
			using (new AutosyncCmd(this._item.ItemId, out networkWriter))
			{
				networkWriter.WriteByte(4);
				networkWriter.WriteRelativePosition(new RelativePosition(fpcRole.FpcModule.Position));
				networkWriter.WriteQuaternion(this._item.Owner.PlayerCameraReference.rotation);
				this.DetectDestructibles();
				if (JailbirdHitreg._detectionsLen > 255)
				{
					JailbirdHitreg._detectionsLen = 255;
				}
				List<ReferenceHub> list = ListPool<ReferenceHub>.Shared.Rent(JailbirdHitreg._detectionsLen);
				for (int i = 0; i < JailbirdHitreg._detectionsLen; i++)
				{
					HitboxIdentity hitboxIdentity = JailbirdHitreg.DetectedDestructibles[i] as HitboxIdentity;
					if (hitboxIdentity != null)
					{
						list.Add(hitboxIdentity.TargetHub);
					}
				}
				networkWriter.WriteByte((byte)list.Count);
				foreach (ReferenceHub referenceHub in list)
				{
					networkWriter.WriteReferenceHub(referenceHub);
					networkWriter.WriteRelativePosition(new RelativePosition(referenceHub));
				}
				ListPool<ReferenceHub>.Shared.Return(list);
			}
			return true;
		}

		public bool ServerAttack(bool isCharging, NetworkReader reader)
		{
			ReferenceHub owner = this._item.Owner;
			bool flag = false;
			if (reader != null)
			{
				RelativePosition relativePosition = reader.ReadRelativePosition();
				Quaternion quaternion = reader.ReadQuaternion();
				JailbirdHitreg.BacktrackedPlayers.Add(new FpcBacktracker(owner, relativePosition.Position, quaternion, 0.1f, 0.15f));
				byte b = reader.ReadByte();
				for (int i = 0; i < (int)b; i++)
				{
					ReferenceHub referenceHub;
					bool flag2 = reader.TryReadReferenceHub(out referenceHub);
					RelativePosition relativePosition2 = reader.ReadRelativePosition();
					if (flag2)
					{
						JailbirdHitreg.BacktrackedPlayers.Add(new FpcBacktracker(referenceHub, relativePosition2.Position, 0.4f));
					}
				}
			}
			this.DetectDestructibles();
			Vector3 forward = this._item.Owner.PlayerCameraReference.forward;
			float num = (isCharging ? this._damageCharge : this._damageMelee);
			for (int j = 0; j < JailbirdHitreg._detectionsLen; j++)
			{
				IDestructible destructible = JailbirdHitreg.DetectedDestructibles[j];
				if (destructible.Damage(num, new JailbirdDamageHandler(owner, num, forward), destructible.CenterOfMass))
				{
					flag = true;
					if (!isCharging)
					{
						this.TotalMeleeDamageDealt += num;
					}
					else
					{
						HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
						if (hitboxIdentity != null)
						{
							hitboxIdentity.TargetHub.playerEffectsController.EnableEffect<Flashed>(this._flashedDuration, true);
							hitboxIdentity.TargetHub.playerEffectsController.EnableEffect<Concussed>(this._concussionDuration, true);
						}
					}
				}
			}
			JailbirdHitreg.BacktrackedPlayers.ForEach(delegate(FpcBacktracker x)
			{
				x.RestorePosition();
			});
			JailbirdHitreg.BacktrackedPlayers.Clear();
			return flag;
		}

		private void DetectDestructibles()
		{
			Transform playerCameraReference = this._item.Owner.PlayerCameraReference;
			Vector3 vector = playerCameraReference.position + playerCameraReference.forward * this._hitregOffset;
			JailbirdHitreg._detectionsLen = 0;
			int num = Physics.OverlapSphereNonAlloc(vector, this._hitregRadius, JailbirdHitreg.DetectedColliders, JailbirdHitreg.DetectionMask);
			if (num == 0)
			{
				return;
			}
			JailbirdHitreg.DetectedNetIds.Clear();
			for (int i = 0; i < num; i++)
			{
				IDestructible destructible;
				RaycastHit raycastHit;
				if (JailbirdHitreg.DetectedColliders[i].TryGetComponent<IDestructible>(out destructible) && (!Physics.Linecast(playerCameraReference.position, destructible.CenterOfMass, out raycastHit, JailbirdHitreg.LinecastMask) || !(raycastHit.collider != JailbirdHitreg.DetectedColliders[i])) && JailbirdHitreg.DetectedNetIds.Add(destructible.NetworkId))
				{
					JailbirdHitreg.DetectedDestructibles[JailbirdHitreg._detectionsLen++] = destructible;
				}
			}
		}

		private const int MaxDetections = 128;

		private static readonly Collider[] DetectedColliders = new Collider[128];

		private static readonly IDestructible[] DetectedDestructibles = new IDestructible[128];

		private static readonly CachedLayerMask DetectionMask = new CachedLayerMask(new string[] { "Hitbox", "Glass" });

		private static readonly CachedLayerMask LinecastMask = new CachedLayerMask(new string[] { "Default" });

		private static readonly HashSet<uint> DetectedNetIds = new HashSet<uint>();

		private static readonly HashSet<FpcBacktracker> BacktrackedPlayers = new HashSet<FpcBacktracker>();

		private static int _detectionsLen;

		[SerializeField]
		private float _hitregOffset;

		[SerializeField]
		private float _hitregRadius;

		[SerializeField]
		private float _damageMelee;

		[SerializeField]
		private float _damageCharge;

		[SerializeField]
		[Tooltip("How long in seconds the 'concussed' effect is applied for on attacked targets.")]
		private float _concussionDuration;

		[SerializeField]
		[Tooltip("How long in seconds the 'flashed' effect is applied for on attacked targets.")]
		private float _flashedDuration = 1.5f;

		private JailbirdItem _item;
	}
}

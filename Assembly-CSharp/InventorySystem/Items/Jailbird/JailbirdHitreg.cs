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

namespace InventorySystem.Items.Jailbird;

[Serializable]
public class JailbirdHitreg
{
	private const int MaxDetections = 128;

	private static readonly Collider[] DetectedColliders = new Collider[128];

	private static readonly IDestructible[] DetectedDestructibles = new IDestructible[128];

	private static readonly CachedLayerMask DetectionMask = new CachedLayerMask("Hitbox", "Glass");

	private static readonly CachedLayerMask LinecastMask = new CachedLayerMask("Default");

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

	public float TotalMeleeDamageDealt { get; internal set; }

	public bool AnyDetected
	{
		get
		{
			DetectDestructibles();
			return _detectionsLen > 0;
		}
	}

	public void Setup(JailbirdItem target)
	{
		_item = target;
	}

	public bool ClientTryAttack()
	{
		if (!(_item.Owner.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		NetworkWriter writer;
		using (new AutosyncCmd(_item.ItemId, out writer))
		{
			writer.WriteByte(4);
			writer.WriteRelativePosition(new RelativePosition(fpcRole.FpcModule.Position));
			writer.WriteQuaternion(_item.Owner.PlayerCameraReference.rotation);
			DetectDestructibles();
			if (_detectionsLen > 255)
			{
				_detectionsLen = 255;
			}
			List<ReferenceHub> list = ListPool<ReferenceHub>.Shared.Rent(_detectionsLen);
			for (int i = 0; i < _detectionsLen; i++)
			{
				if (DetectedDestructibles[i] is HitboxIdentity hitboxIdentity)
				{
					list.Add(hitboxIdentity.TargetHub);
				}
			}
			writer.WriteByte((byte)list.Count);
			foreach (ReferenceHub item in list)
			{
				writer.WriteReferenceHub(item);
				writer.WriteRelativePosition(new RelativePosition(item));
			}
			ListPool<ReferenceHub>.Shared.Return(list);
		}
		return true;
	}

	public bool ServerAttack(bool isCharging, NetworkReader reader)
	{
		ReferenceHub owner = _item.Owner;
		bool result = false;
		if (reader != null)
		{
			RelativePosition relativePosition = reader.ReadRelativePosition();
			Quaternion claimedRot = reader.ReadQuaternion();
			BacktrackedPlayers.Add(new FpcBacktracker(owner, relativePosition.Position, claimedRot));
			byte b = reader.ReadByte();
			for (int i = 0; i < b; i++)
			{
				ReferenceHub hub;
				bool num = reader.TryReadReferenceHub(out hub);
				RelativePosition relativePosition2 = reader.ReadRelativePosition();
				if (num)
				{
					BacktrackedPlayers.Add(new FpcBacktracker(hub, relativePosition2.Position));
				}
			}
		}
		DetectDestructibles();
		Vector3 forward = _item.Owner.PlayerCameraReference.forward;
		float num2 = (isCharging ? _damageCharge : _damageMelee);
		for (int j = 0; j < _detectionsLen; j++)
		{
			IDestructible destructible = DetectedDestructibles[j];
			if (destructible.Damage(num2, new JailbirdDamageHandler(owner, num2, forward), destructible.CenterOfMass))
			{
				result = true;
				if (!isCharging)
				{
					TotalMeleeDamageDealt += num2;
				}
				else if (destructible is HitboxIdentity hitboxIdentity)
				{
					hitboxIdentity.TargetHub.playerEffectsController.EnableEffect<Flashed>(_flashedDuration, addDuration: true);
					hitboxIdentity.TargetHub.playerEffectsController.EnableEffect<Concussed>(_concussionDuration, addDuration: true);
				}
			}
		}
		BacktrackedPlayers.ForEach(delegate(FpcBacktracker x)
		{
			x.RestorePosition();
		});
		BacktrackedPlayers.Clear();
		return result;
	}

	private void DetectDestructibles()
	{
		Transform playerCameraReference = _item.Owner.PlayerCameraReference;
		Vector3 position = playerCameraReference.position + playerCameraReference.forward * _hitregOffset;
		_detectionsLen = 0;
		int num = Physics.OverlapSphereNonAlloc(position, _hitregRadius, DetectedColliders, DetectionMask);
		if (num == 0)
		{
			return;
		}
		DetectedNetIds.Clear();
		for (int i = 0; i < num; i++)
		{
			if (DetectedColliders[i].TryGetComponent<IDestructible>(out var component) && (!Physics.Linecast(playerCameraReference.position, component.CenterOfMass, out var hitInfo, LinecastMask) || !(hitInfo.collider != DetectedColliders[i])) && DetectedNetIds.Add(component.NetworkId))
			{
				DetectedDestructibles[_detectionsLen++] = component;
			}
		}
	}
}

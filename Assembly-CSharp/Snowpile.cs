using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Verification;
using InventorySystem;
using InventorySystem.Items;
using Mirror;
using UnityEngine;

public class Snowpile : NetworkBehaviour, IServerInteractable, IInteractable
{
	public IVerificationRule VerificationRule { get; private set; } = new StandardDistanceVerification(8f, false, true);

	private void Awake()
	{
		this._mat = new Material(this._renderers[0].sharedMaterial);
		Renderer[] renderers = this._renderers;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].sharedMaterial = this._mat;
		}
		this._prevSize = 1f;
		this._initialUses = this._remainingUses;
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			this.ServerUpdate();
		}
		else
		{
			base.transform.SetPositionAndRotation(this._position, this._rotation);
		}
		float num = Mathf.Lerp(this._prevSize, (float)this._remainingUses, Time.deltaTime * this._scaleAdjustSpeed);
		if (Mathf.Abs(num - (float)this._remainingUses) <= 0.03f)
		{
			return;
		}
		this._prevSize = num;
		float num2 = this._fadeOverUses.Evaluate(num);
		this._mat.SetFloat(Snowpile.FadeHash, num2);
		this._scaler.gameObject.SetActive(num2 > 0f);
		this._scaler.localScale = new Vector3(1f, this._scaleOverUses.Evaluate(num), 1f);
	}

	private void ServerUpdate()
	{
		this.Network_position = base.transform.position;
		this.Network_rotation = base.transform.rotation;
		if (!this._regenerate || this._remainingUses >= this._initialUses)
		{
			return;
		}
		this._regenerationTimer += Time.deltaTime;
		if (this._regenerationTimer < this._regenerationDuration)
		{
			return;
		}
		this.Network_remainingUses = this._remainingUses + 1;
		this._regenerationTimer = 0f;
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (ply == null || this._remainingUses <= 0)
		{
			return;
		}
		if (!(ply.roleManager.CurrentRole is IInventoryRole))
		{
			return;
		}
		foreach (KeyValuePair<ushort, ItemBase> keyValuePair in ply.inventory.UserInventory.Items)
		{
			if (keyValuePair.Value is SnowballItem)
			{
				return;
			}
		}
		if (ply.inventory.ServerAddItem(ItemType.Snowball, ItemAddReason.PickedUp, 0, null) == null)
		{
			return;
		}
		this.Network_remainingUses = this._remainingUses - 1;
	}

	public override bool Weaved()
	{
		return true;
	}

	public Vector3 Network_position
	{
		get
		{
			return this._position;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<Vector3>(value, ref this._position, 1UL, null);
		}
	}

	public Quaternion Network_rotation
	{
		get
		{
			return this._rotation;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<Quaternion>(value, ref this._rotation, 2UL, null);
		}
	}

	public int Network_remainingUses
	{
		get
		{
			return this._remainingUses;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<int>(value, ref this._remainingUses, 4UL, null);
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteVector3(this._position);
			writer.WriteQuaternion(this._rotation);
			writer.WriteInt(this._remainingUses);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteVector3(this._position);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteQuaternion(this._rotation);
		}
		if ((base.syncVarDirtyBits & 4UL) != 0UL)
		{
			writer.WriteInt(this._remainingUses);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<Vector3>(ref this._position, null, reader.ReadVector3());
			base.GeneratedSyncVarDeserialize<Quaternion>(ref this._rotation, null, reader.ReadQuaternion());
			base.GeneratedSyncVarDeserialize<int>(ref this._remainingUses, null, reader.ReadInt());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<Vector3>(ref this._position, null, reader.ReadVector3());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<Quaternion>(ref this._rotation, null, reader.ReadQuaternion());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<int>(ref this._remainingUses, null, reader.ReadInt());
		}
	}

	private static readonly int FadeHash = Shader.PropertyToID("_Fade");

	private const float MinDiff = 0.03f;

	private Material _mat;

	private float _prevSize;

	private float _regenerationTimer;

	private int _initialUses;

	[SyncVar]
	private Vector3 _position;

	[SyncVar]
	private Quaternion _rotation;

	[SyncVar]
	[SerializeField]
	private int _remainingUses;

	[SerializeField]
	private Transform _scaler;

	[SerializeField]
	private AnimationCurve _scaleOverUses;

	[SerializeField]
	private AnimationCurve _fadeOverUses;

	[SerializeField]
	private float _scaleAdjustSpeed;

	[SerializeField]
	private bool _regenerate;

	[SerializeField]
	private float _regenerationDuration;

	[SerializeField]
	private Renderer[] _renderers;
}

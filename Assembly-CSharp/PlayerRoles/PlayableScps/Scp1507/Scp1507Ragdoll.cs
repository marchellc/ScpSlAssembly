using System.Runtime.InteropServices;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507;

public class Scp1507Ragdoll : DynamicRagdoll
{
	public const float RevivalMaxTime = 12f;

	private const float ReviveDistanceSqr = 15f;

	private const float ReviveProgressAddition = 1f / 12f;

	private const float ReviveAlphaBoost = 2f;

	private const int FreezeFixedFrames = 3;

	[SyncVar]
	private float _revivalProgress;

	[SyncVar]
	private bool _hasAlreadyRevived;

	[SyncVar]
	private double _lastResetTime;

	[SerializeField]
	private GameObject[] _alphaVariants;

	[SerializeField]
	private GameObject[] _regularVariants;

	[SerializeField]
	private GameObject[] _zombieVariants;

	private int _unfreezeDelay;

	public float RevivalProgress => this._revivalProgress;

	public float Network_revivalProgress
	{
		get
		{
			return this._revivalProgress;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._revivalProgress, 2uL, null);
		}
	}

	public bool Network_hasAlreadyRevived
	{
		get
		{
			return this._hasAlreadyRevived;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._hasAlreadyRevived, 4uL, null);
		}
	}

	public double Network_lastResetTime
	{
		get
		{
			return this._lastResetTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._lastResetTime, 8uL, null);
		}
	}

	public bool ValidateRevive(ReferenceHub vocalizer)
	{
		if (this._hasAlreadyRevived || base.Info.RoleType.GetTeam() != vocalizer.GetTeam())
		{
			return false;
		}
		if (NetworkTime.time - this._lastResetTime > 12.0)
		{
			return false;
		}
		ReferenceHub ownerHub = base.Info.OwnerHub;
		if (ownerHub == null || !(ownerHub.roleManager.CurrentRole is SpectatorRole { ReadyToRespawn: not false }))
		{
			return false;
		}
		return true;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Scp1507VocalizeAbility.OnServerVocalize -= OnVocalize;
	}

	protected override void Start()
	{
		this.SpawnVariant(base.Info.RoleType);
		this.Network_lastResetTime = NetworkTime.time;
		base.Start();
		Scp1507VocalizeAbility.OnServerVocalize += OnVocalize;
	}

	private void FixedUpdate()
	{
		if (this._unfreezeDelay <= 0)
		{
			return;
		}
		this._unfreezeDelay--;
		if (this._unfreezeDelay > 0)
		{
			return;
		}
		Rigidbody[] linkedRigidbodies = base.LinkedRigidbodies;
		foreach (Rigidbody rigidbody in linkedRigidbodies)
		{
			if (!(rigidbody == null))
			{
				rigidbody.isKinematic = false;
			}
		}
	}

	private void SpawnVariant(RoleTypeId role)
	{
		GameObject[] array;
		switch (role)
		{
		default:
			return;
		case RoleTypeId.AlphaFlamingo:
			array = this._alphaVariants;
			break;
		case RoleTypeId.ZombieFlamingo:
			array = this._zombieVariants;
			break;
		case RoleTypeId.Flamingo:
			array = this._regularVariants;
			break;
		}
		GameObject gameObject = array.RandomItem();
		GameObject gameObject2 = Object.Instantiate(gameObject, base.transform);
		gameObject2.transform.SetLocalPositionAndRotation(gameObject.transform.localPosition, gameObject.transform.localRotation);
		base.LinkedRigidbodies = gameObject2.GetComponentsInChildren<Rigidbody>();
		base.LinkedRigidbodiesTransforms = new Transform[base.LinkedRigidbodies.Length];
		for (int i = 0; i < base.LinkedRigidbodies.Length; i++)
		{
			Rigidbody rigidbody = base.LinkedRigidbodies[i];
			rigidbody.isKinematic = true;
			base.LinkedRigidbodiesTransforms[i] = rigidbody.transform;
		}
		this._unfreezeDelay = 3;
	}

	private void OnVocalize(ReferenceHub hub)
	{
		if (!NetworkServer.active || !this.ValidateRevive(hub))
		{
			return;
		}
		Scp1507Role scp1507Role = hub.roleManager.CurrentRole as Scp1507Role;
		bool flag = false;
		for (int i = 0; i < base.LinkedRigidbodiesTransforms.Length; i++)
		{
			if (!(scp1507Role.SqrDistanceTo(base.LinkedRigidbodiesTransforms[i].position) > 15f))
			{
				flag = true;
				break;
			}
		}
		if (flag || !(scp1507Role.SqrDistanceTo(base.Info.StartPosition) > 15f))
		{
			float num = 1f / 12f;
			if (scp1507Role.RoleTypeId == RoleTypeId.AlphaFlamingo)
			{
				num *= 2f;
			}
			this.Network_lastResetTime = NetworkTime.time;
			this.Network_revivalProgress = this._revivalProgress + num;
			if (!(this._revivalProgress < 1f))
			{
				base.Info.OwnerHub.roleManager.ServerSetRole(base.Info.RoleType, RoleChangeReason.Revived);
				NetworkServer.Destroy(base.gameObject);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RagdollManager.ServerOnRagdollCreated += ServerOnRagdollCreated;
	}

	private static void ServerOnRagdollCreated(ReferenceHub owner, BasicRagdoll basicRagdoll)
	{
		if (!(owner == null) && owner.roleManager.CurrentRole is Scp1507Role scp1507Role && basicRagdoll is Scp1507Ragdoll scp1507Ragdoll)
		{
			scp1507Ragdoll.Network_hasAlreadyRevived = scp1507Role.AlreadyRevived;
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this._revivalProgress);
			writer.WriteBool(this._hasAlreadyRevived);
			writer.WriteDouble(this._lastResetTime);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteFloat(this._revivalProgress);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			writer.WriteBool(this._hasAlreadyRevived);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteDouble(this._lastResetTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._revivalProgress, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this._hasAlreadyRevived, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this._lastResetTime, null, reader.ReadDouble());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._revivalProgress, null, reader.ReadFloat());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._hasAlreadyRevived, null, reader.ReadBool());
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._lastResetTime, null, reader.ReadDouble());
		}
	}
}

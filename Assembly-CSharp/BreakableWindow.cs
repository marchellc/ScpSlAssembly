using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Footprinting;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration.StaticHelpers;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using UnityEngine.Rendering;

public class BreakableWindow : NetworkBehaviour, IDestructible, IBlockStaticBatching
{
	public GameObject template;

	public Transform parent;

	[SerializeField]
	private bool _preventScpDamage;

	public Footprint LastAttacker;

	private bool prevStatus;

	[SyncVar]
	public bool isBroken;

	public float health = 30f;

	private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

	private Transform _transform;

	public uint NetworkId => base.netId;

	public Vector3 CenterOfMass => base.transform.position;

	public bool NetworkisBroken
	{
		get
		{
			return isBroken;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref isBroken, 1uL, null);
		}
	}

	[ServerCallback]
	private void ServerDamageWindow(float damage)
	{
		if (NetworkServer.active)
		{
			health -= damage;
			if (health <= 0f)
			{
				NetworkisBroken = true;
			}
		}
	}

	private void Awake()
	{
		meshRenderers.AddRange(GetComponentsInChildren<MeshRenderer>());
		_transform = base.transform;
		GetComponent<Collider>().enabled = false;
		Invoke("EnableColliders", 1f);
	}

	private void EnableColliders()
	{
		GetComponent<Collider>().enabled = true;
	}

	private void Update()
	{
		if (isBroken && !prevStatus)
		{
			StartCoroutine(BreakWindow());
			prevStatus = true;
		}
	}

	private void LateUpdate()
	{
		for (int num = meshRenderers.Count - 1; num >= 0; num--)
		{
			MeshRenderer meshRenderer = meshRenderers[num];
			meshRenderer.shadowCastingMode = (isBroken ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off);
			if (isBroken)
			{
				meshRenderers.RemoveAt(num);
				Object.Destroy(meshRenderer);
			}
			meshRenderer.gameObject.layer = (isBroken ? 28 : 14);
		}
	}

	private IEnumerator BreakWindow()
	{
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		yield break;
	}

	private bool CheckDamagePerms(RoleTypeId roleType)
	{
		if (!_preventScpDamage)
		{
			return true;
		}
		if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(roleType, out var result))
		{
			return false;
		}
		return result.Team != Team.SCPs;
	}

	public bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
	{
		if (!(handler is AttackerDamageHandler attackerDamageHandler))
		{
			ServerDamageWindow(damage);
			return true;
		}
		if (!CheckDamagePerms(attackerDamageHandler.Attacker.Role))
		{
			return false;
		}
		PlayerDamagingWindowEventArgs playerDamagingWindowEventArgs = new PlayerDamagingWindowEventArgs(attackerDamageHandler.Attacker.Hub, this, handler);
		PlayerEvents.OnDamagingWindow(playerDamagingWindowEventArgs);
		if (!playerDamagingWindowEventArgs.IsAllowed)
		{
			return false;
		}
		LastAttacker = attackerDamageHandler.Attacker;
		ServerDamageWindow(damage);
		PlayerEvents.OnDamagedWindow(new PlayerDamagedWindowEventArgs(attackerDamageHandler.Attacker.Hub, this, handler));
		return true;
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
			writer.WriteBool(isBroken);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(isBroken);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref isBroken, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref isBroken, null, reader.ReadBool());
		}
	}
}

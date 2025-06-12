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
			return this.isBroken;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.isBroken, 1uL, null);
		}
	}

	[ServerCallback]
	private void ServerDamageWindow(float damage)
	{
		if (NetworkServer.active)
		{
			this.health -= damage;
			if (this.health <= 0f)
			{
				this.NetworkisBroken = true;
			}
		}
	}

	private void Awake()
	{
		this.meshRenderers.AddRange(base.GetComponentsInChildren<MeshRenderer>());
		this._transform = base.transform;
		base.GetComponent<Collider>().enabled = false;
		base.Invoke("EnableColliders", 1f);
	}

	private void EnableColliders()
	{
		base.GetComponent<Collider>().enabled = true;
	}

	private void Update()
	{
		if (this.isBroken && !this.prevStatus)
		{
			base.StartCoroutine(this.BreakWindow());
			this.prevStatus = true;
		}
	}

	private void LateUpdate()
	{
		for (int num = this.meshRenderers.Count - 1; num >= 0; num--)
		{
			MeshRenderer meshRenderer = this.meshRenderers[num];
			meshRenderer.shadowCastingMode = (this.isBroken ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off);
			if (this.isBroken)
			{
				this.meshRenderers.RemoveAt(num);
				Object.Destroy(meshRenderer);
			}
			meshRenderer.gameObject.layer = (this.isBroken ? 28 : 14);
		}
	}

	private IEnumerator BreakWindow()
	{
		Collider[] componentsInChildren = base.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		yield break;
	}

	private bool CheckDamagePerms(RoleTypeId roleType)
	{
		if (!this._preventScpDamage)
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
			this.ServerDamageWindow(damage);
			return true;
		}
		if (!this.CheckDamagePerms(attackerDamageHandler.Attacker.Role))
		{
			return false;
		}
		PlayerDamagingWindowEventArgs e = new PlayerDamagingWindowEventArgs(attackerDamageHandler.Attacker.Hub, this, handler);
		PlayerEvents.OnDamagingWindow(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		this.LastAttacker = attackerDamageHandler.Attacker;
		this.ServerDamageWindow(damage);
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
			writer.WriteBool(this.isBroken);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this.isBroken);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.isBroken, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.isBroken, null, reader.ReadBool());
		}
	}
}

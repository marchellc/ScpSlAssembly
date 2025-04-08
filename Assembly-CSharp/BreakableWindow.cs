using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Footprinting;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;
using UnityEngine.Rendering;

public class BreakableWindow : NetworkBehaviour, IDestructible
{
	[ServerCallback]
	private void ServerDamageWindow(float damage)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		this.health -= damage;
		if (this.health <= 0f)
		{
			this.NetworkisBroken = true;
		}
	}

	public uint NetworkId
	{
		get
		{
			return base.netId;
		}
	}

	public Vector3 CenterOfMass
	{
		get
		{
			return base.transform.position;
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
		if (!this.isBroken || this.prevStatus)
		{
			return;
		}
		base.StartCoroutine(this.BreakWindow());
		this.prevStatus = true;
	}

	private void LateUpdate()
	{
		for (int i = this.meshRenderers.Count - 1; i >= 0; i--)
		{
			MeshRenderer meshRenderer = this.meshRenderers[i];
			meshRenderer.shadowCastingMode = (this.isBroken ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.Off);
			if (this.isBroken)
			{
				this.meshRenderers.RemoveAt(i);
				global::UnityEngine.Object.Destroy(meshRenderer);
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
		PlayerRoleBase playerRoleBase;
		return !this._preventScpDamage || (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(roleType, out playerRoleBase) && playerRoleBase.Team > Team.SCPs);
	}

	public bool Damage(float damage, DamageHandlerBase handler, Vector3 pos)
	{
		AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
		if (attackerDamageHandler == null)
		{
			return false;
		}
		if (!this.CheckDamagePerms(attackerDamageHandler.Attacker.Role))
		{
			return false;
		}
		PlayerDamagingWindowEventArgs playerDamagingWindowEventArgs = new PlayerDamagingWindowEventArgs(attackerDamageHandler.Attacker.Hub, this, handler);
		PlayerEvents.OnDamagingWindow(playerDamagingWindowEventArgs);
		if (!playerDamagingWindowEventArgs.IsAllowed)
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

	public bool NetworkisBroken
	{
		get
		{
			return this.isBroken;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this.isBroken, 1UL, null);
		}
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
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteBool(this.isBroken);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.isBroken, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.isBroken, null, reader.ReadBool());
		}
	}

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
}

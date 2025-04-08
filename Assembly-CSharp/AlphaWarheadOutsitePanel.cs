using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

public class AlphaWarheadOutsitePanel : NetworkBehaviour
{
	public static AlphaWarheadNukesitePanel nukeside
	{
		get
		{
			return AlphaWarheadNukesitePanel.Singleton;
		}
	}

	private void Update()
	{
		if (AlphaWarheadOutsitePanel.nukeside == null)
		{
			return;
		}
		base.transform.localPosition = new Vector3(0f, 0f, 9f);
		this.panelButtonCoverAnim.SetBool(AlphaWarheadOutsitePanel.Enabled, this.keycardEntered);
	}

	public override bool Weaved()
	{
		return true;
	}

	public bool NetworkkeycardEntered
	{
		get
		{
			return this.keycardEntered;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<bool>(value, ref this.keycardEntered, 1UL, null);
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this.keycardEntered);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteBool(this.keycardEntered);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.keycardEntered, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<bool>(ref this.keycardEntered, null, reader.ReadBool());
		}
	}

	public Animator panelButtonCoverAnim;

	[SyncVar]
	public bool keycardEntered;

	private static readonly int Enabled = Animator.StringToHash("enabled");
}

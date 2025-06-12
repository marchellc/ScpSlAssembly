using System.Linq;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace PlayerRoles.Voice;

public class IntercomDisplay : NetworkBehaviour
{
	private enum IcomText
	{
		Ready,
		Transmitting,
		TrasmittingBypass,
		Restarting,
		AdminUsing,
		Muted,
		Unknown,
		Wait
	}

	private static IntercomDisplay _singleton;

	[SyncVar]
	private string _overrideText;

	private Intercom _icom;

	private string[] _translations;

	private bool[] _translationsSet;

	public string Network_overrideText
	{
		get
		{
			return this._overrideText;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._overrideText, 1uL, null);
		}
	}

	private void Awake()
	{
		this._icom = base.GetComponent<Intercom>();
		IntercomDisplay._singleton = this;
		int num = 0;
		foreach (int item in EnumUtils<IcomText>.Values.ToArray().Cast<int>())
		{
			num = Mathf.Max(num, item + 1);
		}
		this._translations = new string[num];
		this._translationsSet = new bool[num];
	}

	public static bool TrySetDisplay(string str)
	{
		if (IntercomDisplay._singleton == null)
		{
			return false;
		}
		IntercomDisplay._singleton.Network_overrideText = str;
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
			writer.WriteString(this._overrideText);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteString(this._overrideText);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._overrideText, null, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._overrideText, null, reader.ReadString());
		}
	}
}

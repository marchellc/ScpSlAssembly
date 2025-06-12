using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using Mirror;
using Scp914;
using UnityEngine;
using VoiceChat.Playbacks;

namespace InventorySystem.Items.Radio;

public class RadioPickup : CollisionDetectionPickup, IUpgradeTrigger
{
	[SyncVar]
	public bool SavedEnabled;

	[SyncVar]
	public byte SavedRange;

	public float SavedBattery;

	private static RadioItem _radioCache;

	private static bool _radioCacheSet;

	[SerializeField]
	private Material _enabledMat;

	[SerializeField]
	private Material _disabledMat;

	[SerializeField]
	private Renderer _targetRenderer;

	[SerializeField]
	private GameObject _activeObject;

	[SerializeField]
	private SpatializedRadioPlaybackBase _playback;

	private bool _prevEnabled;

	public bool NetworkSavedEnabled
	{
		get
		{
			return this.SavedEnabled;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.SavedEnabled, 2uL, null);
		}
	}

	public byte NetworkSavedRange
	{
		get
		{
			return this.SavedRange;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.SavedRange, 4uL, null);
		}
	}

	private void Update()
	{
		this._playback.RangeId = this.SavedRange;
		if (this._prevEnabled != this.SavedEnabled)
		{
			bool flag = (this._prevEnabled = this.SavedEnabled);
			this._activeObject.SetActive(flag);
			this._targetRenderer.sharedMaterial = (flag ? this._enabledMat : this._disabledMat);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (!RadioPickup._radioCacheSet)
		{
			RadioPickup._radioCacheSet = InventoryItemLoader.TryGetItem<RadioItem>(ItemType.Radio, out RadioPickup._radioCache);
		}
	}

	private void LateUpdate()
	{
		if (NetworkServer.active && this.SavedEnabled && RadioPickup._radioCacheSet)
		{
			float num = RadioPickup._radioCache.Ranges[this.SavedRange].MinuteCostWhenIdle / 60f;
			float num2 = this.SavedBattery - Time.deltaTime * num / 100f;
			if (num2 <= 0f)
			{
				this.NetworkSavedEnabled = false;
				num2 = 0f;
			}
			this.SavedBattery = num2;
		}
	}

	public void ServerOnUpgraded(Scp914KnobSetting setting)
	{
		this.SavedBattery = 1f;
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
			writer.WriteBool(this.SavedEnabled);
			NetworkWriterExtensions.WriteByte(writer, this.SavedRange);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteBool(this.SavedEnabled);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this.SavedRange);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.SavedEnabled, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this.SavedRange, null, NetworkReaderExtensions.ReadByte(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.SavedEnabled, null, reader.ReadBool());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.SavedRange, null, NetworkReaderExtensions.ReadByte(reader));
		}
	}
}

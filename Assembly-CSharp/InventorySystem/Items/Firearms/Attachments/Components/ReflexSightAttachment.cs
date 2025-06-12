using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components;

public class ReflexSightAttachment : SerializableAttachment, ICustomizableAttachment
{
	private static readonly Dictionary<ushort, Dictionary<byte, ReflexSightSyncData>> SyncData = new Dictionary<ushort, Dictionary<byte, ReflexSightSyncData>>();

	public static readonly float[] Sizes = new float[9] { 0.6f, 0.7f, 0.8f, 0.9f, 1f, 1.2f, 1.4f, 1.6f, 1.8f };

	public static readonly float[] BrightnessLevels = new float[6] { 0f, 0.17f, 0.31f, 0.45f, 0.68f, 1f };

	public static readonly Color32[] Colors = new Color32[12]
	{
		new Color32(byte.MaxValue, 0, 0, byte.MaxValue),
		new Color32(0, 42, byte.MaxValue, byte.MaxValue),
		new Color32(0, 166, byte.MaxValue, byte.MaxValue),
		new Color32(0, byte.MaxValue, 0, byte.MaxValue),
		new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue),
		new Color32(byte.MaxValue, 85, 0, byte.MaxValue),
		new Color32(byte.MaxValue, 0, 107, byte.MaxValue),
		new Color32(156, 0, byte.MaxValue, byte.MaxValue),
		new Color32(byte.MaxValue, 0, 172, byte.MaxValue),
		new Color32(78, 0, byte.MaxValue, byte.MaxValue),
		new Color32(177, byte.MaxValue, 0, byte.MaxValue),
		new Color32(0, byte.MaxValue, 166, byte.MaxValue)
	};

	public Action OnValuesChanged;

	public ReflexSightReticlePack TextureOptions;

	private const int DefaultSize = 4;

	private const int DefaultBrightness = 0;

	private const int UnnamedPresetIndex = 0;

	private const string TexturePrefsKey = "Texture";

	private const string ColorPrefsKey = "Color";

	private const string SizePrefsKey = "Size";

	private const string BrightnessPrefsKey = "Brightness";

	private bool _serverPrefsReceived;

	[SerializeField]
	private int _defaultColorId;

	[SerializeField]
	private int _defaultReticle;

	[SerializeField]
	private Vector2 _configIconOffset;

	[SerializeField]
	private float _configIconSize;

	[SerializeField]
	private AttachmentConfigWindow _configWindow;

	public Vector2 ConfigIconOffset => this._configIconOffset;

	public AttachmentConfigWindow ConfigWindow => this._configWindow;

	public float ConfigIconScale => this._configIconSize;

	public override bool AllowCmdsWhileHolstered => true;

	public int CurTextureIndex { get; set; }

	public int CurSizeIndex { get; set; }

	public int CurColorIndex { get; set; }

	public int CurBrightnessIndex { get; set; }

	public void SetValues(int texture, int color, int size, int brightness)
	{
		this.CurTextureIndex = Mathf.Clamp(texture, 0, this.TextureOptions.Length - 1);
		this.CurColorIndex = Mathf.Clamp(color, 0, ReflexSightAttachment.Colors.Length - 1);
		this.CurSizeIndex = Mathf.Clamp(size, 0, ReflexSightAttachment.Sizes.Length - 1);
		this.CurBrightnessIndex = Mathf.Clamp(brightness, 0, ReflexSightAttachment.BrightnessLevels.Length - 1);
		this.OnValuesChanged?.Invoke();
	}

	public void ModifyValues(int? texture = null, int? color = null, int? size = null, int? brightness = null)
	{
		this.SetValues(texture.GetValueOrDefault(this.CurTextureIndex), color.GetValueOrDefault(this.CurColorIndex), size.GetValueOrDefault(this.CurSizeIndex), brightness.GetValueOrDefault(this.CurBrightnessIndex));
		this.SaveValues();
		this.SendCmd(delegate(NetworkWriter writer)
		{
			ReflexSightSyncData reflexSightSyncData = new ReflexSightSyncData(this);
			writer.WriteBool(value: true);
			reflexSightSyncData.Write(writer);
		});
	}

	public void SaveValues()
	{
		int preset = AttachmentPreferences.GetPreset(base.Firearm.ItemTypeId);
		SaveAs(preset);
		SaveAs(0);
		void SaveAs(int presetId)
		{
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Texture"), this.CurTextureIndex);
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Color"), this.CurColorIndex);
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Size"), this.CurSizeIndex);
			PlayerPrefsSl.Set(this.GetPrefsKey(presetId, "Brightness"), this.CurBrightnessIndex);
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		this.SetDatabaseEntry(new ReflexSightSyncData(reader), serial);
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		new ReflexSightSyncData(reader).Apply(this);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		bool flag = reader.ReadBool();
		if (!flag)
		{
			if (this._serverPrefsReceived || !base.Firearm.ServerIsPersonal)
			{
				return;
			}
			this._serverPrefsReceived = true;
		}
		else if (!AttachmentsServerHandler.AnyWorkstationsNearby(base.Firearm.Owner))
		{
			return;
		}
		ReflexSightSyncData data = new ReflexSightSyncData(reader);
		this.SetDatabaseEntry(data);
		this.ServerSendData(data, flag);
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		if (base.IsLocalPlayer)
		{
			this.SendUserPrefs(changeRequest: false);
		}
		if (this.TryGetFromDatabase(out var data))
		{
			data.Apply(this);
		}
		else
		{
			this.SetValues(this._defaultReticle, this._defaultColorId, 4, 0);
		}
		if (base.IsServer && !base.Firearm.ServerIsPersonal)
		{
			this.ServerSendData(data, onlySpectators: false);
		}
	}

	internal override void SpectatorInit()
	{
		base.SpectatorInit();
		if (this.TryGetFromDatabase(out var data))
		{
			data.Apply(this);
		}
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (base.IsServer && this.IsEnabled && this.TryGetFromDatabase(out var data))
		{
			this.ServerSendData(data, onlySpectators: true);
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		ReflexSightAttachment.SyncData.Clear();
	}

	protected override void Awake()
	{
		base.Awake();
		AttachmentSelectorBase.OnPresetLoaded = (Action)Delegate.Combine(AttachmentSelectorBase.OnPresetLoaded, new Action(LoadFromPreset));
		AttachmentSelectorBase.OnPresetSaved = (Action)Delegate.Combine(AttachmentSelectorBase.OnPresetSaved, new Action(SaveValues));
		AttachmentSelectorBase.OnAttachmentsReset = (Action)Delegate.Combine(AttachmentSelectorBase.OnAttachmentsReset, new Action(ResetToDefault));
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private void OnDestroy()
	{
		AttachmentSelectorBase.OnPresetLoaded = (Action)Delegate.Remove(AttachmentSelectorBase.OnPresetLoaded, new Action(LoadFromPreset));
		AttachmentSelectorBase.OnPresetSaved = (Action)Delegate.Remove(AttachmentSelectorBase.OnPresetSaved, new Action(SaveValues));
		AttachmentSelectorBase.OnAttachmentsReset = (Action)Delegate.Remove(AttachmentSelectorBase.OnAttachmentsReset, new Action(ResetToDefault));
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (this.IsEnabled && base.IsServer && newRole is SpectatorRole && this.TryGetFromDatabase(out var data))
		{
			this.SendRpc(userHub, ((ReflexSightSyncData)data).Write);
		}
	}

	private void ServerSendData(ReflexSightSyncData data, bool onlySpectators)
	{
		ReferenceHub owner = base.Firearm.Owner;
		if (onlySpectators)
		{
			this.SendRpc(owner.IsSpectatedBy, ((ReflexSightSyncData)data).Write);
			return;
		}
		this.SendRpc((ReferenceHub x) => x == owner || owner.IsSpectatedBy(x), ((ReflexSightSyncData)data).Write);
	}

	private void SetDatabaseEntry(ReflexSightSyncData data, ushort? serial = null)
	{
		ushort valueOrDefault = serial.GetValueOrDefault();
		if (!serial.HasValue)
		{
			valueOrDefault = base.ItemSerial;
			serial = valueOrDefault;
		}
		ReflexSightAttachment.SyncData.GetOrAdd(serial.Value, () => new Dictionary<byte, ReflexSightSyncData>())[base.Index] = data;
	}

	private bool TryGetFromDatabase(out ReflexSightSyncData data)
	{
		if (ReflexSightAttachment.SyncData.TryGetValue(base.ItemSerial, out var value))
		{
			return value.TryGetValue(base.Index, out data);
		}
		data = default(ReflexSightSyncData);
		return false;
	}

	private string GetPrefsKey(int preset, string setting)
	{
		return $"ReflexSight_{preset}_{base.Firearm.ItemTypeId}_{base.Index}_{setting}";
	}

	private void SendUserPrefs(bool changeRequest)
	{
		if (base.IsLocalPlayer)
		{
			int preset = AttachmentPreferences.GetPreset(base.Firearm.ItemTypeId);
			ReflexSightSyncData data = new ReflexSightSyncData(PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Color"), this._defaultColorId), PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Brightness"), 0), PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Size"), 4), PlayerPrefsSl.Get(this.GetPrefsKey(preset, "Texture"), this._defaultReticle));
			this.SendCmd(delegate(NetworkWriter writer)
			{
				writer.WriteBool(changeRequest);
				data.Write(writer);
			});
			if (changeRequest)
			{
				data.Apply(this);
			}
		}
	}

	private void LoadFromPreset()
	{
		this.SendUserPrefs(changeRequest: true);
	}

	private void ResetToDefault()
	{
		this.ModifyValues(this._defaultReticle, this._defaultColorId, 4, 0);
	}
}

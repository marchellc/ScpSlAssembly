using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;

namespace AdminToys;

public class LightSourceToy : AdminToyBase
{
	[SerializeField]
	private Light _light;

	[SyncVar(hook = "SetIntensity")]
	public float LightIntensity;

	[SyncVar(hook = "SetRange")]
	public float LightRange;

	[SyncVar(hook = "SetColor")]
	public Color LightColor;

	[SyncVar(hook = "SetShadows")]
	public LightShadows ShadowType;

	[SyncVar(hook = "SetShadowStrength")]
	public float ShadowStrength;

	[SyncVar(hook = "SetType")]
	public LightType LightType;

	[SyncVar(hook = "SetShape")]
	public LightShape LightShape;

	[SyncVar(hook = "SetSpotAngle")]
	public float SpotAngle;

	[SyncVar(hook = "SetInnerSpotAngle")]
	public float InnerSpotAngle;

	public override string CommandName => "LightSource";

	public float NetworkLightIntensity
	{
		get
		{
			return this.LightIntensity;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.LightIntensity, 32uL, SetIntensity);
		}
	}

	public float NetworkLightRange
	{
		get
		{
			return this.LightRange;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.LightRange, 64uL, SetRange);
		}
	}

	public Color NetworkLightColor
	{
		get
		{
			return this.LightColor;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.LightColor, 128uL, SetColor);
		}
	}

	public LightShadows NetworkShadowType
	{
		get
		{
			return this.ShadowType;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.ShadowType, 256uL, SetShadows);
		}
	}

	public float NetworkShadowStrength
	{
		get
		{
			return this.ShadowStrength;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.ShadowStrength, 512uL, SetShadowStrength);
		}
	}

	public LightType NetworkLightType
	{
		get
		{
			return this.LightType;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.LightType, 1024uL, SetType);
		}
	}

	public LightShape NetworkLightShape
	{
		get
		{
			return this.LightShape;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.LightShape, 2048uL, SetShape);
		}
	}

	public float NetworkSpotAngle
	{
		get
		{
			return this.SpotAngle;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.SpotAngle, 4096uL, SetSpotAngle);
		}
	}

	public float NetworkInnerSpotAngle
	{
		get
		{
			return this.InnerSpotAngle;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.InnerSpotAngle, 8192uL, SetInnerSpotAngle);
		}
	}

	public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		base.OnSpawned(admin, arguments);
		base.transform.position = admin.PlayerCameraReference.position;
		base.transform.localScale = Vector3.one;
	}

	protected override void Start()
	{
		base.Start();
		if (!UserSetting<bool>.Get(LightingVideoSetting.RenderShadows))
		{
			this._light.shadows = LightShadows.None;
		}
	}

	private void SetIntensity(float oldIntensity, float newIntensity)
	{
		this._light.intensity = newIntensity;
	}

	private void SetRange(float oldRange, float newRange)
	{
		this._light.range = newRange;
	}

	private void SetColor(Color oldColor, Color newColor)
	{
		this._light.color = newColor;
	}

	private void SetShadows(LightShadows oldShadows, LightShadows newShadows)
	{
		if (!UserSetting<bool>.Get(LightingVideoSetting.RenderShadows))
		{
			this._light.shadows = LightShadows.None;
		}
		else
		{
			this._light.shadows = ((this.LightType != LightType.Directional) ? newShadows : LightShadows.None);
		}
	}

	private void SetShadowStrength(float oldStrength, float newStrength)
	{
		this._light.shadowStrength = newStrength;
	}

	private void SetType(LightType oldType, LightType newType)
	{
		if (newType == LightType.Directional)
		{
			this.NetworkShadowType = LightShadows.None;
		}
		if (newType > LightType.Point)
		{
			this.NetworkLightType = LightType.Point;
		}
		else
		{
			this._light.type = newType;
		}
	}

	private void SetShape(LightShape oldShape, LightShape newShape)
	{
		this._light.shape = newShape;
	}

	private void SetSpotAngle(float oldAngle, float newAngle)
	{
		this._light.spotAngle = newAngle;
	}

	private void SetInnerSpotAngle(float oldAngle, float newAngle)
	{
		this._light.innerSpotAngle = newAngle;
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
			writer.WriteFloat(this.LightIntensity);
			writer.WriteFloat(this.LightRange);
			writer.WriteColor(this.LightColor);
			GeneratedNetworkCode._Write_UnityEngine_002ELightShadows(writer, this.ShadowType);
			writer.WriteFloat(this.ShadowStrength);
			GeneratedNetworkCode._Write_UnityEngine_002ELightType(writer, this.LightType);
			GeneratedNetworkCode._Write_UnityEngine_002ELightShape(writer, this.LightShape);
			writer.WriteFloat(this.SpotAngle);
			writer.WriteFloat(this.InnerSpotAngle);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteFloat(this.LightIntensity);
		}
		if ((base.syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteFloat(this.LightRange);
		}
		if ((base.syncVarDirtyBits & 0x80L) != 0L)
		{
			writer.WriteColor(this.LightColor);
		}
		if ((base.syncVarDirtyBits & 0x100L) != 0L)
		{
			GeneratedNetworkCode._Write_UnityEngine_002ELightShadows(writer, this.ShadowType);
		}
		if ((base.syncVarDirtyBits & 0x200L) != 0L)
		{
			writer.WriteFloat(this.ShadowStrength);
		}
		if ((base.syncVarDirtyBits & 0x400L) != 0L)
		{
			GeneratedNetworkCode._Write_UnityEngine_002ELightType(writer, this.LightType);
		}
		if ((base.syncVarDirtyBits & 0x800L) != 0L)
		{
			GeneratedNetworkCode._Write_UnityEngine_002ELightShape(writer, this.LightShape);
		}
		if ((base.syncVarDirtyBits & 0x1000L) != 0L)
		{
			writer.WriteFloat(this.SpotAngle);
		}
		if ((base.syncVarDirtyBits & 0x2000L) != 0L)
		{
			writer.WriteFloat(this.InnerSpotAngle);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightIntensity, SetIntensity, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.LightRange, SetRange, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.LightColor, SetColor, reader.ReadColor());
			base.GeneratedSyncVarDeserialize(ref this.ShadowType, SetShadows, GeneratedNetworkCode._Read_UnityEngine_002ELightShadows(reader));
			base.GeneratedSyncVarDeserialize(ref this.ShadowStrength, SetShadowStrength, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.LightType, SetType, GeneratedNetworkCode._Read_UnityEngine_002ELightType(reader));
			base.GeneratedSyncVarDeserialize(ref this.LightShape, SetShape, GeneratedNetworkCode._Read_UnityEngine_002ELightShape(reader));
			base.GeneratedSyncVarDeserialize(ref this.SpotAngle, SetSpotAngle, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.InnerSpotAngle, SetInnerSpotAngle, reader.ReadFloat());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightIntensity, SetIntensity, reader.ReadFloat());
		}
		if ((num & 0x40L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightRange, SetRange, reader.ReadFloat());
		}
		if ((num & 0x80L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightColor, SetColor, reader.ReadColor());
		}
		if ((num & 0x100L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ShadowType, SetShadows, GeneratedNetworkCode._Read_UnityEngine_002ELightShadows(reader));
		}
		if ((num & 0x200L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.ShadowStrength, SetShadowStrength, reader.ReadFloat());
		}
		if ((num & 0x400L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightType, SetType, GeneratedNetworkCode._Read_UnityEngine_002ELightType(reader));
		}
		if ((num & 0x800L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.LightShape, SetShape, GeneratedNetworkCode._Read_UnityEngine_002ELightShape(reader));
		}
		if ((num & 0x1000L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.SpotAngle, SetSpotAngle, reader.ReadFloat());
		}
		if ((num & 0x2000L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.InnerSpotAngle, SetInnerSpotAngle, reader.ReadFloat());
		}
	}
}

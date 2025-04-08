using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;

namespace AdminToys
{
	public class LightSourceToy : AdminToyBase
	{
		public override string CommandName
		{
			get
			{
				return "LightSource";
			}
		}

		public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
		{
			base.OnSpawned(admin, arguments);
			base.transform.position = admin.PlayerCameraReference.position;
			base.transform.localScale = Vector3.one;
		}

		private void Start()
		{
			if (!UserSetting<bool>.Get<LightingVideoSetting>(LightingVideoSetting.RenderShadows))
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
			if (!UserSetting<bool>.Get<LightingVideoSetting>(LightingVideoSetting.RenderShadows))
			{
				this._light.shadows = LightShadows.None;
				return;
			}
			this._light.shadows = ((this.LightType == LightType.Directional) ? LightShadows.None : newShadows);
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
				return;
			}
			this._light.type = newType;
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

		public float NetworkLightIntensity
		{
			get
			{
				return this.LightIntensity;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this.LightIntensity, 32UL, new Action<float, float>(this.SetIntensity));
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
				base.GeneratedSyncVarSetter<float>(value, ref this.LightRange, 64UL, new Action<float, float>(this.SetRange));
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
				base.GeneratedSyncVarSetter<Color>(value, ref this.LightColor, 128UL, new Action<Color, Color>(this.SetColor));
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
				base.GeneratedSyncVarSetter<LightShadows>(value, ref this.ShadowType, 256UL, new Action<LightShadows, LightShadows>(this.SetShadows));
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
				base.GeneratedSyncVarSetter<float>(value, ref this.ShadowStrength, 512UL, new Action<float, float>(this.SetShadowStrength));
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
				base.GeneratedSyncVarSetter<LightType>(value, ref this.LightType, 1024UL, new Action<LightType, LightType>(this.SetType));
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
				base.GeneratedSyncVarSetter<LightShape>(value, ref this.LightShape, 2048UL, new Action<LightShape, LightShape>(this.SetShape));
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
				base.GeneratedSyncVarSetter<float>(value, ref this.SpotAngle, 4096UL, new Action<float, float>(this.SetSpotAngle));
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
				base.GeneratedSyncVarSetter<float>(value, ref this.InnerSpotAngle, 8192UL, new Action<float, float>(this.SetInnerSpotAngle));
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteFloat(this.LightIntensity);
				writer.WriteFloat(this.LightRange);
				writer.WriteColor(this.LightColor);
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.LightShadows(writer, this.ShadowType);
				writer.WriteFloat(this.ShadowStrength);
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.LightType(writer, this.LightType);
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.LightShape(writer, this.LightShape);
				writer.WriteFloat(this.SpotAngle);
				writer.WriteFloat(this.InnerSpotAngle);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 32UL) != 0UL)
			{
				writer.WriteFloat(this.LightIntensity);
			}
			if ((base.syncVarDirtyBits & 64UL) != 0UL)
			{
				writer.WriteFloat(this.LightRange);
			}
			if ((base.syncVarDirtyBits & 128UL) != 0UL)
			{
				writer.WriteColor(this.LightColor);
			}
			if ((base.syncVarDirtyBits & 256UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.LightShadows(writer, this.ShadowType);
			}
			if ((base.syncVarDirtyBits & 512UL) != 0UL)
			{
				writer.WriteFloat(this.ShadowStrength);
			}
			if ((base.syncVarDirtyBits & 1024UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.LightType(writer, this.LightType);
			}
			if ((base.syncVarDirtyBits & 2048UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.LightShape(writer, this.LightShape);
			}
			if ((base.syncVarDirtyBits & 4096UL) != 0UL)
			{
				writer.WriteFloat(this.SpotAngle);
			}
			if ((base.syncVarDirtyBits & 8192UL) != 0UL)
			{
				writer.WriteFloat(this.InnerSpotAngle);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.LightIntensity, new Action<float, float>(this.SetIntensity), reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<float>(ref this.LightRange, new Action<float, float>(this.SetRange), reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<Color>(ref this.LightColor, new Action<Color, Color>(this.SetColor), reader.ReadColor());
				base.GeneratedSyncVarDeserialize<LightShadows>(ref this.ShadowType, new Action<LightShadows, LightShadows>(this.SetShadows), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.LightShadows(reader));
				base.GeneratedSyncVarDeserialize<float>(ref this.ShadowStrength, new Action<float, float>(this.SetShadowStrength), reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<LightType>(ref this.LightType, new Action<LightType, LightType>(this.SetType), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.LightType(reader));
				base.GeneratedSyncVarDeserialize<LightShape>(ref this.LightShape, new Action<LightShape, LightShape>(this.SetShape), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.LightShape(reader));
				base.GeneratedSyncVarDeserialize<float>(ref this.SpotAngle, new Action<float, float>(this.SetSpotAngle), reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<float>(ref this.InnerSpotAngle, new Action<float, float>(this.SetInnerSpotAngle), reader.ReadFloat());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 32L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.LightIntensity, new Action<float, float>(this.SetIntensity), reader.ReadFloat());
			}
			if ((num & 64L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.LightRange, new Action<float, float>(this.SetRange), reader.ReadFloat());
			}
			if ((num & 128L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<Color>(ref this.LightColor, new Action<Color, Color>(this.SetColor), reader.ReadColor());
			}
			if ((num & 256L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<LightShadows>(ref this.ShadowType, new Action<LightShadows, LightShadows>(this.SetShadows), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.LightShadows(reader));
			}
			if ((num & 512L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.ShadowStrength, new Action<float, float>(this.SetShadowStrength), reader.ReadFloat());
			}
			if ((num & 1024L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<LightType>(ref this.LightType, new Action<LightType, LightType>(this.SetType), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.LightType(reader));
			}
			if ((num & 2048L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<LightShape>(ref this.LightShape, new Action<LightShape, LightShape>(this.SetShape), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.LightShape(reader));
			}
			if ((num & 4096L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.SpotAngle, new Action<float, float>(this.SetSpotAngle), reader.ReadFloat());
			}
			if ((num & 8192L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this.InnerSpotAngle, new Action<float, float>(this.SetInnerSpotAngle), reader.ReadFloat());
			}
		}

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
	}
}

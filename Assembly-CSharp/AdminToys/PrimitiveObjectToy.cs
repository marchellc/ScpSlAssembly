using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace AdminToys
{
	public class PrimitiveObjectToy : AdminToyBase
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				PrimitiveObjectToy.CachedMaterials.Clear();
				PrimitiveObjectToy.PrimitiveTypeToMesh.Clear();
				foreach (PrimitiveType primitiveType in EnumUtils<PrimitiveType>.Values)
				{
					GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
					Mesh sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
					PrimitiveObjectToy.PrimitiveTypeToMesh.Add(primitiveType, sharedMesh);
					global::UnityEngine.Object.Destroy(gameObject);
				}
			};
		}

		public override string CommandName
		{
			get
			{
				return "PrimitiveObject";
			}
		}

		public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
		{
			string[] array = arguments.Array;
			PrimitiveType primitiveType;
			this.NetworkPrimitiveType = ((array.Length > 2 && Enum.TryParse<PrimitiveType>(array[2], true, out primitiveType)) ? primitiveType : PrimitiveType.Sphere);
			Color color;
			this.NetworkMaterialColor = ((array.Length > 3 && ColorUtility.TryParseHtmlString(array[3], out color)) ? color : Color.gray);
			float num2;
			float num = ((array.Length > 4 && float.TryParse(array[4], out num2)) ? num2 : 1f);
			PrimitiveFlags primitiveFlags;
			this.NetworkPrimitiveFlags = ((array.Length > 5 && Enum.TryParse<PrimitiveFlags>(array[5], true, out primitiveFlags)) ? primitiveFlags : ((PrimitiveFlags)255));
			base.transform.SetPositionAndRotation(admin.PlayerCameraReference.position, admin.PlayerCameraReference.rotation);
			base.transform.localScale = Vector3.one * num;
			base.NetworkScale = base.transform.localScale;
			base.OnSpawned(admin, arguments);
		}

		private void Start()
		{
			this.SetPrimitive(PrimitiveType.Sphere, this.PrimitiveType);
		}

		private void SetPrimitive(PrimitiveType _, PrimitiveType newPrim)
		{
			this._filter.sharedMesh = PrimitiveObjectToy.PrimitiveTypeToMesh[newPrim];
			if (this._collider != null)
			{
				global::UnityEngine.Object.Destroy(this._collider);
			}
			if (newPrim == PrimitiveType.Cube)
			{
				this._collider = base.gameObject.AddComponent<BoxCollider>();
			}
			else
			{
				MeshCollider meshCollider = base.gameObject.AddComponent<MeshCollider>();
				bool flag = newPrim != PrimitiveType.Plane && newPrim != PrimitiveType.Quad;
				meshCollider.convex = flag;
				this._collider = meshCollider;
			}
			this.SetColor(Color.clear, this.MaterialColor);
			this.SetFlags(PrimitiveFlags.None, this.PrimitiveFlags);
		}

		private void SetColor(Color _, Color newColor)
		{
			this._renderer.sharedMaterial = this.GetMaterialFromColor(newColor);
		}

		private Material GetMaterialFromColor(Color color)
		{
			Material material;
			if (PrimitiveObjectToy.CachedMaterials.TryGetValue(color, out material))
			{
				return material;
			}
			material = ((color.a >= 1f) ? new Material(this._regularMatTemplate) : new Material(this._transparentMatTemplate));
			material.SetColor(PrimitiveObjectToy.BaseColor, color);
			PrimitiveObjectToy.CachedMaterials.Add(color, material);
			return material;
		}

		private void SetFlags(PrimitiveFlags _, PrimitiveFlags newPrimitiveFlags)
		{
			if (this._collider != null)
			{
				this._collider.enabled = newPrimitiveFlags.HasFlag(PrimitiveFlags.Collidable);
			}
			this._renderer.enabled = newPrimitiveFlags.HasFlag(PrimitiveFlags.Visible);
		}

		public override bool Weaved()
		{
			return true;
		}

		public PrimitiveType NetworkPrimitiveType
		{
			get
			{
				return this.PrimitiveType;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<PrimitiveType>(value, ref this.PrimitiveType, 32UL, new Action<PrimitiveType, PrimitiveType>(this.SetPrimitive));
			}
		}

		public Color NetworkMaterialColor
		{
			get
			{
				return this.MaterialColor;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<Color>(value, ref this.MaterialColor, 64UL, new Action<Color, Color>(this.SetColor));
			}
		}

		public PrimitiveFlags NetworkPrimitiveFlags
		{
			get
			{
				return this.PrimitiveFlags;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<PrimitiveFlags>(value, ref this.PrimitiveFlags, 128UL, new Action<PrimitiveFlags, PrimitiveFlags>(this.SetFlags));
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.PrimitiveType(writer, this.PrimitiveType);
				writer.WriteColor(this.MaterialColor);
				global::Mirror.GeneratedNetworkCode._Write_AdminToys.PrimitiveFlags(writer, this.PrimitiveFlags);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 32UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_UnityEngine.PrimitiveType(writer, this.PrimitiveType);
			}
			if ((base.syncVarDirtyBits & 64UL) != 0UL)
			{
				writer.WriteColor(this.MaterialColor);
			}
			if ((base.syncVarDirtyBits & 128UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_AdminToys.PrimitiveFlags(writer, this.PrimitiveFlags);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<PrimitiveType>(ref this.PrimitiveType, new Action<PrimitiveType, PrimitiveType>(this.SetPrimitive), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.PrimitiveType(reader));
				base.GeneratedSyncVarDeserialize<Color>(ref this.MaterialColor, new Action<Color, Color>(this.SetColor), reader.ReadColor());
				base.GeneratedSyncVarDeserialize<PrimitiveFlags>(ref this.PrimitiveFlags, new Action<PrimitiveFlags, PrimitiveFlags>(this.SetFlags), global::Mirror.GeneratedNetworkCode._Read_AdminToys.PrimitiveFlags(reader));
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 32L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<PrimitiveType>(ref this.PrimitiveType, new Action<PrimitiveType, PrimitiveType>(this.SetPrimitive), global::Mirror.GeneratedNetworkCode._Read_UnityEngine.PrimitiveType(reader));
			}
			if ((num & 64L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<Color>(ref this.MaterialColor, new Action<Color, Color>(this.SetColor), reader.ReadColor());
			}
			if ((num & 128L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<PrimitiveFlags>(ref this.PrimitiveFlags, new Action<PrimitiveFlags, PrimitiveFlags>(this.SetFlags), global::Mirror.GeneratedNetworkCode._Read_AdminToys.PrimitiveFlags(reader));
			}
		}

		private static readonly Dictionary<Color, Material> CachedMaterials = new Dictionary<Color, Material>();

		private static readonly Dictionary<PrimitiveType, Mesh> PrimitiveTypeToMesh = new Dictionary<PrimitiveType, Mesh>(6);

		private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

		[SerializeField]
		private Material _regularMatTemplate;

		[SerializeField]
		private Material _transparentMatTemplate;

		[SerializeField]
		private MeshFilter _filter;

		[SerializeField]
		private MeshRenderer _renderer;

		private Collider _collider;

		[SyncVar(hook = "SetPrimitive")]
		public PrimitiveType PrimitiveType;

		[SyncVar(hook = "SetColor")]
		public Color MaterialColor;

		[SyncVar(hook = "SetFlags")]
		public PrimitiveFlags PrimitiveFlags = PrimitiveFlags.Collidable | PrimitiveFlags.Visible;
	}
}

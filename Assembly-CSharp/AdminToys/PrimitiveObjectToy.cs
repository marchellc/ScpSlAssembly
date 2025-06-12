using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace AdminToys;

public class PrimitiveObjectToy : AdminToyBase
{
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

	public override string CommandName => "PrimitiveObject";

	public PrimitiveType NetworkPrimitiveType
	{
		get
		{
			return this.PrimitiveType;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.PrimitiveType, 32uL, SetPrimitive);
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
			base.GeneratedSyncVarSetter(value, ref this.MaterialColor, 64uL, SetColor);
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
			base.GeneratedSyncVarSetter(value, ref this.PrimitiveFlags, 128uL, SetFlags);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			PrimitiveObjectToy.CachedMaterials.Clear();
			PrimitiveObjectToy.PrimitiveTypeToMesh.Clear();
			PrimitiveType[] values = EnumUtils<PrimitiveType>.Values;
			foreach (PrimitiveType primitiveType in values)
			{
				GameObject obj = GameObject.CreatePrimitive(primitiveType);
				Mesh sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
				PrimitiveObjectToy.PrimitiveTypeToMesh.Add(primitiveType, sharedMesh);
				UnityEngine.Object.Destroy(obj);
			}
		};
	}

	public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		string[] array = arguments.Array;
		this.NetworkPrimitiveType = ((array.Length > 2 && Enum.TryParse<PrimitiveType>(array[2], ignoreCase: true, out var result)) ? result : PrimitiveType.Sphere);
		this.NetworkMaterialColor = ((array.Length > 3 && ColorUtility.TryParseHtmlString(array[3], out var color)) ? color : Color.gray);
		float result2;
		float num = ((array.Length > 4 && float.TryParse(array[4], out result2)) ? result2 : 1f);
		this.NetworkPrimitiveFlags = ((array.Length > 5 && Enum.TryParse<PrimitiveFlags>(array[5], ignoreCase: true, out var result3)) ? result3 : ((PrimitiveFlags)255));
		base.transform.SetPositionAndRotation(admin.PlayerCameraReference.position, admin.PlayerCameraReference.rotation);
		base.transform.localScale = Vector3.one * num;
		base.NetworkScale = base.transform.localScale;
		base.OnSpawned(admin, arguments);
	}

	protected override void Start()
	{
		base.Start();
		this.SetPrimitive(PrimitiveType.Sphere, this.PrimitiveType);
	}

	private void SetPrimitive(PrimitiveType _, PrimitiveType newPrim)
	{
		this._filter.sharedMesh = PrimitiveObjectToy.PrimitiveTypeToMesh[newPrim];
		if (this._collider != null)
		{
			UnityEngine.Object.Destroy(this._collider);
		}
		if (newPrim == PrimitiveType.Cube)
		{
			this._collider = base.gameObject.AddComponent<BoxCollider>();
		}
		else
		{
			MeshCollider meshCollider = base.gameObject.AddComponent<MeshCollider>();
			bool convex = newPrim != PrimitiveType.Plane && newPrim != PrimitiveType.Quad;
			meshCollider.convex = convex;
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
		if (PrimitiveObjectToy.CachedMaterials.TryGetValue(color, out var value))
		{
			return value;
		}
		value = ((color.a >= 1f) ? new Material(this._regularMatTemplate) : new Material(this._transparentMatTemplate));
		value.SetColor(PrimitiveObjectToy.BaseColor, color);
		PrimitiveObjectToy.CachedMaterials.Add(color, value);
		return value;
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

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			GeneratedNetworkCode._Write_UnityEngine_002EPrimitiveType(writer, this.PrimitiveType);
			writer.WriteColor(this.MaterialColor);
			GeneratedNetworkCode._Write_AdminToys_002EPrimitiveFlags(writer, this.PrimitiveFlags);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			GeneratedNetworkCode._Write_UnityEngine_002EPrimitiveType(writer, this.PrimitiveType);
		}
		if ((base.syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteColor(this.MaterialColor);
		}
		if ((base.syncVarDirtyBits & 0x80L) != 0L)
		{
			GeneratedNetworkCode._Write_AdminToys_002EPrimitiveFlags(writer, this.PrimitiveFlags);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.PrimitiveType, SetPrimitive, GeneratedNetworkCode._Read_UnityEngine_002EPrimitiveType(reader));
			base.GeneratedSyncVarDeserialize(ref this.MaterialColor, SetColor, reader.ReadColor());
			base.GeneratedSyncVarDeserialize(ref this.PrimitiveFlags, SetFlags, GeneratedNetworkCode._Read_AdminToys_002EPrimitiveFlags(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.PrimitiveType, SetPrimitive, GeneratedNetworkCode._Read_UnityEngine_002EPrimitiveType(reader));
		}
		if ((num & 0x40L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.MaterialColor, SetColor, reader.ReadColor());
		}
		if ((num & 0x80L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.PrimitiveFlags, SetFlags, GeneratedNetworkCode._Read_AdminToys_002EPrimitiveFlags(reader));
		}
	}
}

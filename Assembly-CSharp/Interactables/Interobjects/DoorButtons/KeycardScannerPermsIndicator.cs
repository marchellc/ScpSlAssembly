using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using MEC;
using UnityEngine;

namespace Interactables.Interobjects.DoorButtons;

public class KeycardScannerPermsIndicator : MonoBehaviour
{
	private readonly struct MaterialVariant : IEquatable<MaterialVariant>
	{
		public readonly Color32 Color;

		public readonly Vector2 Offset;

		public MaterialVariant(Color32 color, Vector2 offset)
		{
			this.Offset = offset;
			this.Color = new Color32(color.r, color.g, color.b, byte.MaxValue);
		}

		public Material CreateVariant(Material template)
		{
			Material material = new Material(template);
			material.SetTextureOffset(KeycardScannerPermsIndicator.EmissionTextureHash, this.Offset);
			material.SetColor(KeycardScannerPermsIndicator.EmissionColorHash, this.Color);
			material.mainTextureOffset = this.Offset;
			return material;
		}

		public bool Equals(MaterialVariant other)
		{
			if (this.Color.Equals(other.Color))
			{
				return this.Offset == other.Offset;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is MaterialVariant other)
			{
				return this.Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(this.Color, this.Offset);
		}
	}

	private static readonly Dictionary<MaterialVariant, Material> MaterialVariants = new Dictionary<MaterialVariant, Material>();

	private static readonly int EmissionTextureHash = Shader.PropertyToID("_EmissiveColorMap");

	private static readonly int EmissionColorHash = Shader.PropertyToID("_EmissiveColor");

	private static readonly Vector2 PermsOffsetContainment = new Vector2(0f, 0.5f);

	private static readonly Vector2 PermsOffsetArmory = new Vector2(0.5f, 0.5f);

	private static readonly Vector2 PermsOffsetAdmin = new Vector2(0f, 0f);

	private static readonly Color32 DefaultColor = new Color32(0, 72, 200, byte.MaxValue);

	private static readonly Color32 RedColor = Color.red;

	private static readonly Color32 GreenColor = Color.green;

	private static readonly Color32 InactiveColor = new Color32(10, 10, 15, byte.MaxValue);

	[SerializeField]
	private Material _trimMaterialTemplate;

	[SerializeField]
	private Renderer[] _targetIcons;

	[SerializeField]
	private int _minBlinkDelay;

	[SerializeField]
	private DoorPermissionFlags _ignoredPerms;

	private Material _defaultMat;

	private Material _redMat;

	private Material _greenMat;

	private Material _inactiveMat;

	private CoroutineHandle _lastHandle;

	private DoorPermissionFlags _requiredPerms;

	private int _requiredLevel;

	private int _lastAnimLevel;

	private bool _lastAccepted;

	private float? _lastAnimTime;

	public void Register(IDoorPermissionRequester requester)
	{
		this._requiredPerms = requester.PermissionsPolicy.RequiredPermissions;
		KeycardLevels keycardLevels = new KeycardLevels((DoorPermissionFlags)((uint)this._requiredPerms & (uint)(ushort)(~(int)this._ignoredPerms)));
		Vector2 offset;
		if (keycardLevels.Containment > 0)
		{
			this._requiredLevel = keycardLevels.Containment;
			offset = KeycardScannerPermsIndicator.PermsOffsetContainment;
		}
		else if (keycardLevels.Armory > 0)
		{
			this._requiredLevel = keycardLevels.Armory;
			offset = KeycardScannerPermsIndicator.PermsOffsetArmory;
		}
		else
		{
			this._requiredLevel = keycardLevels.Admin;
			offset = KeycardScannerPermsIndicator.PermsOffsetAdmin;
		}
		this._defaultMat = this.GetSharedMaterial(KeycardScannerPermsIndicator.DefaultColor, offset);
		this._redMat = this.GetSharedMaterial(KeycardScannerPermsIndicator.RedColor, offset);
		this._greenMat = this.GetSharedMaterial(KeycardScannerPermsIndicator.GreenColor, offset);
		this._inactiveMat = this.GetSharedMaterial(KeycardScannerPermsIndicator.InactiveColor, offset);
		this.ShowIdle();
	}

	public void ShowIdle()
	{
		if (this._lastHandle.IsRunning)
		{
			Timing.KillCoroutines(this._lastHandle);
		}
		for (int i = 0; i < this._targetIcons.Length; i++)
		{
			Material sharedMaterial = ((i >= this._requiredLevel) ? this._inactiveMat : this._defaultMat);
			this._targetIcons[i].sharedMaterial = sharedMaterial;
		}
		base.enabled = false;
	}

	public void PlayAccepted(float? duration)
	{
		this._lastAccepted = true;
		this._lastAnimLevel = this._requiredLevel;
		this._lastAnimTime = duration;
		this.Play();
	}

	public void PlayDenied(DoorPermissionFlags flags, float? duration)
	{
		KeycardLevels keycardLevels = new KeycardLevels(new KeycardLevels((DoorPermissionFlags)((uint)this._requiredPerms & (uint)(ushort)(~(int)this._ignoredPerms))).Permissions & flags);
		this._lastAccepted = false;
		this._lastAnimLevel = keycardLevels.HighestLevelValue;
		this._lastAnimTime = duration;
		this.Play();
	}

	private void Play()
	{
		base.enabled = true;
		if (this._lastHandle.IsRunning)
		{
			Timing.KillCoroutines(this._lastHandle);
		}
		this._lastHandle = Timing.RunCoroutine(this.PlayAnimation().CancelWith(base.gameObject), Segment.Update);
	}

	private IEnumerator<float> PlayAnimation()
	{
		double timestamp = Time.timeAsDouble;
		Renderer[] targetIcons = this._targetIcons;
		for (int i = 0; i < targetIcons.Length; i++)
		{
			targetIcons[i].sharedMaterial = this._inactiveMat;
		}
		yield return Timing.WaitForSeconds(0.1f);
		Material targetMat = (this._lastAccepted ? this._greenMat : this._redMat);
		for (int j = 0; j < Mathf.Max(this._lastAnimLevel, this._minBlinkDelay); j++)
		{
			if (j < this._lastAnimLevel && this._targetIcons.TryGet(j, out var element))
			{
				element.sharedMaterial = targetMat;
			}
			yield return Timing.WaitForSeconds(0.1f);
		}
		int blinkStartIndex = ((!this._lastAccepted) ? this._lastAnimLevel : 0);
		int blinkEndIndex = Mathf.Min(this._targetIcons.Length, this._requiredLevel);
		for (int j = 0; j < 6; j++)
		{
			Material sharedMaterial = ((j % 2 == 1) ? targetMat : this._inactiveMat);
			for (int k = blinkStartIndex; k < blinkEndIndex; k++)
			{
				this._targetIcons[k].sharedMaterial = sharedMaterial;
			}
			yield return Timing.WaitForSeconds(0.1f);
		}
		if (this._lastAnimTime.HasValue)
		{
			double num = Time.timeAsDouble - timestamp;
			double num2 = (double)this._lastAnimTime.Value - num;
			yield return Timing.WaitForSeconds((float)num2);
			this.ShowIdle();
		}
	}

	private Material GetSharedMaterial(Color32 color, Vector2 offset)
	{
		MaterialVariant key = new MaterialVariant(color, offset);
		if (!KeycardScannerPermsIndicator.MaterialVariants.TryGetValue(key, out var value))
		{
			value = key.CreateVariant(this._trimMaterialTemplate);
			KeycardScannerPermsIndicator.MaterialVariants.Add(key, value);
		}
		return value;
	}
}

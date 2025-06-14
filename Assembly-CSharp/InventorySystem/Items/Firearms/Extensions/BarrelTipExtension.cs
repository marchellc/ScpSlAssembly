using System;
using InventorySystem.Items.Firearms.Attachments;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class BarrelTipExtension : MixedExtension
{
	[Serializable]
	private class TipPosition
	{
		public AttachmentLink Attachment;

		public Vector3 Position;
	}

	private const float OffsetScale = 0.001f;

	[SerializeField]
	private Vector3 _localDirection = Vector3.forward;

	[SerializeField]
	private Vector3 _baselineOffset;

	[SerializeField]
	private TipPosition[] _attachmentOffsets;

	[SerializeField]
	[Header("These transforms will always be at the barrel tip position.")]
	private Transform[] _followers;

	private uint[] _cachedFilters;

	private Vector3 _curTipPosition;

	private Transform _cachedTr;

	private bool _transformCacheSet;

	public Vector3 WorldspaceDirection => this.CachedTr.TransformDirection(this._localDirection);

	public Vector3 WorldspacePosition => this.CachedTr.TransformPoint(this._curTipPosition);

	private Transform CachedTr
	{
		get
		{
			if (!this._transformCacheSet)
			{
				this._cachedTr = base.transform;
				this._transformCacheSet = true;
			}
			return this._cachedTr;
		}
	}

	public static bool TryFindWorldmodelBarrelTip(ushort serial, out BarrelTipExtension foundExtension)
	{
		if (FirearmWorldmodel.Instances.TryGetValue(serial, out var value))
		{
			return value.TryGetExtension<BarrelTipExtension>(out foundExtension);
		}
		foundExtension = null;
		return false;
	}

	public override void SetupWorldmodel(FirearmWorldmodel worldmodel)
	{
		base.SetupWorldmodel(worldmodel);
		this.SetAttachments(worldmodel.AttachmentCode);
	}

	public override void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		base.InitViewmodel(viewmodel);
		viewmodel.OnAttachmentsUpdated += UpdateViewmodel;
		this.UpdateViewmodel();
	}

	private void UpdateViewmodel()
	{
		this.SetAttachments(base.Viewmodel.ParentFirearm.GetCurrentAttachmentsCode());
	}

	private void ValidateCache()
	{
		if (this._cachedFilters != null)
		{
			return;
		}
		this._cachedFilters = new uint[this._attachmentOffsets.Length];
		for (int i = 0; i < this._cachedFilters.Length; i++)
		{
			if (!this._attachmentOffsets[i].Attachment.TryGetFilter(base.Identifier.TypeId, out var filter))
			{
				throw new InvalidOperationException($"Cannot generate filter for barrel tip {i}.");
			}
			this._cachedFilters[i] = filter;
		}
	}

	private void SetAttachments(uint attCode)
	{
		this.ValidateCache();
		this._curTipPosition = this._baselineOffset;
		for (int i = 0; i < this._cachedFilters.Length; i++)
		{
			if ((this._cachedFilters[i] & attCode) != 0)
			{
				this._curTipPosition += this._attachmentOffsets[i].Position;
			}
		}
		this._curTipPosition *= 0.001f;
		int num = this._followers.Length;
		if (num > 0)
		{
			Vector3 worldspacePosition = this.WorldspacePosition;
			for (int j = 0; j < num; j++)
			{
				this._followers[j].position = worldspacePosition;
			}
		}
	}
}

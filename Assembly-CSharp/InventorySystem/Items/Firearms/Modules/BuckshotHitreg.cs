using System;
using System.Collections.Generic;
using InventorySystem.Crosshairs;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class BuckshotHitreg : HitscanHitregModuleBase
{
	private readonly Dictionary<uint, int> _hitCounter = new Dictionary<uint, int>();

	private int _hitmarkerMaxHits;

	private int _hitmarkerMisses;

	[field: SerializeField]
	public BuckshotSettings BasePattern { get; private set; }

	public override Type CrosshairType => typeof(BuckshotCrosshair);

	public float BuckshotScale => this.ActivePattern.OverallScale * base.Firearm.AttachmentsValue(AttachmentParam.SpreadMultiplier);

	public override bool UseHitboxMultipliers => false;

	private BuckshotSettings ActivePattern
	{
		get
		{
			Attachment[] attachments = base.Firearm.Attachments;
			foreach (Attachment attachment in attachments)
			{
				if (attachment.IsEnabled && attachment is BuckshotPatternAttachment buckshotPatternAttachment)
				{
					return buckshotPatternAttachment.Pattern;
				}
			}
			return this.BasePattern;
		}
	}

	protected override float HitmarkerSizeAtDamage(float damage)
	{
		int num = this._hitmarkerMaxHits - this._hitmarkerMisses;
		if (num <= 0)
		{
			return 0f;
		}
		float num2 = damage / (float)num;
		float num3 = this.DamageAtDistance(0f);
		float num4 = num2 / num3;
		float num5 = (float)this._hitmarkerMisses / (float)this._hitmarkerMaxHits;
		float num6 = num5 * num5 * num5;
		float num7 = 1f - num6;
		return num4 * num7;
	}

	protected override void Fire()
	{
		this._hitCounter.Clear();
		Ray ray = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy);
		BuckshotSettings activePattern = this.ActivePattern;
		float num = base.Firearm.AttachmentsValue(AttachmentParam.SpreadPredictability);
		float randomness = 1f - Mathf.Clamp01(1f - activePattern.Randomness) * num;
		float buckshotScale = this.BuckshotScale;
		HitscanResult resultNonAlloc = base.ResultNonAlloc;
		resultNonAlloc.Clear();
		Vector2[] predefinedPellets = activePattern.PredefinedPellets;
		foreach (Vector2 pelletVector in predefinedPellets)
		{
			Vector3 pelletDirection = this.GetPelletDirection(pelletVector, buckshotScale, randomness, ray.direction);
			base.ServerAppendPrescan(new Ray(ray.origin, pelletDirection), resultNonAlloc);
		}
		this._hitmarkerMaxHits += this.ActivePattern.MaxHits;
		this._hitmarkerMisses += resultNonAlloc.Obstacles.Count;
		this.ServerApplyDamage(resultNonAlloc);
	}

	protected override float DamageAtDistance(float dist)
	{
		return base.DamageAtDistance(dist) / (float)this.ActivePattern.MaxHits;
	}

	protected override void ServerApplyDestructibleDamage(DestructibleHitPair target, HitscanResult result)
	{
		uint networkId = target.Destructible.NetworkId;
		int valueOrDefault = this._hitCounter.GetValueOrDefault(networkId);
		if (valueOrDefault < this.ActivePattern.MaxHits)
		{
			this._hitCounter[networkId] = valueOrDefault + 1;
			base.ServerApplyDestructibleDamage(target, result);
		}
	}

	internal override void AlwaysUpdate()
	{
		base.AlwaysUpdate();
		this._hitmarkerMisses = 0;
		this._hitmarkerMaxHits = 0;
	}

	private Vector3 GetPelletDirection(Vector2 pelletVector, float scale, float randomness, Vector3 fwdDirection)
	{
		Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
		Vector2 vector = Vector2.Lerp(pelletVector, insideUnitCircle, randomness) * scale;
		Transform playerCameraReference = base.Firearm.Owner.PlayerCameraReference;
		fwdDirection = Quaternion.AngleAxis(vector.x, playerCameraReference.up) * fwdDirection;
		fwdDirection = Quaternion.AngleAxis(vector.y, playerCameraReference.right) * fwdDirection;
		return fwdDirection;
	}
}

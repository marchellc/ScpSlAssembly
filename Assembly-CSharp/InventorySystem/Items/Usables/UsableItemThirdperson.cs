using InventorySystem.Items.Thirdperson;
using InventorySystem.Items.Thirdperson.LayerProcessors;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Usables;

public class UsableItemThirdperson : IdleThirdpersonItem, ILookatModifier
{
	[SerializeField]
	private AnimationClip _useAnim;

	[SerializeField]
	private HandPoseData _useHandPoseData;

	protected bool IsUsing { get; set; }

	protected float CurWeight { get; set; }

	protected virtual float IncreaseWeightSpeed => 8f;

	protected virtual float DecreaseWeightSpeed => 5f;

	public override void ResetObject()
	{
		base.ResetObject();
		this.IsUsing = false;
	}

	public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
	{
		ThirdpersonLayerWeight weightForLayer = base.GetWeightForLayer(layer);
		return new ThirdpersonLayerWeight(weightForLayer.Weight, !this.IsUsing && weightForLayer.AllowOther);
	}

	public override HandPoseData ProcessHandPose(HandPoseData data)
	{
		return base.ProcessHandPose(data).LerpTo(this._useHandPoseData, this.CurWeight);
	}

	public virtual LookatData ProcessLookat(LookatData data)
	{
		data.GlobalWeight *= Mathf.Clamp01(1f - this.CurWeight);
		return data;
	}

	internal override void Initialize(InventorySubcontroller sctrl, ItemIdentifier id)
	{
		base.Initialize(sctrl, id);
		base.SetAnim(AnimState3p.Override1, this._useAnim);
	}

	protected override void Update()
	{
		base.Update();
		if (!base.Pooled)
		{
			if (this.IsUsing)
			{
				this.CurWeight += Time.deltaTime * this.IncreaseWeightSpeed;
			}
			else
			{
				this.CurWeight -= Time.deltaTime * this.DecreaseWeightSpeed;
			}
			this.CurWeight = Mathf.Clamp01(this.CurWeight);
			base.OverrideBlend = this.CurWeight;
			if (base.TryGetLayerProcessor<HybridLayerProcessor>(out var processor))
			{
				processor.SetDualHandBlend(this.CurWeight);
			}
		}
	}

	protected virtual void OnUsingStatusChanged()
	{
		if (this.IsUsing)
		{
			base.ReplayOverrideBlend(soft: true);
		}
	}

	private void OnClientStatusReceived(StatusMessage msg)
	{
		if (msg.ItemSerial == base.ItemId.SerialNumber)
		{
			bool flag = msg.Status == StatusMessage.StatusType.Start;
			if (flag != this.IsUsing)
			{
				this.IsUsing = flag;
				this.OnUsingStatusChanged();
			}
		}
	}

	protected virtual void Awake()
	{
		UsableItemsController.OnClientStatusReceived += OnClientStatusReceived;
	}

	protected virtual void OnDestroy()
	{
		UsableItemsController.OnClientStatusReceived -= OnClientStatusReceived;
	}
}

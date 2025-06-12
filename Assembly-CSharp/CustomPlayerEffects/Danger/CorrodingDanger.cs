using System;

namespace CustomPlayerEffects.Danger;

public class CorrodingDanger : DangerStackBase
{
	private Corroding _corroding;

	private PocketCorroding _pocketCorroding;

	public override float DangerValue { get; set; } = 1f;

	public override void Initialize(ReferenceHub target)
	{
		base.Initialize(target);
		if (!base.Owner.playerEffectsController.TryGetEffect<Corroding>(out this._corroding))
		{
			throw new NullReferenceException("Corroding wasn't found. This DangerOverride will not function as intended.");
		}
		if (!base.Owner.playerEffectsController.TryGetEffect<PocketCorroding>(out this._pocketCorroding))
		{
			throw new NullReferenceException("PocketCorroding wasn't found. This DangerOverride will not function as intended.");
		}
		this.IsActive = this._corroding.IsEnabled || this._pocketCorroding.IsEnabled;
		StatusEffectBase.OnEnabled += UpdateState;
		StatusEffectBase.OnDisabled += UpdateState;
	}

	public override void Dispose()
	{
		base.Dispose();
		StatusEffectBase.OnEnabled -= UpdateState;
		StatusEffectBase.OnDisabled -= UpdateState;
	}

	private void UpdateState(StatusEffectBase effect)
	{
		if (!(effect != this._corroding) || !(effect != this._pocketCorroding))
		{
			this.IsActive = this._corroding.IsEnabled || this._pocketCorroding.IsEnabled;
		}
	}
}

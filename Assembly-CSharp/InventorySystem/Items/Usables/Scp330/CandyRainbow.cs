using CustomPlayerEffects;
using PlayerStatsSystem;

namespace InventorySystem.Items.Usables.Scp330;

public class CandyRainbow : ICandy
{
	private const int HealthInstant = 15;

	private const int InvigorationDuration = 5;

	private const bool InvigorationDurationStacking = true;

	private const int AhpInstant = 20;

	private const int AhpSustainDuration = 10;

	private const bool AhpSustainDurationStacking = false;

	private const int RainbowDuration = 10;

	private const bool RainbowDurationStacking = false;

	private const bool BodyshotReductionStacking = true;

	private AhpStat.AhpProcess _previousProcess;

	public CandyKindID Kind => CandyKindID.Rainbow;

	public float SpawnChanceWeight => 1f;

	public void ServerApplyEffects(ReferenceHub hub)
	{
		hub.playerStats.GetModule<HealthStat>().ServerHeal(15f);
		hub.playerEffectsController.EnableEffect<Invigorated>(5f, addDuration: true);
		bool num = this._previousProcess != null;
		float num2 = (num ? this._previousProcess.CurrentAmount : 0f);
		float num3 = 0f;
		if (num)
		{
			this._previousProcess.CurrentAmount = 0f;
		}
		this._previousProcess = hub.playerStats.GetModule<AhpStat>().ServerAddProcess(num2 + 20f);
		this._previousProcess.SustainTime = num3 + 10f;
		hub.playerEffectsController.EnableEffect<RainbowTaste>(10f);
		BodyshotReduction effect = hub.playerEffectsController.GetEffect<BodyshotReduction>();
		if (effect.Intensity < byte.MaxValue)
		{
			effect.Intensity++;
		}
	}
}

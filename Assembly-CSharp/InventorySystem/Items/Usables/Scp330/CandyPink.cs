using Utils;

namespace InventorySystem.Items.Usables.Scp330;

public class CandyPink : ICandy
{
	public CandyKindID Kind => CandyKindID.Pink;

	public float SpawnChanceWeight => 0f;

	public void ServerApplyEffects(ReferenceHub hub)
	{
		ExplosionUtils.ServerExplode(hub, ExplosionType.PinkCandy);
	}
}

namespace InventorySystem.Items.Firearms.ShotEvents;

public static class ShotEventManager
{
	public delegate void Shot(ShotEvent shotEvent);

	public static event Shot OnShot;

	public static void Trigger(ShotEvent ev)
	{
		ShotEventManager.OnShot?.Invoke(ev);
	}
}

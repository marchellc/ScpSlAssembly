using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components;

[CreateAssetMenu(fileName = "New Sight Pack", menuName = "ScriptableObject/Firearms/Reflex Sight Pack")]
public class ReflexSightReticlePack : ScriptableObject
{
	public Texture[] Reticles;

	public Texture this[int index] => this.Reticles[index];

	public int Length => this.Reticles.Length;
}

using Mirror;
using UnityEngine;

public class AlphaWarheadOutsitePanel : NetworkBehaviour
{
	public Animator panelButtonCoverAnim;

	private static readonly int Enabled = Animator.StringToHash("enabled");

	public static AlphaWarheadNukesitePanel nukeside => AlphaWarheadNukesitePanel.Singleton;

	private void Update()
	{
		if (!(AlphaWarheadOutsitePanel.nukeside == null))
		{
			base.transform.localPosition = new Vector3(0f, 0f, 9f);
			this.panelButtonCoverAnim.SetBool(AlphaWarheadOutsitePanel.Enabled, AlphaWarheadActivationPanel.IsUnlocked);
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}

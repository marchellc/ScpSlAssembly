using System;
using TMPro;

namespace InventorySystem.Items.Test
{
	public class TestItemViewmodel : ItemViewmodelBase
	{
		public void UpdateText(string stringUwU)
		{
			this.Text.text = stringUwU;
		}

		public TextMeshProUGUI Text;
	}
}

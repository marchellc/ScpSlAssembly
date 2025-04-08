using System;
using Mirror;

namespace Hints
{
	public readonly struct HintMessage : NetworkMessage
	{
		public HintMessage(Hint content)
		{
			this.Content = content;
		}

		public readonly Hint Content;
	}
}

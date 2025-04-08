using System;
using Mirror;

namespace Subtitles
{
	public struct SubtitleMessage : NetworkMessage
	{
		public SubtitleMessage(params SubtitlePart[] subtitleParts)
		{
			this.SubtitleParts = subtitleParts;
		}

		public SubtitlePart[] SubtitleParts;
	}
}

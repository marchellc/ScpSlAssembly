using System;

namespace Subtitles
{
	public struct SubtitlePart
	{
		public SubtitlePart(SubtitleType subtitle, params string[] optionalData)
		{
			this.Subtitle = subtitle;
			this.OptionalData = optionalData;
		}

		public SubtitleType Subtitle;

		public string[] OptionalData;
	}
}

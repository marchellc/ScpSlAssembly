using System;

namespace ProgressiveCulling
{
	public interface IAutoCullerOverrideComponent
	{
		bool AllowAutoCulling { get; }
	}
}

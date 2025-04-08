using System;
using UnityEngine;

namespace AnimatorLayerManagement
{
	public interface IAnimatorLayerSource
	{
		RuntimeAnimatorController AnimLayersSource { get; }
	}
}

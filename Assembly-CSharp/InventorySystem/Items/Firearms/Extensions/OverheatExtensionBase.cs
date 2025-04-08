using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public abstract class OverheatExtensionBase : MixedExtension, IDestroyExtensionReceiver
	{
		public override void SetupWorldmodel(FirearmWorldmodel worldmodel)
		{
			base.SetupWorldmodel(worldmodel);
			if (this._worldmodelSetup)
			{
				return;
			}
			this._worldmodelSetup = true;
			OverheatExtensionBase.WorldmodelInstances.Add(this);
			OverheatExtensionBase.WorldmodelUpdateQueue.Enqueue(this);
		}

		public virtual void OnDestroyExtension()
		{
			if (!this._worldmodelSetup)
			{
				return;
			}
			OverheatExtensionBase.WorldmodelInstances.Remove(this);
		}

		protected abstract void OnTemperatureChanged(float temp);

		protected virtual void Update()
		{
			if (base.ViewmodelMode)
			{
				this.UpdateInstance();
			}
		}

		private void UpdateInstance()
		{
			this.OnTemperatureChanged(TemperatureTrackerModule.GetTemperature(base.Identifier));
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			TemperatureTrackerModule.OnTemperatureSet += OverheatExtensionBase.OnTemperatureSet;
			StaticUnityMethods.OnUpdate += OverheatExtensionBase.UpdateWorldmodels;
		}

		private static void OnTemperatureSet(ItemIdentifier id)
		{
			foreach (OverheatExtensionBase overheatExtensionBase in OverheatExtensionBase.WorldmodelInstances)
			{
				if (!(overheatExtensionBase.Identifier != id))
				{
					overheatExtensionBase.UpdateInstance();
				}
			}
		}

		private static void UpdateWorldmodels()
		{
			OverheatExtensionBase overheatExtensionBase;
			if (!OverheatExtensionBase.WorldmodelUpdateQueue.TryDequeue(out overheatExtensionBase))
			{
				return;
			}
			if (overheatExtensionBase == null)
			{
				return;
			}
			overheatExtensionBase.UpdateInstance();
			OverheatExtensionBase.WorldmodelUpdateQueue.Enqueue(overheatExtensionBase);
		}

		private static readonly Queue<OverheatExtensionBase> WorldmodelUpdateQueue = new Queue<OverheatExtensionBase>();

		private static readonly HashSet<OverheatExtensionBase> WorldmodelInstances = new HashSet<OverheatExtensionBase>();

		private bool _worldmodelSetup;
	}
}

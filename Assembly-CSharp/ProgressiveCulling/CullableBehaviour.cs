using System;
using UnityEngine;

namespace ProgressiveCulling
{
	public abstract class CullableBehaviour : MonoBehaviour, ICullable
	{
		public bool IsCulled
		{
			get
			{
				return this._prevVisible != null && !this._prevVisible.Value;
			}
		}

		public abstract bool ShouldBeVisible { get; }

		private protected bool EditorShowGizmos { protected get; private set; }

		public void SetVisibility(bool isVisible)
		{
			bool? prevVisible = this._prevVisible;
			if (!((prevVisible.GetValueOrDefault() == isVisible) & (prevVisible != null)))
			{
				this._prevVisible = new bool?(isVisible);
				this.OnVisibilityChanged(isVisible);
				return;
			}
			if (isVisible)
			{
				this.UpdateVisible();
				return;
			}
			this.UpdateInvisible();
		}

		protected virtual void UpdateVisible()
		{
		}

		protected virtual void UpdateInvisible()
		{
		}

		protected abstract void OnVisibilityChanged(bool isVisible);

		protected virtual void OnDrawGizmosSelected()
		{
		}

		private bool? _prevVisible;
	}
}

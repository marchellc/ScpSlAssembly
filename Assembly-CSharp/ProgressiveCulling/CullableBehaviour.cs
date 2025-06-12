using UnityEngine;

namespace ProgressiveCulling;

public abstract class CullableBehaviour : MonoBehaviour, ICullable
{
	private bool? _prevVisible;

	public bool IsCulled
	{
		get
		{
			if (this._prevVisible.HasValue)
			{
				return !this._prevVisible.Value;
			}
			return false;
		}
	}

	public abstract bool ShouldBeVisible { get; }

	protected bool EditorShowGizmos { get; private set; }

	public void SetVisibility(bool isVisible)
	{
		if (this._prevVisible == isVisible)
		{
			if (isVisible)
			{
				this.UpdateVisible();
			}
			else
			{
				this.UpdateInvisible();
			}
		}
		else
		{
			this._prevVisible = isVisible;
			this.OnVisibilityChanged(isVisible);
		}
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
}

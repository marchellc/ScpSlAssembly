using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldableButton : Button
{
	private readonly Stopwatch _holdSw = new Stopwatch();

	private bool _eventCalled;

	[SerializeField]
	private Image _loadingCircle;

	[SerializeField]
	private bool _deselectOnComplete;

	public bool IsHeld => base.IsPressed();

	public bool IsHovering
	{
		get
		{
			if (!base.IsHighlighted())
			{
				return this.IsHeld;
			}
			return true;
		}
	}

	public float HeldPercent
	{
		get
		{
			if (this.HoldTime <= 0f || !this.IsHeld)
			{
				return 0f;
			}
			return Mathf.Clamp01((float)this._holdSw.Elapsed.TotalSeconds / this.HoldTime);
		}
	}

	[field: SerializeField]
	public virtual float HoldTime { get; private set; }

	[field: SerializeField]
	public ButtonClickedEvent OnHeld { get; private set; }

	public override void OnPointerDown(PointerEventData eventData)
	{
		base.OnPointerDown(eventData);
		this._holdSw.Restart();
		this._eventCalled = false;
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		base.OnPointerUp(eventData);
		this._holdSw.Reset();
	}

	private void Update()
	{
		if (this._loadingCircle != null)
		{
			this._loadingCircle.fillAmount = this.HeldPercent;
		}
		if (!this._eventCalled && this.IsHeld && !(this.HeldPercent < 1f))
		{
			this._eventCalled = true;
			if (this.OnHeld != null)
			{
				this.OnHeld.Invoke();
			}
		}
	}
}

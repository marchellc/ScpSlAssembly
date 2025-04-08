using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldableButton : Button
{
	public bool IsHeld
	{
		get
		{
			return base.IsPressed();
		}
	}

	public bool IsHovering
	{
		get
		{
			return base.IsHighlighted() || this.IsHeld;
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

	public virtual float HoldTime { get; private set; }

	public Button.ButtonClickedEvent OnHeld { get; private set; }

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
		if (this._eventCalled || !this.IsHeld || this.HeldPercent < 1f)
		{
			return;
		}
		this._eventCalled = true;
		if (this.OnHeld == null)
		{
			return;
		}
		this.OnHeld.Invoke();
	}

	private readonly Stopwatch _holdSw = new Stopwatch();

	private bool _eventCalled;

	[SerializeField]
	private Image _loadingCircle;

	[SerializeField]
	private bool _deselectOnComplete;
}

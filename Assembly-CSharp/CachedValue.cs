using System;

public class CachedValue<T>
{
	private bool _cacheSet;

	private T _cachedValue;

	private readonly Func<T> _factory;

	private readonly Func<bool> _updateChecker;

	private readonly bool _usesChecker;

	public T Value
	{
		get
		{
			if (this.IsDirty || (this._usesChecker && this._updateChecker()))
			{
				this.RefreshValue();
			}
			return this._cachedValue;
		}
	}

	public bool IsDirty => !this._cacheSet;

	public CachedValue(Func<T> factory)
	{
		this._factory = factory;
		this._usesChecker = false;
		this._updateChecker = null;
	}

	public CachedValue(Func<T> factory, Func<bool> checker)
	{
		this._factory = factory;
		this._usesChecker = true;
		this._updateChecker = checker;
	}

	public void RefreshValue()
	{
		this._cacheSet = true;
		this._cachedValue = this._factory();
	}

	public void SetDirty()
	{
		this._cacheSet = false;
	}
}

using UnityEngine;
using UnityEngine.UI;

public class ImageBasedRealisticLoadingBar : MonoBehaviour
{
	private RealisticLoadingBar _bar;

	[SerializeField]
	private float _targetTime;

	[SerializeField]
	private float _stepVar;

	[SerializeField]
	private float _tickVar;

	[SerializeField]
	private int _minNumOfSteps;

	[SerializeField]
	private int _maxNumOfSteps;

	[SerializeField]
	private Image _targetImage;

	[SerializeField]
	private float _smoothing;

	private void OnEnable()
	{
		this._targetImage.fillAmount = 0f;
		this._bar = new RealisticLoadingBar(this._targetTime, Random.Range(this._minNumOfSteps, this._maxNumOfSteps + 1), this._stepVar, this._tickVar);
	}

	private void Update()
	{
		this._targetImage.fillAmount = Mathf.Lerp(this._targetImage.fillAmount, this._bar.Progress, Time.deltaTime * this._smoothing);
	}
}

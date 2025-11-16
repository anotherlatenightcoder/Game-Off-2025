using Route24.Core;
using Route24.GameOff;
using System;
using UnityEngine;
using System.Collections;

public class BigButton : BaseSensor, IInteractable
{
    [Header("Press View")]
    [SerializeField] private Transform _model;
    [SerializeField] private float _pressDistance = 0.05f;
    [SerializeField] private float _pressDuration = 0.08f;
    [SerializeField] private float _returnDuration = 0.12f;

    private Action _onInteract;
    private Func<bool> _canInteract;
    private Func<bool> _canShowMassage;
    private Func<string> _getInteractionText;

    private Vector3 _initialLocalPos;
    private Coroutine _pressRoutine;

    #region Interaction
    public void SetOnInteract(Action onInteract) => _onInteract = onInteract;
    public void SetCanInteractAction (Func<bool> canInteract) => _canInteract = canInteract;
    public void SetCanShowMassage(Func<bool> canShowMassage) => _canShowMassage = canShowMassage;
    public void SetInteractionText(Func<string> getInteractionText) => _getInteractionText = getInteractionText;

    public string GetInteractionText() => _getInteractionText?.Invoke() ?? string.Empty;
    public bool CanInteract() => _canInteract?.Invoke() ?? true;
    public bool CanShowMessage() => _canShowMassage?.Invoke() ?? true;
    public void OnInteract()
    {
        _onInteract?.Invoke();
        ButtonPressedView();
    }
    #endregion

    private void Awake()
    {
        if (_model != null)
            _initialLocalPos = _model.localPosition;
    }


    private void ButtonPressedView()
    {
        if (_pressRoutine != null)
            StopCoroutine(_pressRoutine);

        _pressRoutine = StartCoroutine(PressAnimationRoutine());
    }

    private IEnumerator PressAnimationRoutine()
    {
        Vector3 targetPos = _initialLocalPos + Vector3.forward * _pressDistance;

        // Press
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / _pressDuration;
            _model.localPosition = Vector3.Lerp(_initialLocalPos, targetPos, t);
            yield return null;
        }

        // Optional small hold at the bottom
        yield return new WaitForSeconds(0.05f);

        // Return
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / _returnDuration;
            _model.localPosition = Vector3.Lerp(targetPos, _initialLocalPos, t);
            yield return null;
        }

        _model.localPosition = _initialLocalPos;
    }

}

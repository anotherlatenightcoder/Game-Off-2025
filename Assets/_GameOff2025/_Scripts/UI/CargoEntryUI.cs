using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Route24.GameOff
{
    public class CargoEntryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private Color _defaultColor = Color.gray;
        [SerializeField] private Color _safeColor = Color.green;
        [SerializeField] private Color _bannedColor = Color.red;

        public bool IsScanned { get; private set; }
        public string ActualName => _actualName;
        private string _actualName;

        public void Initialize(string displayName, string actualName)
        {
            _text.text = displayName;
            _actualName = actualName;
            _text.color = _defaultColor;
            IsScanned = false;
        }

        public IEnumerator ScanReveal(float delay, List<string> bannedList)
        {
            yield return new WaitForSeconds(delay);

            bool isBanned = bannedList.Contains(_actualName);
            _text.text = _actualName;
            _text.color = isBanned ? _bannedColor : _safeColor;

            IsScanned = true;
        }
    }
}
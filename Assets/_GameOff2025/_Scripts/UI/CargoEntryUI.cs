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
        public CargoItem Cargo { get; private set; }

        public void Initialize(CargoItem cargo)
        {
            Cargo = cargo;
            _text.text = DistortText(cargo.DisplayName);
            _text.color = _defaultColor;
            IsScanned = false;
        }

        public IEnumerator ScanReveal(float delay, List<CargoItem> bannedList)
        {
            yield return new WaitForSeconds(delay);

            bool isBanned = bannedList.Exists(x => x.Id == Cargo.Id);
            _text.text = Cargo.DisplayName;
            _text.color = isBanned ? _bannedColor : _safeColor;

            IsScanned = true;
        }

        private string DistortText(string original)
        {
            var chars = original.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Random.value < 0.3f) chars[i] = '?';
                else if (Random.value < 0.1f) chars[i] = (char)Random.Range(65, 90);
            }
            return new string(chars);
        }
    }
}
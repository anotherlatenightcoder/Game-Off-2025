using System;
using UnityEngine;

namespace Route24.GameOff
{
    public class BaseSensor : MonoBehaviour
    {
        public event Action OnActivated;

        public event Action OnDeactivated;

        public bool IsActive { get; protected set; }

        protected void FireActivated() => OnActivated?.Invoke();

        protected void FireDeactivated() => OnDeactivated?.Invoke();
    }
}
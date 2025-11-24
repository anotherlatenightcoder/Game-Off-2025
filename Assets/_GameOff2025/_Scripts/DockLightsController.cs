using System.Collections;
using System.Collections.Generic;
using Route24.Core;
using UnityEngine;

namespace Route24.GameOff
{
    public class DockLightsController : MonoBehaviour, ISceneInitializable
    {
        [Header("Light Objects")]
        [SerializeField] private List<Light> _lights;

        [Header("Fade Settings")]
        [SerializeField] private float _fadeInDuration = 5f;

        [Header("Flicker Settings")]
        [SerializeField] private float _startupFlickerDuration = 1.5f;
        [SerializeField, Range(1f, 5f)] private float _flickerIntensityMultiplier = 2f;

        [Header("Faulty Flicker Timing")]
        [SerializeField] private float _flickerDelayMin = 20f;
        [SerializeField] private float _flickerDelayMax = 30f;
        [SerializeField] private float _faultyFlickerDuration = 1.5f;

        private List<float> _originalIntensities = new();
        private EventHub _eventHub;


        // ───────────────────────────────────────────────
        // INITIALIZE
        // ───────────────────────────────────────────────
        public void SceneInitialize()
        {
            _eventHub = ServiceLocator.Get<EventHub>();

            _originalIntensities.Clear();

            foreach (var light in _lights)
            {
                if (!light) continue;

                _originalIntensities.Add(light.intensity);
                light.intensity = 0f; // Turn off at start
            }

            _eventHub?.Subscribe<LightsPoweredOnEvent>(OnLightsPoweredOn);
        }


        // ───────────────────────────────────────────────
        // EVENT: POWER ON
        // ───────────────────────────────────────────────
        private void OnLightsPoweredOn(LightsPoweredOnEvent evt)
        {
            StopAllCoroutines();
            StartCoroutine(FadeInSequence());
        }


        // ───────────────────────────────────────────────
        // FADE-IN + STARTUP FLICKER
        // ───────────────────────────────────────────────
        private IEnumerator FadeInSequence()
        {
            if (_lights.Count == 0) yield break;

            // Pick 2 random lights to flicker during warmup
            int idxA = Random.Range(0, _lights.Count);
            int idxB = Random.Range(0, _lights.Count);
            while (idxB == idxA) idxB = Random.Range(0, _lights.Count);

            // Begin flickers
            StartCoroutine(StartupFlicker(idxA));
            StartCoroutine(StartupFlicker(idxB));

            float elapsed = 0f;

            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _fadeInDuration);

                for (int i = 0; i < _lights.Count; i++)
                {
                    if (!_lights[i]) continue;
                    _lights[i].intensity = Mathf.Lerp(0f, _originalIntensities[i], t);
                }

                yield return null;
            }

            // Lock lights to final intensity
            for (int i = 0; i < _lights.Count; i++)
            {
                if (_lights[i])
                    _lights[i].intensity = _originalIntensities[i];
            }

            // Start long-term faulty flicker loop
            StartCoroutine(LongTermFlickerLoop());
        }


        // ───────────────────────────────────────────────
        // STARTUP FLICKER
        // ───────────────────────────────────────────────
        private IEnumerator StartupFlicker(int index)
        {
            Light light = _lights[index];
            float baseIntensity = _originalIntensities[index];
            float time = 0f;

            while (time < _startupFlickerDuration)
            {
                time += Time.deltaTime;
                float noise = Mathf.PerlinNoise(Time.time * 25f, 0f);

                light.intensity = Mathf.Lerp(baseIntensity, baseIntensity * _flickerIntensityMultiplier, noise);

                yield return null;
            }

            // Restore after flicker
            light.intensity = 0f; // It will be faded in by FadeInSequence()
        }


        // ───────────────────────────────────────────────
        // LONG TERM FAULTY FLICKER LOOP
        // ───────────────────────────────────────────────
        private IEnumerator LongTermFlickerLoop()
        {
            while (true)
            {
                float delay = Random.Range(_flickerDelayMin, _flickerDelayMax);
                yield return new WaitForSeconds(delay);

                int i = Random.Range(0, _lights.Count);
                if (_lights[i])
                    yield return StartCoroutine(FaultyFlicker(i));
            }
        }


        private IEnumerator FaultyFlicker(int index)
        {
            Light light = _lights[index];
            float baseIntensity = _originalIntensities[index];
            float elapsed = 0f;

            while (elapsed < _faultyFlickerDuration)
            {
                elapsed += Time.deltaTime;

                float noise = Mathf.PerlinNoise(Time.time * 30f, 0f);
                light.intensity = Mathf.Lerp(baseIntensity, baseIntensity * _flickerIntensityMultiplier, noise);

                yield return null;
            }

            // Restore normal intensity
            light.intensity = baseIntensity;
        }
    }
}

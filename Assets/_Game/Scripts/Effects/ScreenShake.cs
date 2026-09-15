using System.Collections;
using UnityEngine;
using DeadDawn.Core;

namespace DeadDawn.Effects
{
    public class ScreenShake : MonoBehaviour
    {
        private Vector3 originalLocalPos;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            originalLocalPos = transform.localPosition;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ScreenShakeEvent>(OnScreenShakeRequested);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ScreenShakeEvent>(OnScreenShakeRequested);
        }

        private void OnScreenShakeRequested(ScreenShakeEvent evt)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }
            shakeCoroutine = StartCoroutine(DoShake(evt.Intensity, evt.Duration));
        }

        private IEnumerator DoShake(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float strength = Mathf.Lerp(intensity, 0f, elapsed / duration);
                Vector3 randomOffset = Random.insideUnitSphere * strength;
                randomOffset.z = 0; // Keep depth stable in orthographic

                transform.localPosition = originalLocalPos + randomOffset;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalLocalPos;
            shakeCoroutine = null;
        }
    }
}

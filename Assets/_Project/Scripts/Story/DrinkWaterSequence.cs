using System;
using System.Collections;
using Game.Audio;
using Game.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Story
{
    // Lavabodan su içme: musluk açılır, bardak dolar, oyuncu bardağı ağzına götürür ve içer.
    // Sıra koddadır, süreler Inspector'dadır. Bitince onFinished çalışır.
    public class DrinkWaterSequence : MonoBehaviour
    {
        [Header("Parçalar")]
        [SerializeField] private Transform glass;
        [Tooltip("Bardağın içindeki su: pivotu altta, Y ölçeği doluluğu verir.")]
        [SerializeField] private Transform water;
        [SerializeField] private Transform underFaucet;
        [SerializeField] private Transform handle;
        [SerializeField] private GameObject stream;

        [Header("Ses")]
        [SerializeField] private SoundSet faucetOn;
        [SerializeField] private SoundSet faucetOff;
        [SerializeField] private SoundSet waterFill;
        [SerializeField] private SoundSet glassLift;
        [SerializeField] private SoundSet glassSet;

        [Header("Hareket")]
        [SerializeField] private float handleAngle = 55f;
        [SerializeField] private float fullLevel = 0.1f;
        [SerializeField] private float moveTime = 0.55f;
        [SerializeField] private float fillTime = 1.6f;
        [SerializeField] private float liftTime = 0.7f;
        [SerializeField] private float drinkTime = 1.7f;
        [SerializeField] private Vector3 mouthOffset = new Vector3(0f, -0.09f, 0.2f);
        [SerializeField] private float tiltAngle = 75f;
        [Tooltip("İçerken kamera bu kadar yukarı bakar (derece).")]
        [SerializeField] private float headTilt = 14f;

        [SerializeField] private UnityEvent onFinished;

        public void Play()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            var player = GameObject.FindWithTag("Player").GetComponent<PlayerRig>();
            player.SetScriptedBlock(true);
            Transform view = player.GetComponentInChildren<PlayerLook>().CameraPivot;

            Vector3 restPosition = glass.position;
            Quaternion restRotation = glass.rotation;
            Quaternion handleRest = handle.localRotation;

            Sfx.Play(glassLift, glass.position);
            yield return MoveGlass(underFaucet.position, underFaucet.rotation, moveTime);

            Sfx.Play(faucetOn, handle.position);
            yield return Turn(handle, handleRest, handleRest * Quaternion.Euler(0f, handleAngle, 0f), 0.25f);
            stream.SetActive(true);
            Sfx.Play(waterFill, underFaucet.position);
            yield return Animate(fillTime, t => SetLevel(fullLevel * t));
            stream.SetActive(false);
            Sfx.Play(faucetOff, handle.position);
            yield return Turn(handle, handle.localRotation, handleRest, 0.25f);

            yield return Animate(liftTime, t =>
            {
                float s = Smooth(t);
                glass.SetPositionAndRotation(Vector3.Lerp(underFaucet.position, view.TransformPoint(mouthOffset), s),
                    Quaternion.Slerp(underFaucet.rotation, view.rotation, s));
            });

            Quaternion headRest = view.localRotation;
            Quaternion headUp = headRest * Quaternion.Euler(-headTilt, 0f, 0f);
            yield return Animate(drinkTime, t =>
            {
                float rise = Smooth(Mathf.Clamp01(t * 3f));
                view.localRotation = Quaternion.Slerp(headRest, headUp, rise);
                glass.SetPositionAndRotation(view.TransformPoint(mouthOffset),
                    view.rotation * Quaternion.Euler(-tiltAngle * rise, 0f, 0f));
                SetLevel(fullLevel * (1f - Mathf.Clamp01((t - 0.2f) / 0.8f)));
            });

            yield return Animate(0.35f, t => view.localRotation = Quaternion.Slerp(headUp, headRest, Smooth(t)));
            yield return MoveGlass(restPosition, restRotation, liftTime);
            Sfx.Play(glassSet, restPosition);

            player.SetScriptedBlock(false);
            onFinished.Invoke();
        }

        private void SetLevel(float level)
        {
            water.gameObject.SetActive(level > 0.002f);
            Vector3 scale = water.localScale;
            scale.y = Mathf.Max(level, 0.001f);
            water.localScale = scale;
        }

        private IEnumerator MoveGlass(Vector3 position, Quaternion rotation, float duration)
        {
            Vector3 startPosition = glass.position;
            Quaternion startRotation = glass.rotation;
            yield return Animate(duration, t =>
            {
                // Yay çizerek gider: düz çizgide kaymasın, elle taşınıyor gibi dursun.
                float s = Smooth(t);
                Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.06f);
                glass.SetPositionAndRotation(Vector3.Lerp(startPosition, position, s) + arc,
                    Quaternion.Slerp(startRotation, rotation, s));
            });
        }

        private IEnumerator Turn(Transform target, Quaternion from, Quaternion to, float duration)
        {
            yield return Animate(duration, t => target.localRotation = Quaternion.Slerp(from, to, Smooth(t)));
        }

        private static IEnumerator Animate(float duration, Action<float> step)
        {
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                step(time / duration);
                yield return null;
            }

            step(1f);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}

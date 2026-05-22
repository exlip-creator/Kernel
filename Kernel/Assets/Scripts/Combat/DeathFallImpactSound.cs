using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Один раз проигрывает звук, когда рэгдолл после смерти ударяется о поверхность.
    /// Добавляется из <see cref="Health"/> при падении игрока.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DeathFallImpactSound : MonoBehaviour
    {
        private AudioClip _clip;
        private float _volume = 1f;
        private float _minSpeed = 2.5f;
        private AudioSource _audioSource;
        private bool _played;

        public void Configure(AudioClip clip, float volume, float minSpeed, AudioSource source = null)
        {
            _clip = clip;
            _volume = Mathf.Clamp01(volume);
            _minSpeed = Mathf.Max(0f, minSpeed);
            _audioSource = source;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_played || _clip == null)
                return;

            if (collision.relativeVelocity.magnitude < _minSpeed)
                return;

            _played = true;

            if (_audioSource != null)
                _audioSource.PlayOneShot(_clip, _volume);
            else
                AudioSource.PlayClipAtPoint(_clip, collision.GetContact(0).point, _volume);
        }
    }
}

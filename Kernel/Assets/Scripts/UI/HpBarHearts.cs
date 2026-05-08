using System.Collections.Generic;
using Kernel.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Kernel.UI
{
    public sealed class HpBarHearts : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private Health health;

        [Header("Hearts")]
        [SerializeField] private List<Image> heartImages = new();
        [SerializeField] private Sprite emptyHeartSprite;
        [SerializeField] private float hpPerHeart = 20f;

        private Sprite _fullHeartSprite;

        private void Reset()
        {
            heartImages.Clear();
            heartImages.AddRange(GetComponentsInChildren<Image>(true));
        }

        private void Awake()
        {
            if (health == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    health = player.GetComponentInParent<Health>();
            }

            if (_fullHeartSprite == null)
            {
                Image first = GetFirstHeartImage();
                _fullHeartSprite = first != null ? first.sprite : null;
            }
        }

        private void OnEnable()
        {
            if (health != null)
                health.HpChanged += OnHpChanged;

            if (health != null)
                OnHpChanged(health.CurrentHp, health.MaxHp);
        }

        private void OnDisable()
        {
            if (health != null)
                health.HpChanged -= OnHpChanged;
        }

        private void OnHpChanged(float currentHp, float maxHp)
        {
            if (heartImages == null || heartImages.Count == 0)
                AutoCollectHearts();

            if (heartImages == null || heartImages.Count == 0)
                return;

            if (hpPerHeart <= 0f)
                hpPerHeart = 20f;

            int filledHearts = Mathf.CeilToInt(Mathf.Clamp(currentHp, 0f, maxHp) / hpPerHeart);
            filledHearts = Mathf.Clamp(filledHearts, 0, heartImages.Count);

            Sprite full = _fullHeartSprite != null ? _fullHeartSprite : (heartImages[0] != null ? heartImages[0].sprite : null);
            for (int i = 0; i < heartImages.Count; i++)
            {
                Image img = heartImages[i];
                if (img == null)
                    continue;

                bool isFilled = i < filledHearts;
                img.sprite = isFilled ? full : emptyHeartSprite;
            }
        }

        private void AutoCollectHearts()
        {
            heartImages = new List<Image>();
            foreach (Transform child in transform)
            {
                var img = child.GetComponent<Image>();
                if (img != null)
                    heartImages.Add(img);
            }
        }

        private Image GetFirstHeartImage()
        {
            if (heartImages != null)
            {
                for (int i = 0; i < heartImages.Count; i++)
                {
                    if (heartImages[i] != null)
                        return heartImages[i];
                }
            }

            AutoCollectHearts();
            for (int i = 0; i < heartImages.Count; i++)
            {
                if (heartImages[i] != null)
                    return heartImages[i];
            }

            return null;
        }
    }
}


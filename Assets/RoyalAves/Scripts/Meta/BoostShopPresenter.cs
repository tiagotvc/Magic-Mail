// Apresentação da modal de compra de power-up (fica no mesmo objeto do BoostShop). Quando o BoostShop abre
// um power-up, este componente:
//  - põe o NOME como imagem (sprite PT/EN do BoostShopCatalog) e, se cadastrada, a figura;
//  - faz as faixas de luz girarem atrás da figura;
//  - mostra a quantidade ("x2") como sprite, conforme a tabela "Count Sprites";
//  - bloqueia o botão "Assistir vídeo" por 6h depois de usado (por power-up) e mostra o tempo restante.
// Todos os campos são opcionais: o que não estiver ligado é simplesmente ignorado.
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Boost Shop Presenter")]
    public class BoostShopPresenter : MonoBehaviour
    {
        [Serializable]
        public class CountSprite
        {
            public int count = 2;
            [Tooltip("Sprite escrito \"x2\" (ou o número correspondente).")]
            public Sprite sprite;
        }

        [Header("Nome e figura")]
        [Tooltip("Image da faixa rosa com o nome (ex.: light_ball_EN). Ganha um LocalizedImage sozinho.")]
        [SerializeField] Image nameImage;
        [Tooltip("Image da figura do power-up. Vazio = não troca a figura.")]
        [SerializeField] Image iconImage;

        [Tooltip("Texto de descrição. Vazio = usa o campo Description do BoostShop.")]
        [SerializeField] TMP_Text descriptionText;

        [Header("Faixas girando")]
        [Tooltip("Image das faixas, atrás da figura. Ganha o SpinningRays sozinho.")]
        [SerializeField] Image raysImage;
        [SerializeField] float raysDegreesPerSecond = -25f;

        [Header("Quantidade (x2)")]
        [SerializeField] Image countImage;
        [SerializeField] List<CountSprite> countSprites = new List<CountSprite>();

        [Header("Botão de vídeo (6h por power-up)")]
        [SerializeField] Button watchButton;
        [Tooltip("Opcional: mostra o tempo restante (hh:mm:ss) enquanto bloqueado.")]
        [SerializeField] TMP_Text watchTimerLabel;
        [Tooltip("Opcional: filhos escondidos enquanto bloqueado (ex.: o ícone de vídeo).")]
        [SerializeField] GameObject[] hideWhileLocked;

        BoostType current = BoostType.None;

        /// Verdadeiro se a quantidade está sendo mostrada como sprite (o texto "xN" antigo deve ficar escondido).
        public bool ShowsCountSprite => countImage != null && countImage.enabled && countImage.sprite != null;

        public bool CanWatch(BoostType type) => BoostAdCooldown.IsAvailable(type);

        public void Show(BoostProduct product)
        {
            if (product == null) return;
            current = product.boostType;
            var entry = BoostShopCatalog.Find(current);

            if (nameImage != null && entry != null && (entry.namePt != null || entry.nameEn != null))
            {
                var localized = nameImage.GetComponent<LocalizedImage>() ?? nameImage.gameObject.AddComponent<LocalizedImage>();
                localized.SetSprite(GameLanguage.Portuguese, entry.namePt != null ? entry.namePt : entry.nameEn);
                localized.SetSprite(GameLanguage.English, entry.nameEn != null ? entry.nameEn : entry.namePt);
                nameImage.preserveAspect = true;
            }

            var text = ResolveDescriptionText();
            if (text != null && entry != null)
            {
                var pt = LocalizedImage.Current == GameLanguage.Portuguese;
                var value = pt ? entry.descriptionPt : entry.descriptionEn;
                if (string.IsNullOrEmpty(value))
                {
                    var catalog = BoostShopCatalog.Instance;
                    var template = catalog == null ? null : pt ? catalog.descriptionTemplatePt : catalog.descriptionTemplateEn;
                    var itemName = pt ? entry.displayNamePt : entry.displayNameEn;
                    if (string.IsNullOrEmpty(itemName)) itemName = pt ? entry.displayNameEn : entry.displayNamePt;
                    if (!string.IsNullOrEmpty(template) && !string.IsNullOrEmpty(itemName))
                        value = template.Replace("{nome}", itemName).Replace("{name}", itemName);
                }
                if (!string.IsNullOrEmpty(value))
                {
                    text.text = value;
                    text.color = Color.white;
                }
            }

            if (iconImage != null && entry != null && entry.icon != null)
            {
                iconImage.sprite = entry.icon;
                iconImage.preserveAspect = true;
            }

            if (raysImage != null)
            {
                var spin = raysImage.GetComponent<SpinningRays>() ?? raysImage.gameObject.AddComponent<SpinningRays>();
                spin.DegreesPerSecond = raysDegreesPerSecond;
                raysImage.raycastTarget = false;
            }

            if (countImage != null)
            {
                var found = countSprites.Find(c => c != null && c.count == product.count && c.sprite != null);
                countImage.sprite = found != null ? found.sprite : null;
                countImage.enabled = found != null;
                countImage.preserveAspect = true;
            }

            RefreshWatch();
        }

        // No prefab original os campos do BoostShop estão trocados: "Description" aponta para um "Label (1)" e o
        // texto visível "BoostDescription" é o campo "Boost Name". Por isso, sem campo preenchido aqui, o texto
        // "Comece o nível com..." é procurado pelo nome do objeto.
        TMP_Text ResolveDescriptionText()
        {
            if (descriptionText != null) return descriptionText;
            foreach (var t in GetComponentsInChildren<TMP_Text>(true))
                if (t.name == "BoostDescription") return descriptionText = t;
            return GetComponent<BoostShop>()?.description;
        }

        void Update()
        {
            if (current != BoostType.None && watchButton != null && watchButton.gameObject.activeInHierarchy)
                RefreshWatch();
        }

        public void RefreshWatch()
        {
            if (current == BoostType.None) return;
            var left = BoostAdCooldown.Remaining(current);
            var locked = left > TimeSpan.Zero;
            if (watchButton != null) watchButton.interactable = !locked;
            if (watchTimerLabel != null)
            {
                watchTimerLabel.enabled = locked;
                if (locked) watchTimerLabel.text = $"{(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00}";
            }
            if (hideWhileLocked != null)
                foreach (var go in hideWhileLocked) if (go != null) go.SetActive(!locked);
        }
    }
}

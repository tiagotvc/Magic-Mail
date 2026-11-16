// Game background of Correio Mágico: the lobby room of the current area (with its restored upgrades), blurred, behind
// the board, instead of Sweet Sugar's background picture. LobbyController draws the room when a level starts and hands
// the picture here; this object sits on the level's CanvasBack.
using SweetSugar.Scripts.GUI;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class AreaBackdrop : MonoBehaviour
    {
        const string PictureName = "SalaDesfocada";

        [Tooltip("Escurece um pouco a sala para as peças se destacarem (1 = sem escurecer).")]
        [SerializeField, Range(0.3f, 1f)] float brightness = 0.85f;

        RawImage picture;
        AspectRatioFitter fitter;
        RenderTexture texture;

        // Desligado de propósito: o fundo da fase é o Background original do CanvasBack (a arte montada à mão).
        // Para voltar à sala desfocada, ponha true.
        const bool Enabled = false;

        public static void Show(Transform canvasBack, RenderTexture room)
        {
            if (!Enabled)
            {
                // Sem SalaDesfocada e sem desligar o Background; só libera a textura que o LobbyController renderizou.
                if (room != null) Release(room);
                return;
            }
            var backdrop = canvasBack.GetComponent<AreaBackdrop>();
            if (backdrop == null) backdrop = canvasBack.gameObject.AddComponent<AreaBackdrop>();
            backdrop.Apply(room);
        }

        void Apply(RenderTexture room)
        {
            var sweetSugarBackground = GetComponentInChildren<Background>(true);
            if (picture == null)
            {
                var rect = new GameObject(PictureName, typeof(RectTransform)).GetComponent<RectTransform>();
                rect.gameObject.layer = gameObject.layer;
                rect.SetParent(transform, false);
                // Right above Sweet Sugar's background, behind everything else on this canvas.
                rect.SetSiblingIndex(sweetSugarBackground != null ? sweetSugarBackground.transform.GetSiblingIndex() + 1 : 0);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                picture = rect.gameObject.AddComponent<RawImage>();
                picture.raycastTarget = false;
                fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // covers the screen
            }
            if (texture != null && texture != room) Release(texture);
            texture = room;
            // Com o kit Royal Postman o fundo é a arte do mock, sem escurecer nem desfocar.
            var kit = RoyalPostmanGameplayKit.Load();
            if (kit != null && kit.background != null)
            {
                picture.texture = kit.background.texture;
                picture.color = Color.white;
                fitter.aspectRatio = kit.background.rect.width / kit.background.rect.height;
            }
            else
            {
                picture.texture = room;
                picture.color = new Color(brightness, brightness, brightness, 1);
                fitter.aspectRatio = room.width / (float)room.height;
            }
            if (sweetSugarBackground != null && sweetSugarBackground.GetComponent<Image>() is Image old) old.enabled = false;
        }

        void OnDestroy()
        {
            if (texture != null) Release(texture);
        }

        static void Release(RenderTexture target)
        {
            target.Release();
            Destroy(target);
        }
    }
}

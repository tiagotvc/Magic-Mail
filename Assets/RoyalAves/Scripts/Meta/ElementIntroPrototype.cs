using SweetSugar.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // Crate introduction prototype, shown after the pre-play goals on levels 1-3.
    public sealed class ElementIntroPrototype : MonoBehaviour
    {
        CanvasGroup opacity;
        Texture2D panelTexture;
        Sprite panelSprite, haloSprite;
        readonly System.Collections.Generic.List<Material> textMaterials = new System.Collections.Generic.List<Material>();
        LevelManager manager;
        SweetSugar.Scripts.Level.LevelData data;
        bool paused;

        public static void ShowAfterGoals()
        {
            var manager = LevelManager.THIS;
            if (manager == null || manager.levelData == null || manager.field == null) return;
            int level = manager.levelData.levelNum;
            if (level < 1 || level > 3) return;
            var crate = manager.field.GetComponentInChildren<RoyalAves.Gameplay.ParcelStackLayer>();
            if (crate == null) return;
            var renderer = crate.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null) Show(renderer.sprite);
        }

        public static void Show(Sprite crate)
        {
            if (FindFirstObjectByType<ElementIntroPrototype>() != null) return;
            var root = new GameObject("Apresentacao das caixas - referencia", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup), typeof(GraphicRaycaster), typeof(ElementIntroPrototype));
            var view = root.GetComponent<ElementIntroPrototype>();
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = .5f;
            view.opacity = root.GetComponent<CanvasGroup>();
            view.opacity.interactable = false;
            view.opacity.blocksRaycasts = true;
            view.opacity.alpha = 0;
            var dim = Picture(root.transform, "Escurecimento", null, Vector2.zero, Vector2.zero);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            dim.color = new Color(.025f, .02f, .045f, .75f);
            dim.raycastTarget = true;
            var art = LobbyFeaturesConfig.Instance;
            Canvas.ForceUpdateCanvases();
            var area = (RectTransform)root.transform;
            var content = new GameObject("Apresentação",typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(root.transform,false);
            content.sizeDelta = new Vector2(1080,1520);
            float fit = Mathf.Min(1,area.rect.width*.94f/1080,area.rect.height*.88f/1520);
            content.localScale = Vector3.one*fit;
            view.CreatePanelSprite();

            var goldTitle = Label(content,"Caixas!",140,new Color(1,.98f,.84f),new Vector2(1000,200),new Vector2(0,590),art);
            view.Outline(goldTitle,new Color(1,.75f,.06f),.38f);
            var title = Label(content,"Caixas!",140,new Color(1,.98f,.84f),new Vector2(1000,200),new Vector2(0,590),art);
            view.Outline(title,new Color(.65f,.05f,.13f),.19f);
            var subtitle = Label(content,"Novo elemento desbloqueado!",58,new Color(1,.97f,.84f),new Vector2(1040,110),new Vector2(0,405),art);
            view.Outline(subtitle,new Color(.35f,.055f,.10f),.18f);

            var glowTexture = Resources.Load<Texture2D>("Effects/BagOpeningGlow");
            if(glowTexture != null)
            {
                view.haloSprite = Sprite.Create(glowTexture,new Rect(0,0,glowTexture.width,glowTexture.height),Vector2.one*.5f,100);
                var halo=Picture(content,"Luz atrás da caixa",view.haloSprite,new Vector2(680,680),new Vector2(0,10));
                halo.color=new Color(1,.87f,.55f,.55f);
            }
            var icon=Picture(content,"Caixa em destaque",crate,new Vector2(500,500),new Vector2(0,10));
            icon.preserveAspect=true;
            var shadow=Picture(content,"Sombra da placa",view.panelSprite,new Vector2(940,286),new Vector2(0,-548));
            shadow.type=Image.Type.Sliced;shadow.color=new Color(.23f,.01f,.07f,.8f);
            var border=Picture(content,"Borda cereja",view.panelSprite,new Vector2(940,280),new Vector2(0,-530));
            border.type=Image.Type.Sliced;border.color=new Color(.66f,.035f,.20f);
            var rim=Picture(border.transform,"Filete dourado",view.panelSprite,new Vector2(904,244),Vector2.zero);
            rim.type=Image.Type.Sliced;rim.color=new Color(1,.79f,.40f);
            var paper=Picture(border.transform,"Fundo creme",view.panelSprite,new Vector2(884,224),Vector2.zero);
            paper.type=Image.Type.Sliced;paper.color=new Color(1,.95f,.84f);
            Label(border.transform,"Faça combinações ao lado das <color=#AE193F>CAIXAS</color>\nou use <color=#AE193F>especiais</color> para quebrá-las!",51,
                new Color(.15f,.13f,.34f),new Vector2(820,185),Vector2.zero,art);
            view.manager = LevelManager.THIS;
            view.data = view.manager != null ? view.manager.levelData : null;
            if (view.manager != null && (view.manager.gameStatus == GameState.Playing || view.manager.gameStatus == GameState.Tutorial))
            {
                view.paused = true;
                view.manager.gameStatus = GameState.Pause;
            }
            view.StartCoroutine(view.Fade());
        }

        System.Collections.IEnumerator Fade()
        {
            const float fadeIn = .18f, hold = 5f, fadeOut = .35f;
            for (float t = 0; t < fadeIn; t += Time.unscaledDeltaTime)
            {
                opacity.alpha = t / fadeIn;
                yield return null;
            }
            opacity.alpha = 1;
            yield return new WaitForSecondsRealtime(hold);
            for (float t = 0; t < fadeOut; t += Time.unscaledDeltaTime)
            {
                opacity.alpha = 1 - t / fadeOut;
                yield return null;
            }
            Destroy(gameObject);
        }

        void Update()
        {
            if (manager != null && (manager.levelData != data || manager.gameStatus != GameState.Pause))
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            foreach(var material in textMaterials) if(material != null) Destroy(material);
            if(panelSprite != null) Destroy(panelSprite);
            if(panelTexture != null) Destroy(panelTexture);
            if(haloSprite != null) Destroy(haloSprite);
            if (paused && manager != null && manager.levelData == data && manager.gameStatus == GameState.Pause)
                manager.gameStatus = GameState.Playing;
        }

        void Outline(TextMeshProUGUI label,Color color,float width)
        {
            if(label.fontSharedMaterial == null) return;
            var material=new Material(label.fontSharedMaterial);
            material.SetColor(ShaderUtilities.ID_OutlineColor,color);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth,width);
            material.EnableKeyword("OUTLINE_ON");
            label.fontSharedMaterial=material;
            label.extraPadding=true;
            textMaterials.Add(material);
        }

        void CreatePanelSprite()
        {
            const int size=64;
            const float radius=14;
            panelTexture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            panelTexture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=Mathf.Max(radius-x,0,x-(size-1-radius));
                float dy=Mathf.Max(radius-y,0,y-(size-1-radius));
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)));
            }
            panelTexture.SetPixels(pixels);panelTexture.Apply();
            panelSprite=Sprite.Create(panelTexture,new Rect(0,0,size,size),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
        }

        static Image Picture(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(parent, false);
            var image = node.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return image;
        }

        static TextMeshProUGUI Label(Transform parent, string content, float size, Color color, Vector2 bounds,
            Vector2 position, LobbyFeaturesConfig art)
        {
            var node = new GameObject(content, typeof(RectTransform), typeof(TextMeshProUGUI));
            node.transform.SetParent(parent, false);
            var label = node.GetComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            if (art != null && art.displayFont != null) label.font = art.displayFont;
            label.rectTransform.sizeDelta = bounds;
            label.rectTransform.anchoredPosition = position;
            return label;
        }

    }
}

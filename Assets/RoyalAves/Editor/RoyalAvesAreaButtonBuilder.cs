// Redraws the lobby's area button as in the web prototype: a yellow button as tall as "Nível N", with the area name in
// the postal font, the progress ("0/5") in a blue pill under it and the chest in a blue square on the right. Only the
// button's inside changes; its size and place in the lobby prefab (aligned by hand) are kept.
// Menu: Royal Aves > Arrumar botão da área. Running it again redoes the same layout.
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesAreaButtonBuilder
    {
        const string LobbyPath = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";
        const string Art = "Assets/RoyalAves/Art/";
        // Must match LobbyController.AreaTrackName / AreaFillName / AreaBarInset.
        const string TrackName = "Trilho";
        const string FillName = "Preenchimento";
        const float FillInset = 6;

        [MenuItem("Royal Aves/Arrumar botão da área")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            ReimportGenerated("btn-area", "progress-blue");
            var button = Load("UI/Generated/btn-area");
            var pill = Load("UI/Generated/progress-blue");
            var slot = Load("UI/Generated/btn-blue");
            var chest = Load("UI/Chest/chest");
            var green = Load("UI/Generated/bar-fill");
            var trackSprite = Load("UI/Generated/bar-bg");
            if (button == null || pill == null || slot == null || chest == null || green == null || trackSprite == null)
            {
                EditorUtility.DisplayDialog("Botão da área", "Faltam imagens (UI/Generated/btn-area, progress-blue, btn-blue ou UI/Chest/chest).", "OK");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(LobbyPath);
            try
            {
                var area = root.transform.Find("Content/Actions/AreaButton") as RectTransform;
                var play = root.transform.Find("Content/Actions/PlayButton") as RectTransform;
                var art = area != null ? area.Find("Art") as RectTransform : null;
                if (area == null || art == null)
                {
                    EditorUtility.DisplayDialog("Botão da área", "Não achei Content/Actions/AreaButton/Art no prefab do lobby.", "OK");
                    return;
                }

                // The button face: yellow, the width of the button and the height of "Nível N", centred.
                var height = play != null ? play.rect.height : area.rect.height * 0.67f;
                art.anchorMin = new Vector2(0, 0.5f);
                art.anchorMax = new Vector2(1, 0.5f);
                art.pivot = new Vector2(0.5f, 0.5f);
                art.anchoredPosition = Vector2.zero;
                art.sizeDelta = new Vector2(0, height);
                var face = art.GetComponent<Image>();
                face.sprite = button;
                face.type = Image.Type.Sliced;
                face.preserveAspect = false;
                face.color = Color.white;
                face.raycastTarget = true;

                // Area name: postal text on the upper left, no box behind it.
                if (area.Find("LabelBox") is RectTransform label)
                {
                    label.SetParent(art, false);
                    Place(label, new Vector2(0.07f, 0.5f), new Vector2(0.73f, 0.9f));
                    if (label.GetComponent<Image>() is Image labelBox) labelBox.enabled = false;
                }
                // Progress: blue pill with a gold border under the name.
                if (area.Find("ProgressBox") is RectTransform progress)
                {
                    progress.SetParent(art, false);
                    Place(progress, new Vector2(0.09f, 0.13f), new Vector2(0.71f, 0.46f));
                    if (progress.GetComponent<Image>() is Image progressBox)
                    {
                        progressBox.enabled = true;
                        progressBox.sprite = pill;
                        progressBox.type = Image.Type.Sliced;
                        progressBox.color = Color.white;
                    }
                    // Dark track inside the gold border, so the bar shows even at 0/5.
                    var track = progress.Find(TrackName) as RectTransform;
                    if (track == null) track = NewRect(TrackName, progress);
                    track.SetAsFirstSibling();
                    track.anchorMin = Vector2.zero;
                    track.anchorMax = Vector2.one;
                    track.offsetMin = new Vector2(FillInset, FillInset);
                    track.offsetMax = new Vector2(-FillInset, -FillInset);
                    var trackImage = track.GetComponent<Image>();
                    if (trackImage == null) trackImage = track.gameObject.AddComponent<Image>();
                    trackImage.sprite = trackSprite;
                    trackImage.type = Image.Type.Sliced;
                    trackImage.raycastTarget = false;
                    // Green fill over the track, behind the "0/5"; LobbyController sets its width from the area's
                    // progress.
                    var fill = progress.Find(FillName) as RectTransform;
                    if (fill == null) fill = NewRect(FillName, progress);
                    fill.SetSiblingIndex(1);
                    fill.anchorMin = Vector2.zero;
                    fill.anchorMax = new Vector2(0.4f, 1);
                    fill.pivot = new Vector2(0, 0.5f);
                    fill.offsetMin = new Vector2(FillInset, FillInset);
                    fill.offsetMax = new Vector2(-FillInset, -FillInset);
                    var fillImage = fill.GetComponent<Image>();
                    if (fillImage == null) fillImage = fill.gameObject.AddComponent<Image>();
                    fillImage.sprite = green;
                    fillImage.type = Image.Type.Sliced;
                    fillImage.raycastTarget = false;
                }
                // Chest in a blue square on the right.
                var box = art.Find("Bau") as RectTransform ?? NewRect("Bau", art);
                Place(box, new Vector2(0.75f, 0.15f), new Vector2(0.95f, 0.85f));
                var boxImage = box.GetComponent<Image>();
                if (boxImage == null) boxImage = box.gameObject.AddComponent<Image>();
                boxImage.sprite = slot;
                boxImage.type = Image.Type.Sliced;
                boxImage.preserveAspect = false;
                boxImage.raycastTarget = false;
                var picture = box.Find("Imagem") as RectTransform ?? NewRect("Imagem", box);
                Place(picture, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f));
                var chestImage = picture.GetComponent<Image>();
                if (chestImage == null) chestImage = picture.gameObject.AddComponent<Image>();
                chestImage.sprite = chest;
                chestImage.preserveAspect = true;
                chestImage.raycastTarget = false;
                box.SetAsLastSibling();

                PrefabUtility.SaveAsPrefabAsset(root, LobbyPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            EditorUtility.DisplayDialog("Botão da área",
                "Botão da área refeito como no protótipo: amarelo, nome da área, progresso na pílula azul e o baú à direita.\n" +
                "O tamanho e a posição do botão no lobby foram mantidos.", "OK");
        }

        static void Place(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer }.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        // The importer gives these their stretch borders; pictures imported before it knew them need a new import.
        static void ReimportGenerated(params string[] names)
        {
            foreach (var name in names)
            {
                var path = Art + "UI/Generated/" + name + ".png";
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.spriteBorder == Vector4.zero)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + path + ".png");
    }
}

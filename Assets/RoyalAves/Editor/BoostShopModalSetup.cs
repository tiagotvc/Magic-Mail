// Liga a modal de compra de power-up (BoostShop) aos componentes novos, sem mexer no seu layout:
//  Royal Aves > Modal de compra de power-up > Configurar BoostShop selecionado
//    - adiciona BoostShopPresenter no objeto BoostShop e acha, pelos nomes, a imagem do nome (filho de
//      "Boosters_names"), o botão de vídeo, e cria (se faltar) a imagem "Rays" girando atrás da figura;
//    - põe ButtonPopPress (encolhe 15%) nos botões de comprar, assistir vídeo e fechar.
//  Royal Aves > Modal de compra de power-up > Criar ou selecionar catálogo
//    - cria Resources/BoostShop/BoostShopCatalog com uma linha por power-up (nomes PT/EN e figura).
// Rode com o objeto BoostShop selecionado na Hierarchy; depois salve a cena (Ctrl+S).
using System;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class BoostShopModalSetup
    {
        const string CatalogPath = "Assets/Royal Postman Art/Resources/BoostShop/BoostShopCatalog.asset";
        const string RaysPath = "Assets/RoyalAves/Art/UI/Generated/rays.png";

        [MenuItem("Royal Aves/Modal de compra de power-up/Criar ou selecionar catálogo")]
        static void CreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BoostShopCatalog>(CatalogPath);
            if (catalog == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CatalogPath));
                catalog = ScriptableObject.CreateInstance<BoostShopCatalog>();
                foreach (BoostType type in Enum.GetValues(typeof(BoostType)))
                    if (type != BoostType.None) catalog.entries.Add(new BoostShopCatalog.Entry { type = type });
                AssetDatabase.CreateAsset(catalog, CatalogPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }

        [MenuItem("Royal Aves/Modal de compra de power-up/Configurar BoostShop selecionado")]
        static void Configure()
        {
            var shop = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<BoostShop>(true) : null;
            if (shop == null) shop = UnityEngine.Object.FindFirstObjectByType<BoostShop>(FindObjectsInactive.Include);
            if (shop == null) { Debug.LogError("BoostShop não encontrado. Selecione o objeto BoostShop na Hierarchy."); return; }

            var root = shop.transform;
            var presenter = shop.GetComponent<BoostShopPresenter>() ?? Undo.AddComponent<BoostShopPresenter>(shop.gameObject);
            var so = new SerializedObject(presenter);

            // Nome: imagem dentro de "Boosters_names".
            var names = FindDeep(root, "Boosters_names");
            var nameImage = names != null ? names.GetComponentInChildren<Image>(true) : null;
            SetRef(so, "nameImage", nameImage);

            // Botões.
            var buy = new SerializedObject(shop).FindProperty("buyBoostButton").objectReferenceValue as Button;
            var watch = new SerializedObject(shop).FindProperty("watchAdButton").objectReferenceValue as Button;
            if (watch == null)
            {
                var found = FindDeep(root, "Watch") ?? FindDeep(root, "WatchVideo") ?? FindDeep(root, "WatchAd");
                watch = found != null ? found.GetComponent<Button>() : null;
            }
            SetRef(so, "watchButton", watch);

            // Faixas girando, logo atrás da figura (irmão anterior a "Boosters img").
            var rays = FindDeep(root, "Rays");
            var iconHolder = FindDeep(root, "Boosters img");
            if (rays == null && iconHolder != null)
            {
                var go = new GameObject("Rays", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Criar Rays");
                var rt = (RectTransform)go.transform;
                rt.SetParent(iconHolder.parent, false);
                rt.SetSiblingIndex(iconHolder.GetSiblingIndex());
                var holder = (RectTransform)iconHolder;
                rt.anchorMin = rt.anchorMax = holder.anchorMin == holder.anchorMax ? holder.anchorMin : new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = holder.anchoredPosition;
                var side = Mathf.Max(holder.rect.width, holder.rect.height) * 1.6f;
                rt.sizeDelta = new Vector2(side > 1 ? side : 600f, side > 1 ? side : 600f);
                var image = go.GetComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RaysPath);
                image.raycastTarget = false;
                rays = rt;
            }
            SetRef(so, "raysImage", rays != null ? rays.GetComponent<Image>() : null);
            so.ApplyModifiedProperties();

            // Efeito de clique (15%) em comprar, vídeo e fechar.
            AddPop(buy != null ? buy.transform : FindDeep(root, "BuyBoost"));
            AddPop(watch != null ? watch.transform : null);
            AddPop(FindDeep(root, "Close"));

            EditorUtility.SetDirty(shop);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
            Debug.Log("BoostShop configurado. Falta: arrastar a imagem da quantidade (x2) em 'Count Image', preencher 'Count Sprites' e salvar a cena.", presenter);
        }

        static void AddPop(Transform t)
        {
            if (t == null || t.GetComponent<ButtonPopPress>() != null) return;
            Undo.AddComponent<ButtonPopPress>(t.gameObject);
        }

        static void SetRef(SerializedObject so, string field, UnityEngine.Object value)
        {
            var p = so.FindProperty(field);
            if (p != null && value != null && p.objectReferenceValue == null) p.objectReferenceValue = value;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var deeper = FindDeep(child, name);
                if (deeper != null) return deeper;
            }
            return null;
        }
    }
}

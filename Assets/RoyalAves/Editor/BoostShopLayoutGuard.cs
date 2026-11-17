// Guarda do layout da modal BoostShop no editor. Sintoma que ele resolve: depois de uma compilação, a moldura da
// modal (instância do prefab CanvasGlobal na cena) voltava para o tamanho/posição do prefab, perdendo o ajuste
// feito na cena. Antes de cada recarga de scripts ele anota posição/tamanho/escala de todos os RectTransform
// dentro do BoostShop; depois da recarga compara, RESTAURA o que mudou sozinho e escreve no Console o que foi.
// Só age no modo de edição, nunca em Play. Para desligar: menu Royal Aves > Modal de compra de power-up.
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    [InitializeOnLoad]
    public static class BoostShopLayoutGuard
    {
        const string StateKey = "RoyalAves.BoostShopLayoutGuard.snapshot";
        const string EnabledKey = "RoyalAves.BoostShopLayoutGuard.enabled";
        const string MenuPath = "Royal Aves/Modal de compra de power-up/Proteger layout ao compilar";

        [Serializable]
        class Item
        {
            public string path;      // índices dos irmãos a partir do BoostShop, ex.: "0/3/1"
            public string name;
            public Vector2 anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot;
            public Vector3 localScale;
        }

        [Serializable]
        class Snapshot { public List<Item> items = new List<Item>(); }

        static bool Enabled => EditorPrefs.GetBool(EnabledKey, true);

        static BoostShopLayoutGuard()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Save;
            // O prefab é reimportado logo depois da recarga; espera alguns quadros do editor antes de comparar.
            var frames = 0;
            EditorApplication.CallbackFunction check = null;
            check = () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (++frames < 5) return;
                EditorApplication.update -= check;
                Restore();
            };
            EditorApplication.update += check;
        }

        [MenuItem(MenuPath)]
        static void Toggle() => EditorPrefs.SetBool(EnabledKey, !Enabled);

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        static BoostShop FindShop() =>
            UnityEngine.Object.FindFirstObjectByType<BoostShop>(FindObjectsInactive.Include);

        static void Collect(Transform t, string path, List<(string path, RectTransform rect)> into)
        {
            if (t is RectTransform rect) into.Add((path, rect));
            for (var i = 0; i < t.childCount; i++) Collect(t.GetChild(i), path + "/" + i, into);
        }

        static void Save()
        {
            if (!Enabled || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var shop = FindShop();
            if (shop == null) { SessionState.EraseString(StateKey); return; }
            var all = new List<(string path, RectTransform rect)>();
            Collect(shop.transform, "", all);
            var snapshot = new Snapshot();
            foreach (var (path, r) in all)
                snapshot.items.Add(new Item
                {
                    path = path, name = r.name,
                    anchoredPosition = r.anchoredPosition, sizeDelta = r.sizeDelta,
                    anchorMin = r.anchorMin, anchorMax = r.anchorMax, pivot = r.pivot, localScale = r.localScale,
                });
            SessionState.SetString(StateKey, JsonUtility.ToJson(snapshot));
        }

        static void Restore()
        {
            var json = SessionState.GetString(StateKey, "");
            SessionState.EraseString(StateKey);
            if (!Enabled || string.IsNullOrEmpty(json) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var shop = FindShop();
            if (shop == null) return;

            var all = new List<(string path, RectTransform rect)>();
            Collect(shop.transform, "", all);
            var current = new Dictionary<string, RectTransform>();
            foreach (var (path, r) in all) current[path] = r;

            var report = new List<string>();
            foreach (var item in JsonUtility.FromJson<Snapshot>(json).items)
            {
                // Mesma posição na hierarquia E mesmo nome; se a hierarquia mudou, não mexe.
                if (!current.TryGetValue(item.path, out var r) || r.name != item.name) continue;
                if (Same(r.anchoredPosition, item.anchoredPosition) && Same(r.sizeDelta, item.sizeDelta) &&
                    Same(r.anchorMin, item.anchorMin) && Same(r.anchorMax, item.anchorMax) &&
                    Same(r.pivot, item.pivot) && Same(r.localScale, item.localScale)) continue;

                report.Add($"{Path(r, shop.transform)}: tamanho {r.sizeDelta} -> {item.sizeDelta}, posição {r.anchoredPosition} -> {item.anchoredPosition}, escala {r.localScale} -> {item.localScale}");
                Undo.RecordObject(r, "Restaurar layout do BoostShop");
                r.anchorMin = item.anchorMin; r.anchorMax = item.anchorMax; r.pivot = item.pivot;
                r.sizeDelta = item.sizeDelta; r.anchoredPosition = item.anchoredPosition; r.localScale = item.localScale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                EditorUtility.SetDirty(r);
            }

            if (report.Count == 0) return;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
            Debug.LogWarning("Royal Aves: o layout da modal BoostShop mudou sozinho durante a compilação e foi RESTAURADO (" +
                             report.Count + " objeto(s)). Valores (como ficou -> restaurado):\n" + string.Join("\n", report), shop);
        }

        static bool Same(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 0.0001f;
        static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 0.0001f;

        static string Path(Transform t, Transform root)
        {
            var path = t.name;
            while (t != root && t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }
    }
}

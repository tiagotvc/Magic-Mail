using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class RouteCentralLaboratoryTools : EditorWindow
    {
        const string Menu = "Royal Aves/Elementos/Poções/";
        const string PreviewKey = "MagicMail.RouteCentralPreview";
        const string Request = "Logs/route-central-lab.request";
        public const string MetaballsKey = "MagicMail.RouteCentralLab.Metaballs";
        public const string DrawnKey = "MagicMail.RouteCentralLab.DrawnLiquid";
        static double nextPoll;

        [MenuItem(Menu + "Laboratório no jogo")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request, "preview");
        }

        [MenuItem(Menu + "Laboratório estouro e respingos no jogo")]
        public static void PreviewSplash() => Preview();

        [MenuItem(Menu + "Laboratório metaballs no jogo")]
        public static void PreviewMetaballs()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request, "metaballs");
        }

        [MenuItem(Menu + "Laboratório poção desenhada no jogo")]
        public static void PreviewDrawn()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request, "drawn");
        }

        [MenuItem(Menu + "Preparar laboratório no jogo")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pare o Play antes de preparar.");
            RouteCentralPrototypeTools.Build();
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/Elements/RouteCentralVisual.prefab");
            var art = visual.GetComponent<RoyalAves.Effects.RouteCentralPrototype>().artwork;
            var sourceCell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SweetSugar/Resources/Blocks/SolidBlock.prefab").GetComponent<LayeredBlock>().layers[0];
            var root = Instantiate(sourceCell.gameObject);
            try
            {
                root.name = "RouteCentralCell";
                foreach (var parcel in root.GetComponents<ParcelStackLayer>()) DestroyImmediate(parcel);
                var square = root.GetComponent<Square>();
                square.type = SquareTypes.SolidBlock;
                square.canMoveIn = square.canMoveOut = false;
                square.undestroyable = square.cannotBeDestroy = false;
                square.sizeInSquares = Vector2Int.one;
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = art;
                renderer.enabled = false;
                var marker = root.AddComponent<RouteCentralCell>();
                var layers = visual.GetComponent<RoyalAves.Effects.RouteCentralPrototype>();
                marker.artwork = art;
                marker.standArtwork = layers.standArtwork;
                marker.flaskArtwork = layers.flaskArtwork;
                marker.breakSheet = layers.breakSheet;
                PrefabUtility.SaveAsPrefabAsset(root, RouteCentralLaboratory.CellPath);
            }
            finally { DestroyImmediate(root); }

            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            if (!config.targets.Any(t => t.name == "RoutePotions"))
            {
                var target = JsonUtility.FromJson<TargetContainer>(JsonUtility.ToJson(config.targets.First(t => t.name == "FlyingLetters")));
                target.name = "RoutePotions";
                target.setCount = SetCount.Manually;
                target.localization.description.text = "Quebre as quatro poções";
                target.localization.failed.text = "Faltaram poções";
                target.prefabs = new System.Collections.Generic.List<GameObject> { visual };
                foreach (var group in target.defaultSprites)
                    foreach (var sprite in group.sprites) sprite.icon = art;
                config.targets.Add(target);
                EditorUtility.SetDirty(config);
            }
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(FlyingLetterLaboratory.LevelPath);
            var lab = AssetDatabase.LoadAssetAtPath<LevelContainer>(RouteCentralLaboratory.LevelPath);
            if (lab == null) { lab = CreateInstance<LevelContainer>(); AssetDatabase.CreateAsset(lab, RouteCentralLaboratory.LevelPath); }
            lab.levelData = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(source.levelData));
            var data = lab.levelData;
            data.limit = 35;
            data.levelNum = 61;
            data.colorLimit = 4;
            data.pieceColors = new System.Collections.Generic.List<int> { 0, 1, 2, 6 };
            var field = data.fields[0];
            field.layers = 2;
            var normal = Resources.Load<GameObject>("Items/Item");
            for (int row = 0; row < field.maxRows; row++)
                for (int col = 0; col < field.maxCols; col++)
                {
                    var cell = field.levelSquares[row * field.maxCols + col];
                    cell.blocks.Clear();
                    cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.EmptySquare });
                    cell.block = SquareTypes.EmptySquare;
                    cell.blockLayer = 1;
                    cell.obstacle = SquareTypes.NONE;
                    cell.startWithoutItem = false;
                    cell.item = new ItemForEditor { Item = normal, ItemType = ItemsTypes.NONE, Color = new[] { 0, 1, 2, 6 }[(row + 2 * col) % 4] };
                    if (row >= 2 && row <= 3 && col >= 3 && col <= 4)
                    {
                        cell.block = SquareTypes.SolidBlock;
                        cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.SolidBlock });
                        cell.item = new ItemForEditor();
                    }
                }
            void Set(int col, int row, int color) => field.levelSquares[row * field.maxCols + col].item.Color = color;
            Set(2, 1, 0); Set(2, 2, 1); Set(2, 3, 0); Set(1, 2, 0);
            var rocket = field.levelSquares[5 * field.maxCols + 3];
            rocket.item = new ItemForEditor { Item = Resources.Load<GameObject>("Items/VERTICAL_STRIPED"), ItemType = ItemsTypes.VERTICAL_STRIPED, Color = 1 };
            RouteCentralLaboratory.ApplyPalette(data);
            data.LoadTargetObject();
            int index = config.targets.FindIndex(t => t.name == "RoutePotions");
            data.SetTarget(index);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(RouteCentralLaboratory.TargetPath);
            if (targets == null) { targets = CreateInstance<TargetLevel>(); AssetDatabase.CreateAsset(targets, RouteCentralLaboratory.TargetPath); }
            targets.targets = new TargetList();
            targets.targets.Add(new TargetObject {
                targetType = new TargetType { type = index },
                sprites = new SpriteList { new SpriteObject { icon = art, uiSprite = true } },
                CountDrawer = new Count { count = 4 }, ShowTheScoreForStar = new BoolScoreShow()
            });
            EditorUtility.SetDirty(lab);
            EditorUtility.SetDirty(targets);
            AssetDatabase.SaveAssets();
        }

        [InitializeOnLoadMethod]
        static void Initialize() { EditorApplication.update -= Poll; EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + .5;
            if (SessionState.GetBool(PreviewKey, false) && EditorApplication.isPlaying &&
                LevelManager.THIS != null && AI.THIS != null && LevelManager.THIS.gameStatus == GameState.Map)
            {
                SessionState.SetBool(PreviewKey, false);
                LevelManager.THIS.BoostPackage = LevelManager.THIS.BoostColorfullBomb = LevelManager.THIS.BoostStriped = 0;
                LevelManager.THIS.gameStatus = GameState.PrepareGame;
            }
            if (!File.Exists(Request)) return;
            if (File.ReadAllText(Request).Trim() == "verify-stand")
            {
                var visual = UnityEngine.Object.FindObjectsByType<RoyalAves.Effects.RouteCentralPrototype>(FindObjectsSortMode.None)
                    .FirstOrDefault(v => v.name == "RouteCentralNativeStandCheck");
                if (!EditorApplication.isPlaying) return;
                if (visual == null)
                {
                    var fixture = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/Elements/RouteCentralVisual.prefab"));
                    fixture.name = "RouteCentralNativeStandCheck";
                    fixture.transform.position = Vector3.right * 10000;
                    return;
                }
                visual.BreakFlask(0);
                visual.BreakFlask(2);
                var stand = visual.transform.Find("IntactPotionStand")?.GetComponent<SpriteRenderer>();
                bool valid = stand != null && stand.enabled && stand.sprite.texture == visual.standArtwork.texture &&
                    visual.flaskArtwork != null && stand.sprite.texture != visual.flaskArtwork.texture;
                if (stand != null)
                    valid &= Mathf.Abs(stand.sprite.bounds.size.x - RoyalAves.Effects.RouteCentralPrototype.CanvasSize) < .001f;
                for (int i = 0; i < 4; i++)
                {
                    var potion = visual.transform.Find("Potion" + i)?.GetComponent<SpriteRenderer>();
                    valid &= potion != null && Mathf.Abs(potion.sprite.bounds.size.x - 2f) < .001f &&
                        Mathf.Abs(potion.transform.localPosition.x - (i % 2 == 0 ? -1f : 1f)) < .001f &&
                        Mathf.Abs(potion.transform.localPosition.y - (i < 2 ? 1f : -1f)) < .001f;
                }
                File.WriteAllText("Logs/route-central-stand.result", (valid ? "PASS" : "FAIL") +
                    ": native layer alignment and two-flask break; normalized layout and intact stand=" + valid + "; assembly=" + typeof(RouteCentralCell).Module.ModuleVersionId);
                File.Delete(Request);
                if (visual.gameObject.name == "RouteCentralNativeStandCheck") Destroy(visual.gameObject);
                return;
            }
            if (File.ReadAllText(Request).Trim() == "status")
            {
                var manager = LevelManager.THIS;
                File.WriteAllText("Logs/route-central-lab.status", "playing=" + EditorApplication.isPlaying +
                    "; active=" + RouteCentralLaboratory.IsActive + "; state=" + manager?.gameStatus +
                    "; markers=" + UnityEngine.Object.FindObjectsByType<RouteCentralCell>(FindObjectsSortMode.None).Length +
                    "; visuals=" + UnityEngine.Object.FindObjectsByType<RoyalAves.Effects.RouteCentralPrototype>(FindObjectsSortMode.None).Length +
                    "; goal=" + manager?.levelData?.TargetCounters?.Where(t => t.targetLevel.name == "RoutePotions").Sum(t => t.count));
                if (EditorApplication.isPlaying) ScreenCapture.CaptureScreenshot("Logs/route-central-game.png");
                File.Delete(Request);
                return;
            }
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool metaballs = File.ReadAllText(Request).Trim() == "metaballs";
            bool drawn = File.ReadAllText(Request).Trim() == "drawn";
            File.Delete(Request);
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Prepare();
                SessionState.SetBool(MetaballsKey, metaballs);
                SessionState.SetBool(DrawnKey, drawn);
                foreach (var key in new[] { FlyingLetterLaboratory.EnabledKey, FrankingMachineLaboratory.EnabledKey,
                    MailCubbyLaboratory.EnabledKey, InkLaboratory.EnabledKey, ParcelStackLaboratory.EnabledKey }) SessionState.SetBool(key, false);
                SessionState.SetBool(RouteCentralLaboratory.EnabledKey, true);
                SessionState.SetBool(PreviewKey, true);
                EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/main.unity");
                GetWindow<RouteCentralLaboratoryTools>("Poções");
                File.WriteAllText("Logs/route-central-lab.result", "OK: laboratório real preparado; central 2x2, quatro poções, 35 jogadas; metaballs=" + metaballs);
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { File.WriteAllText("Logs/route-central-lab.result", error.ToString()); Debug.LogException(error); }
        }

        [MenuItem(Menu + "Desativar laboratório no jogo")]
        public static void Disable()
        {
            SessionState.SetBool(RouteCentralLaboratory.EnabledKey, false);
            SessionState.SetBool(PreviewKey, false);
            SessionState.SetBool(MetaballsKey, false);
            SessionState.SetBool(DrawnKey, false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        void OnGUI()
        {
            GUILayout.Label("Poções — laboratório no jogo", EditorStyles.boldLabel);
            GUILayout.Label("35 jogadas • meta: quatro poções\nMatch adjacente da cor da poção; especiais quebram uma restante.");
            GUILayout.Label("Primeiro teste: troque (1,2) com (2,2).\nCoordenadas a partir de zero: forma uma trinca roxa à esquerda.");
            if (GUILayout.Button("Abrir / reiniciar laboratório")) Preview();
            if (GUILayout.Button("Testar estouro e respingos (referência)")) PreviewSplash();
            if (GUILayout.Button("Abrir / reiniciar laboratório metaballs")) PreviewMetaballs();
            if (GUILayout.Button("Abrir / reiniciar laboratório poção desenhada")) PreviewDrawn();
            if (GUILayout.Button("Desativar laboratório")) Disable();
        }
    }
}

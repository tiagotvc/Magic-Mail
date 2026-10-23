using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System.Pool;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class MailbagLaboratoryTools : EditorWindow
    {
        const string Menu = "Royal Aves/Elementos/Malote/";
        const string Request = "Logs/mailbag-lab.request";
        const string PreviewKey = "MagicMail.MailbagLab.Start";
        const string PrefabPath = "Assets/SweetSugar/Resources/Items/MAILBAG.prefab";
        const string ArtPath = "Assets/RoyalAves/Art/Obstacles/Mailbag/CM_malote_conceito_v1.png";

        [InitializeOnLoadMethod]
        static void Initialize() { EditorApplication.update -= Poll; EditorApplication.update += Poll; }

        [MenuItem(Menu + "Laboratório no jogo")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request,"preview");
        }

        [MenuItem(Menu + "Desativar laboratório")]
        public static void Disable()
        {
            SessionState.SetBool(MailbagLaboratory.EnabledKey,false);
            SessionState.SetBool(PreviewKey,false);
            SessionState.SetBool("MagicMail.BagOpeningLab.Start",false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        [MenuItem(Menu + "Preparar laboratório")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pare o Play antes de preparar.");
            AssetDatabase.ImportAsset(ArtPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath);
            Sprite MakeState(int state, string name)
            {
                string path = "Assets/RoyalAves/Laboratory/" + name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (existing != null) return existing;
                float w = texture.width / 2f;
                var sprite = Sprite.Create(texture,new Rect(state*w,0,w,texture.height),Vector2.one*.5f,w/1.9f);
                sprite.name = name;
                AssetDatabase.CreateAsset(sprite,path);
                return sprite;
            }
            var intact = MakeState(0,"MailbagIntact");
            var damaged = MakeState(1,"MailbagDamaged");
            var eggPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SweetSugar/Resources/Items/FLYING_LETTER.prefab");
            var root = Instantiate(eggPrefab);
            GameObject prefab;
            try
            {
                root.SetActive(false); root.name = "MAILBAG";
                var egg = root.GetComponent<ItemFlyingLetter>();
                string state = JsonUtility.ToJson(egg);
                DestroyImmediate(egg);
                var bag = root.AddComponent<ItemMailbag>();
                JsonUtility.FromJsonOverwrite(state,bag);
                bag.currentType = bag.debugType = ItemsTypes.MAILBAG;
                bag.Combinable = bag.CombinableWithBonus = false;
                bag.DestroyByNeighbour = true;
                bag.intactSprite = intact; bag.damagedSprite = damaged;
                foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true)) r.sprite = intact;
                var icon = root.GetComponent<EditorIcon>();
                if (icon != null) icon.itemType = ItemsTypes.MAILBAG;
                root.SetActive(true);
                prefab = PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { DestroyImmediate(root); }
            var pool = Resources.Load<PoolerScriptable>("Scriptable/PoolSettings");
            if (!pool.itemsToPool.Any(p => p.objectToPool == prefab))
                pool.itemsToPool.Add(new ObjectPoolItem {objectToPool=prefab,poolName="MAILBAG",amountToPool=6,shouldExpand=true,inEditor=true});
            EditorUtility.SetDirty(pool);
            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var goal = config.targets.FirstOrDefault(t => t.name == "Mailbags");
            if (goal == null)
            {
                goal = JsonUtility.FromJson<TargetContainer>(JsonUtility.ToJson(config.targets.First(t => t.name == "FlyingLetters")));
                goal.name = "Mailbags"; config.targets.Add(goal);
            }
            goal.localization.description.text = "Colete os malotes";
            goal.localization.failed.text = "Faltaram malotes";
            goal.prefabs = new System.Collections.Generic.List<GameObject> {prefab};
            foreach (var group in goal.defaultSprites) foreach (var sprite in group.sprites) sprite.icon = intact;
            EditorUtility.SetDirty(config);
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(FlyingLetterLaboratory.LevelPath);
            var lab = AssetDatabase.LoadAssetAtPath<LevelContainer>(MailbagLaboratory.LevelPath);
            if (lab == null) {lab=CreateInstance<LevelContainer>();AssetDatabase.CreateAsset(lab,MailbagLaboratory.LevelPath);}
            lab.levelData = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(source.levelData));
            lab.levelData.limit = 30; lab.levelData.levelNum = 81;
            foreach (var field in lab.levelData.fields)
                foreach (var cell in field.levelSquares)
                    if (cell.item != null && cell.item.ItemType == ItemsTypes.FLYING_LETTER)
                    {cell.item.ItemType=ItemsTypes.MAILBAG;cell.item.Item=prefab;}
            int index = config.targets.FindIndex(t => t.name == "Mailbags");
            lab.levelData.LoadTargetObject();lab.levelData.SetTarget(index);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(MailbagLaboratory.TargetPath);
            if (targets == null) {targets=CreateInstance<TargetLevel>();AssetDatabase.CreateAsset(targets,MailbagLaboratory.TargetPath);}
            targets.targets = new TargetList();
            targets.targets.Add(new TargetObject { targetType=new TargetType {type=index},
                sprites=new SpriteList {new SpriteObject {icon=intact,uiSprite=true}},
                CountDrawer=new Count {count=4},ShowTheScoreForStar=new BoolScoreShow() });
            EditorUtility.SetDirty(lab);EditorUtility.SetDirty(targets);AssetDatabase.SaveAssets();
            if (ItemMailbag.CountBags(lab.levelData) != 4) throw new InvalidOperationException("O laboratório deve conter quatro malotes.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/mailbag-lab.result","READY: 7x7, quatro malotes, duas resistências, meta Mailbags, 30 jogadas, foguete e match preparado.");
        }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (SessionState.GetBool(PreviewKey,false) && EditorApplication.isPlaying && LevelManager.THIS != null
                && AI.THIS != null && LevelManager.THIS.gameStatus == GameState.Map)
            {
                SessionState.SetBool(PreviewKey,false);
                LevelManager.THIS.BoostPackage=LevelManager.THIS.BoostColorfullBomb=LevelManager.THIS.BoostStriped=0;
                LevelManager.THIS.gameStatus=GameState.PrepareGame;
                GetWindow<MailbagLaboratoryTools>("Malote");
            }
            if (!File.Exists(Request)) return;
            if (EditorApplication.isPlaying) {EditorApplication.isPlaying=false;return;}
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Prepare();
                foreach (string key in new[] {RouteCentralLaboratory.EnabledKey,FlyingLetterLaboratory.EnabledKey,
                    MailCubbyLaboratory.EnabledKey,FrankingMachineLaboratory.EnabledKey,InkLaboratory.EnabledKey,ParcelStackLaboratory.EnabledKey})
                    SessionState.SetBool(key,false);
                SessionState.SetBool(MailbagLaboratory.EnabledKey,true);
                SessionState.SetBool(PreviewKey,true);
                EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/main.unity");
                EditorApplication.isPlaying=true;
            }
            catch (Exception error) {File.WriteAllText("Logs/mailbag-lab.result",error.ToString());Debug.LogException(error);}
        }

        void OnInspectorUpdate() => Repaint();
        void OnGUI()
        {
            GUILayout.Label("Malote — laboratório jogável",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Dois impactos: rompe a cinta; depois remove e coleta. Cai com o dano preservado. A mesma combinação não deve consumir os dois impactos.",MessageType.Info);
            if (GUILayout.Button("Abrir / reiniciar laboratório")) Preview();
            if (GUILayout.Button("Desativar laboratório")) Disable();
            EditorGUILayout.HelpBox("Primeiro match: troque (2,3) com (2,2), contando de zero. Foguete vertical em (4,4) para atingir o malote da coluna 4.",MessageType.None);
            bool active=EditorApplication.isPlaying && MailbagLaboratory.IsActive && LevelManager.THIS?.field != null;
            using (new EditorGUI.DisabledScope(!active))
            {
                var bags=active ? LevelManager.THIS.field.GetItems().OfType<ItemMailbag>().OrderBy(b=>b.square.row).ThenBy(b=>b.square.col).ToArray() : new ItemMailbag[0];
                if (GUILayout.Button("Aplicar um impacto — diagnóstico")) bags.FirstOrDefault()?.TryHit();
                foreach (var bag in bags) GUILayout.Label("("+bag.square.col+","+bag.square.row+"): "+bag.RemainingHits+" impactos restantes");
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using RoyalAves.Effects;
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
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class InkwellLaboratoryTools : EditorWindow
    {
        const string Menu = "Royal Aves/Elementos/Tinteiro/";
        const string Request = "Logs/inkwell-lab.request";
        const string Art = "Assets/RoyalAves/Art/Obstacles/Inkwell/InkwellStages.png";
        const string MaterialPath = "Assets/RoyalAves/Resources/Effects/Inkwell/InkTint.mat";
        // Pixel registration measured in each 512px frame, from its top-left. Do not center on the
        // remaining silhouette: chips change its centroid. The unchanged gold foot is the landmark.
        static readonly Vector2[] RegistrationFeet =
        {
            new Vector2(287.5f,464), new Vector2(255,464), new Vector2(223.5f,464),
            new Vector2(287,432), new Vector2(252,432), new Vector2(229.5f,425)
        };
        static Vector2 RegisteredPivot(int index)
        {
            var delta=RegistrationFeet[index]-RegistrationFeet[0];
            // Center the intact visible alpha bounds, then preserve the registered foot across damage states.
            return new Vector2((288f+delta.x)/512f,(240.5f-delta.y)/512f);
        }
        static double nextPoll;
        static bool starting { get => SessionState.GetBool(InkwellLaboratory.EnabledKey + ".Starting", false); set => SessionState.SetBool(InkwellLaboratory.EnabledKey + ".Starting", value); }
        [InitializeOnLoadMethod] static void Init()
        {
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) { starting = false; InkwellLaboratory.Disable(); }
        }
        public static Sprite[] ImportRegisteredStages()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pare o Play antes de preparar os assets.");
            AssetDatabase.ImportAsset(Art, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 210;
            // Native sprite regions preserve the generated PNG bytes and the common frame size.
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existing = provider.GetSpriteRects();
            var regions = new SpriteRect[6];
            for (int i = 0; i < 6; i++) regions[i] = new SpriteRect
            {
                name = "Inkwell_" + i, alignment = SpriteAlignment.Custom, pivot = RegisteredPivot(i),
                spriteID = existing.FirstOrDefault(s=>s.name == "Inkwell_" + i)?.spriteID ?? GUID.Generate(),
                rect = new Rect((i % 3) * 512, i < 3 ? 512 : 0, 512, 512)
            };
            provider.SetSpriteRects(regions);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(regions.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Art).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (sprites.Length != 6) throw new InvalidOperationException("Esperados seis estágios importados.");
            return sprites;
        }
        [MenuItem(Menu + "Corrigir alinhamento dos sprites")]
        public static void RepairRegistration()
        {
            ImportRegisteredStages(); VerifyRegistration();
        }
        [MenuItem(Menu + "Preparar assets")]
        public static void Prepare()
        {
            var sprites=ImportRegisteredStages();
            var shader = Shader.Find("RoyalAves/InkStain");
            if (shader == null) throw new InvalidOperationException("Shader InkStain ausente.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
            var root = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EnchantedStoneLaboratory.PrefabPath));
            try
            {
                root.name = "InkwellCell";
                DestroyImmediate(root.GetComponent<EnchantedStone>());
                var square = root.GetComponent<Square>(); square.type = SquareTypes.Inkwell;
                var binding = root.GetComponent<BindLayer>();
                if (binding == null) binding = root.AddComponent<BindLayer>();
                binding.order = 6;
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = sprites[0]; renderer.color = Color.white;
                // Use the existing persistent default material rather than storing a temporary material.
                renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                root.transform.localScale = Vector3.one;
                var ink = root.AddComponent<InkwellCell>(); ink.stages = sprites; ink.tintMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(root, InkwellLaboratory.PrefabPath);
                root.name = "Inkwell";
                var production = PrefabUtility.SaveAsPrefabAsset(root, "Assets/SweetSugar/Resources/Blocks/Inkwell.prefab");
                RegisterProductionTarget(production, sprites[0]);
            }
            finally { DestroyImmediate(root); }
            var template = AssetDatabase.LoadAssetAtPath<LevelContainer>(FlyingLetterLaboratory.LevelPath);
            var level = AssetDatabase.LoadAssetAtPath<LevelContainer>(InkwellLaboratory.LevelPath);
            if (level == null) { level = CreateInstance<LevelContainer>(); AssetDatabase.CreateAsset(level, InkwellLaboratory.LevelPath); }
            level.levelData = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template.levelData));
            var data = level.levelData; data.fields = data.fields.Take(1).ToList(); data.currentSublevelIndex = 0;
            data.levelNum = 1; data.selectedTutorial = 0; data.limit = 40; data.limitType = LIMIT.MOVES;
            data.colorLimit = 4; data.pieceColors = new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            var field = data.fields[0]; field.layers = 2; field.frankingMachines.Clear();
            for (int y = 0; y < field.maxRows; y++) for (int x = 0; x < field.maxCols; x++)
            {
                var cell = field.levelSquares[y * field.maxCols + x];
                cell.blocks.Clear(); cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.EmptySquare });
                bool ink = x == field.maxCols / 2 && y == field.maxRows / 2;
                if (ink) cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.Inkwell });
                cell.block = ink ? SquareTypes.Inkwell : SquareTypes.EmptySquare;
                cell.blockLayer = 1; cell.obstacle = SquareTypes.NONE; cell.obstacleLayer = 1;
                cell.spawners.Clear(); cell.startWithoutItem = ink; cell.direction = Vector2.down; cell.enterSquare = y == 0;
                cell.isEnterTeleport = false; cell.teleportCoordinatesLinked = cell.teleportCoordinatesLinkedBack = new Vector2Int(-1,-1);
                cell.separatorIndexes = new bool[4]; cell.position = new Vector2Int(x,y);
                cell.item = new ItemForEditor { Item = Resources.Load<GameObject>("Items/Item"), ItemType = ItemsTypes.NONE, Color = (y + 2*x) % 4 };
            }
            // A real rocket can be swapped/activated to test direct power-up impacts.
            int middle = field.maxRows / 2;
            field.levelSquares[middle * field.maxCols].item = new ItemForEditor
                { Item = Resources.Load<GameObject>("Items/HORIZONTAL_STRIPED"), ItemType = ItemsTypes.HORIZONTAL_STRIPED, Color = 0 };
            data.LoadTargetObject(); int index = data.GetTargetIndex("Inkwell"); data.SetTarget(index);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(InkwellLaboratory.TargetPath);
            if (targets == null) { targets = CreateInstance<TargetLevel>(); AssetDatabase.CreateAsset(targets, InkwellLaboratory.TargetPath); }
            targets.targets = new TargetList { new TargetObject { targetType = new TargetType { type = index },
                sprites = new SpriteList { new SpriteObject { icon = sprites[0], uiSprite = true } },
                CountDrawer = new Count { count = 1 }, ShowTheScoreForStar = new BoolScoreShow() } };
            EditorUtility.SetDirty(level); EditorUtility.SetDirty(targets); EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            if (File.Exists("Logs/inkwell-error.result")) File.Delete("Logs/inkwell-error.result");
            File.WriteAllText("Logs/inkwell-prepare.result", "READY: seis sprites, material de preenchimento, prefab único, laboratório e meta de um tinteiro. Campanha preservada.");
        }
        [MenuItem(Menu + "Abrir laboratório")]
        public static void Open() { Directory.CreateDirectory("Logs"); File.WriteAllText(Request, "open"); }
        static void RegisterProductionTarget(GameObject prefab, Sprite icon)
        {
            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var target = config.targets.FirstOrDefault(t=>t.name=="Inkwell");
            if (target == null) { target = new TargetContainer { name="Inkwell" }; config.targets.Add(target); }
            target.collectAction=CollectingTypes.Destroy; target.setCount=SetCount.FromLevel;
            target.localization = new LocalizationIndexFolder
            {
                description = new SweetSugar.Scripts.Localization.LocalizationIndex { text="Quebre os tinteiros", index=-1 },
                failed = new SweetSugar.Scripts.Localization.LocalizationIndex { text="Ainda faltam tinteiros", index=-1 }
            };
            target.defaultSprites = new SprArrays { new SprArray { sprites = new SpriteList { new SpriteObject { icon=icon, uiSprite=true } } } };
            target.prefabs = new System.Collections.Generic.List<GameObject> { prefab };
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
        }
        [MenuItem(Menu + "Painel de teste")]
        public static void Panel() => GetWindow<InkwellLaboratoryTools>("Tinteiro");
        [MenuItem(Menu + "Desativar laboratório")]
        public static void Disable() { InkwellLaboratory.Disable(); starting = false; if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + .2;
            try
            {
                if (File.Exists(Request))
                {
                    string command = File.ReadAllText(Request).Trim();
                    if (command == "capture" && EditorApplication.isPlaying) { Capture(); File.Delete(Request); return; }
                    if (command == "status") { Status(); File.Delete(Request); return; }
                    if (command == "hit" && EditorApplication.isPlaying)
                    {
                        var ink = FindFirstObjectByType<InkwellCell>();
                        if (ink != null) ink.transform.parent.GetComponent<Square>().DestroyBlock();
                        File.Delete(Request); return;
                    }
                    if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                        throw new InvalidOperationException("Pare o Play antes de preparar/abrir o laboratório.");
                    File.Delete(Request);
                    if (command == "prepare") { Prepare(); return; }
                    if (command == "verify") { VerifyAssets(); return; }
                    if (command != "open") return;
                    bool changeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/SweetSugar/Scenes/game.unity";
                    if (changeScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                    Prepare();
                    foreach (string key in LightBallLaboratory.OtherKeys) SessionState.SetBool(key, false);
                    LightBallLaboratory.Disable();
                    SessionState.SetBool(InkwellLaboratory.EnabledKey, true); starting = true;
                    if (changeScene)
                        EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity");
                    Panel(); EditorApplication.isPlaying = true;
                }
                var manager = LevelManager.THIS;
                if (!EditorApplication.isPlaying || !starting || manager == null) return;
                if (manager.gameStatus == GameState.Map && AI.THIS != null)
                {
                    Application.runInBackground = true;
                    manager.BoostPackage = manager.BoostColorfullBomb = manager.BoostStriped = 0;
                    manager.gameStatus = GameState.PrepareGame;
                }
                if (manager.gameStatus == GameState.Tutorial) manager.gameStatus = GameState.Playing;
                if (manager.gameStatus == GameState.Playing && InkwellLaboratory.IsActive)
                {
                    starting = false;
                    foreach (var boost in manager.InGameBoosts) if (boost != null) boost.gameObject.SetActive(false);
                    File.WriteAllText("Logs/inkwell-ready.result", "READY: laboratório jogável. Atalhos de dano não substituem teste de matches e poderes reais.");
                }
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/inkwell-error.result", error.ToString()); Debug.LogException(error); starting = false;
                if (File.Exists(Request)) File.Delete(Request);
            }
        }
        void OnInspectorUpdate() => Repaint();
        void OnGUI()
        {
            EditorGUILayout.LabelField("Tinteiro — cinco impactos", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Quatro vizinhos, um por impacto. No quinto, quebra e pinta a própria célula. A peça leva a tinta; o chão continua pintado. A cor lógica do match não muda.", MessageType.Info);
            if (GUILayout.Button("Abrir laboratório (fora do Play)")) Open();
            if (!EditorApplication.isPlaying || !InkwellLaboratory.IsActive) return;
            var ink = FindFirstObjectByType<InkwellCell>();
            if (ink != null)
            {
                EditorGUILayout.LabelField($"Acertos: {ink.Hits}/5");
                ink.tintDuration = EditorGUILayout.Slider("Duração para tingir", ink.tintDuration, .15f, 1.5f);
                if (GUILayout.Button("Impacto de diagnóstico")) ink.transform.parent.GetComponent<Square>().DestroyBlock();
            }
            if (GUILayout.Button("Capturar tabuleiro")) Capture();
            if (GUILayout.Button("Desativar e sair do Play")) Disable();
        }
        static void Status()
        {
            var manager = LevelManager.THIS;
            File.WriteAllText("Logs/inkwell-status.result", $"play={EditorApplication.isPlaying}; scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}; active={InkwellLaboratory.IsActive}; state={manager?.gameStatus}; stains={FindObjectsByType<InkwellStainedCell>(FindObjectsSortMode.None).Length}; pieces={FindObjectsByType<InkwellStainedPiece>(FindObjectsSortMode.None).Length}; hits={FindFirstObjectByType<InkwellCell>()?.Hits}");
        }
        internal static void Capture(string path = "Logs/inkwell-board.png")
        {
            var field = LevelManager.THIS.field;
            var first = field.GetSquare(0,0).transform.position;
            var last = field.GetSquare(field.fieldData.maxCols-1,field.fieldData.maxRows-1).transform.position;
            var root = new GameObject("InkwellCaptureCamera");
            var camera = root.AddComponent<Camera>(); camera.enabled = false; camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(Mathf.Abs(first.x-last.x),Mathf.Abs(first.y-last.y))*.5f + 1;
            camera.transform.position = (first+last)*.5f + Vector3.back*20;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f,.06f,.15f);
            var target = new RenderTexture(1024,1024,24); var texture = new Texture2D(1024,1024,TextureFormat.RGBA32,false);
            var previous = RenderTexture.active;
            try { camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1024,1024),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG()); }
            finally { RenderTexture.active=previous;camera.targetTexture=null;target.Release();DestroyImmediate(target);DestroyImmediate(texture);DestroyImmediate(root); }
        }
        public static void VerifyAssets()
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(InkwellLaboratory.PrefabPath);
            if (root == null || root.transform.parent != null || root.GetComponent<InkwellCell>() == null) throw new Exception("Prefab inválido.");
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) != 0) throw new Exception("Script ausente no tinteiro.");
            var stages = root.GetComponent<InkwellCell>().stages;
            if (stages.Length != 6 || stages.Any(s=>s == null || s.uv.Distinct().Count()<4)) throw new Exception("Sprites ou UVs inválidos.");
            var shader = Shader.Find("RoyalAves/InkStain");
            if (shader == null || UnityEditor.ShaderUtil.ShaderHasError(shader)) throw new Exception("Shader com erro.");
            if (root.GetComponent<InkwellCell>().tintMaterial == null) throw new Exception("Material ausente.");
            VerifyRegistration();
            File.WriteAllText("Logs/inkwell-assets-verification.result", "PASS: prefab raiz única, seis sprites com UVs válidos, material referenciado e shader importado sem erro. Não é validação de input/partida completa.");
        }
        public static void VerifyRegistration()
        {
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Art).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            if(sprites.Length!=6)throw new Exception("Seis estágios são necessários para verificar o alinhamento.");
            var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(Art));
            var pixels=texture.GetPixels32(); Vector2 reference=Vector2.zero;
            var report=new System.Text.StringBuilder();
            try
            {
                for(int i=0;i<sprites.Length;i++)
                {
                    var sprite=sprites[i];var rect=sprite.rect;
                    int minGold=512,maxGold=-1,minOpaque=512,maxOpaque=-1,bottom=0;
                    for(int y=0;y<512;y++)for(int x=0;x<512;x++)
                    {
                        var color=pixels[((int)rect.y+511-y)*texture.width+(int)rect.x+x];
                        if(color.a<=160)continue;
                        minOpaque=Math.Min(minOpaque,x);maxOpaque=Math.Max(maxOpaque,x);bottom=Math.Max(bottom,y+1);
                        if(y>=330 && y<490 && color.r>165 && color.g>85 && color.b<110 && color.r>color.g*1.08f)
                        {minGold=Math.Min(minGold,x);maxGold=Math.Max(maxGold,x);}
                    }
                    float center=i<5?(minGold+maxGold)*.5f:(minOpaque+maxOpaque)*.5f;
                    var local=(new Vector2(center,512-bottom)-sprite.pivot)/sprite.pixelsPerUnit;
                    if(i==0)reference=local;
                    float drift=Vector2.Distance(local,reference)*sprite.pixelsPerUnit;
                    if(drift>.02f)throw new Exception($"Estágio {i} desloca a base em {drift:F3} pixels.");
                    report.AppendLine($"stage={i}; foot={local}; driftPixels={drift:F4}; pivot={sprite.pivot}; ppu={sprite.pixelsPerUnit}");
                }
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/inkwell-registration-verification.result","PASS: measured gold-foot centers/support lines coincide in all five standing states; rubble support also aligned; common centered placement and all frame scales preserved.\n"+report);
            }
            finally {DestroyImmediate(texture);}
        }
        public static void VerifyAuthoring()
        {
            var prefab=Resources.Load<GameObject>("Blocks/Inkwell");
            if(prefab==null || prefab.GetComponent<Square>().type!=SquareTypes.Inkwell || prefab.GetComponent<BindLayer>()==null)
                throw new Exception("Prefab de produção inválido.");
            if(prefab.GetComponent<BindLayer>().order!=6)throw new Exception("Tinteiro deve ficar acima do piso/grama, como os outros obstáculos fixos.");
            if((int)SquareTypes.Inkwell!=16 || (int)SquareTypes.EnchantedStone!=15)
                throw new Exception("Índices antigos do enum mudaram.");
            var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            if(config.targets.Count(t=>t.name=="Inkwell")!=1 || config.targets.FindIndex(t=>t.name=="EnchantedStone")!=14)
                throw new Exception("Catálogo duplicado ou índices antigos alterados.");
            var texture=SweetSugar.Scripts.Editor.SquareUtils.GetSquareTexture(SquareTypes.Inkwell);
            if(texture.Texture2D==null || texture.Texture2D.width!=512)throw new Exception("Ícone da paleta ausente ou usando a prancha inteira.");
            var source=AssetDatabase.LoadAssetAtPath<LevelContainer>(InkwellLaboratory.LevelPath).levelData;
            var copy=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(source));
            var editor=CreateInstance<SweetSugar.Scripts.Editor.LevelMakerEditor>();
            var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var type=editor.GetType();
            void Set(string name,object value)=>type.GetField(name,flags).SetValue(editor,value);
            try
            {
                Set("levelData",copy);Set("levelNumber",102);Set("subLevelNumber",1);Set("layer",0);
                Set("squareType",SquareTypes.Inkwell);
                type.GetMethod("SetSquareType",flags).Invoke(editor,new object[]{0,0});
                if(!(bool)type.GetField("pendingInkwellTarget",flags).GetValue(editor))throw new Exception("Pincel não solicita meta automática.");
                var roundtrip=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(copy));
                if(!roundtrip.GetBlock(0,0).blocks.Any(b=>b.squareType==SquareTypes.Inkwell))throw new Exception("Pincel/save/load do tinteiro falhou.");
                var layered=roundtrip.GetBlock(0,0);
                layered.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.SugarSquare});layered.SortMergeBlocks();
                if(layered.blocks.Last().squareType!=SquareTypes.Inkwell)throw new Exception("Grama ocultou o tinteiro na ordenação.");
                if(Resources.Load<GameObject>("Blocks/SolidBlock").GetComponent<InkwellCell>()!=null)throw new Exception("Caixas foram substituídas pelo tinteiro.");
                File.WriteAllText("Logs/inkwell-authoring-verification.result","PASS: enum append-only 16, independent resource prefab/BindLayer, one appended objective, separate cropped palette icon, real level-editor brush/save-load and automatic target request; crates preserved. In-memory test only; campaign layouts not saved.");
            }
            finally { DestroyImmediate(editor); }
        }
    }
}

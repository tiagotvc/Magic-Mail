// Replaces the Pacote Explosivo's bomb art with the dynamite. The resting look is a single sprite with the fuse off
// (Art/Specials/dinamite-idle.png). The explosion plays the spritesheet (Art/Specials/dinamite-sheet.png, 5x2 grid) once,
// from frame 0 (fuse lights) to frame 9 (smoke fades). The PACKAGE prefab gets a controller with an Idle and an Explode
// state and an "Explode" trigger, which ItemDestroyAnimation.DestroyPackage fires.
// Menu: Royal Aves > Aplicar dinamite no pacote explosivo.
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesDinamiteBuilder
    {
        public const string SheetPath = "Assets/RoyalAves/Art/Specials/dinamite-sheet.png";
        public const string IdlePath = "Assets/RoyalAves/Art/Specials/dinamite-idle.png";
        // The same entrance the other power-ups play (Square.cs fires "bonus_appear" on them).
        const string AppearClipPath = "Assets/SweetSugar/Animation/horr_appearing.anim";
        const string PackagePath = "Assets/SweetSugar/Resources/Items/PACKAGE.prefab";
        const string AnimFolder = "Assets/RoyalAves/Animations/Dinamite";
        const int Columns = 5;
        const int Rows = 2;
        const int ExplodeFirst = 0, ExplodeLast = 9;
        const float ExplodeFps = 24f;

        [MenuItem("Royal Aves/Aplicar dinamite no pacote explosivo")]
        public static void Apply()
        {
            var frames = SliceSheet();
            var idleSprite = LoadIdleSprite();
            var idle = BuildClip("dinamite_idle", new[] { idleSprite }, 0, 0, 1f, loop: true);
            var explode = BuildClip("dinamite_explode", frames, ExplodeFirst, ExplodeLast, ExplodeFps, loop: false);
            var appear = AssetDatabase.LoadAssetAtPath<AnimationClip>(AppearClipPath);
            if (appear == null) throw new System.InvalidOperationException($"Clipe não encontrado: {AppearClipPath}");
            var controller = BuildController(idle, explode, appear);
            UpdatePrefab(controller, idleSprite);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("Dinamite aplicada ao PACKAGE: repouso com pavio apagado e explosão (frames 0-9 da folha).");
        }

        static Sprite[] SliceSheet()
        {
            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            var cellWidth = texture.width / Columns;
            var cellHeight = texture.height / Rows;

            // The sheet is Multiple by the importer (RoyalAvesAssetImporter), so only the sprite rects are set here.
            var rects = new SpriteRect[Columns * Rows];
            for (var i = 0; i < rects.Length; i++)
            {
                var col = i % Columns;
                var row = i / Columns;
                rects[i] = new SpriteRect
                {
                    name = $"dinamite_{i:00}",
                    rect = new Rect(col * cellWidth, texture.height - (row + 1) * cellHeight, cellWidth, cellHeight),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                    spriteID = GUID.Generate()
                };
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(rects);
            dataProvider.Apply();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        static Sprite LoadIdleSprite()
        {
            AssetDatabase.ImportAsset(IdlePath, ImportAssetOptions.ForceUpdate);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IdlePath);
            if (sprite == null) throw new System.InvalidOperationException($"Sprite não encontrado: {IdlePath}");
            return sprite;
        }

        static AnimationClip BuildClip(string name, Sprite[] sprites, int first, int last, float fps, bool loop)
        {
            EnsureFolder(AnimFolder);
            var clip = new AnimationClip { frameRate = fps };
            var frames = last - first + 1;
            var keys = new ObjectReferenceKeyframe[loop ? frames + 1 : frames];
            for (var i = 0; i < keys.Length; i++)
            {
                var frame = first + (i < frames ? i : 0);
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[frame] };
            }
            // Both sprite renderers of the package: "Sprite" (root child) and "Sprite/Sprite" (nested under it).
            foreach (var path in new[] { "Sprite", "Sprite/Sprite" })
            {
                var binding = new EditorCurveBinding { path = path, type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var assetPath = $"{AnimFolder}/{name}.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if (existing != null) AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }

        static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static AnimatorController BuildController(AnimationClip idle, AnimationClip explode, AnimationClip appear)
        {
            var path = $"{AnimFolder}/Dinamite.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Explode", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("bonus_appear", AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle");
            idleState.motion = idle;
            machine.defaultState = idleState;

            var appearState = machine.AddState("BonusAppear");
            appearState.motion = appear;
            var appearBack = appearState.AddTransition(idleState);
            appearBack.hasExitTime = true;
            appearBack.exitTime = 1f;
            appearBack.duration = 0f;
            var appearTransition = machine.AddAnyStateTransition(appearState);
            appearTransition.hasExitTime = false;
            appearTransition.duration = 0f;
            appearTransition.AddCondition(AnimatorConditionMode.If, 0, "bonus_appear");

            var explodeState = machine.AddState("Explode");
            explodeState.motion = explode;

            var transition = machine.AddAnyStateTransition(explodeState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0, "Explode");
            return controller;
        }

        static void UpdatePrefab(AnimatorController controller, Sprite firstFrame)
        {
            var root = PrefabUtility.LoadPrefabContents(PackagePath);
            try
            {
                var animator = root.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    renderer.sprite = firstFrame;
                PrefabUtility.SaveAsPrefabAsset(root, PackagePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class BoardGoalTools
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            const string request = "Logs/board-goals.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Apply(); Validate(); }
            catch (Exception error) { File.WriteAllText("Logs/board-goals.result", error.ToString()); Debug.LogException(error); }
        }

        [MenuItem("Royal Aves/Elementos/Atualizar metas de gelo e caixa")]
        public static void Apply()
        {
            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var ice = config.targets.First(t => t.name == "IceBlock");
            ice.setCount = SetCount.FromLevel;
            int crateIndex = config.targets.FindIndex(t => t.name == "SolidBlock");
            var crate = config.targets[crateIndex];
            foreach (var group in crate.defaultSprites) SelectBase(group.sprites);
            EditorUtility.SetDirty(config);
            foreach (var guid in AssetDatabase.FindAssets("t:TargetLevel", new[] {
                "Assets/SweetSugar/Resources/Levels/Targets", "Assets/RoyalAves/Laboratory" }))
            {
                var level = AssetDatabase.LoadAssetAtPath<TargetLevel>(AssetDatabase.GUIDToAssetPath(guid));
                bool changed = false;
                if (level.targets == null) continue;
                foreach (var goal in level.targets)
                    if (goal.targetType.type == crateIndex) changed |= SelectBase(goal.sprites);
                if (changed) EditorUtility.SetDirty(level);
            }
            AssetDatabase.SaveAssets();
        }

        static bool SelectBase(SpriteList sprites)
        {
            if (sprites == null || sprites.Count == 0) return false;
            // Find the base state by asset identity, even if an older target has a different order.
            var baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/RoyalAves/Art/Obstacles/ParcelStack/CM_pilha_estado01_00.png");
            bool changed = false;
            foreach (var sprite in sprites)
            {
                bool selected = sprite.icon == baseSprite;
                if (sprite.uiSprite != selected) { sprite.uiSprite = selected; changed = true; }
            }
            return changed;
        }

        [MenuItem("Royal Aves/Elementos/Validar metas de gelo e caixa")]
        public static void Validate()
        {
            var previous = LevelData.THIS;
            LevelData data = null;
            try
            {
                var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
                var thin = new SquareBlocks(); thin.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.IceBlock });
                var thick = new SquareBlocks();
                thick.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.IceBlock });
                thick.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.IceBlock });
                var legacy = new SquareBlocks { obstacle = SquareTypes.IceBlock, obstacleLayer = 2 };
                var stale = new SquareBlocks { block = SquareTypes.IceBlock };
                stale.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.EmptySquare });
                data = new LevelData(false, 1);
                data.fields.Add(new FieldData { levelSquares = new[] { thin, thick, stale } });
                data.fields.Add(new FieldData { levelSquares = new[] { legacy } });
                Expect(IceLayer.CountCells(data) == 3, "Count cells once across thin, thick, legacy and multiple fields.");
                data.SetTarget(config.targets.FindIndex(t => t.name == "IceBlock"));
                Expect(data.targetObject is SweetSugar.Scripts.TargetScripts.IceBlock, "Ice primary goal must load by reflection.");
                var definition = config.targets.First(t => t.name == "IceBlock");
                var entry = new TargetObject { sprites = definition.defaultSprites[0].sprites.Copy(), CountDrawer = new Count { count = 999 }, ShowTheScoreForStar = new BoolScoreShow() };
                var counter = new TargetCounter(definition.prefabs[0], 999, entry.sprites.Select(s => s.icon).ToArray(), -1, definition, false, entry, data);
                Expect(counter.count == 3 && counter.GetCount() == 3 && counter.GetCount(true) == 3,
                    "Start popup and game goal must use painted cells, ignoring manual or scene counts.");
                thick.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.IceBlock });
                Expect(IceLayer.CountCells(data) == 3, "Extra protection must not increase the objective.");
                var crate = config.targets.First(t => t.name == "SolidBlock");
                var baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Obstacles/ParcelStack/CM_pilha_estado01_00.png");
                var crateEntry = new TargetObject { sprites = crate.defaultSprites[0].sprites.Copy(), CountDrawer = new Count(), ShowTheScoreForStar = new BoolScoreShow() };
                // Deliberately stale per-level UI flags must still display the canonical base crate.
                foreach (var sprite in crateEntry.sprites) sprite.uiSprite = sprite == crateEntry.sprites.Last();
                var crateCounter = new TargetCounter(crate.prefabs[0], 0, crateEntry.sprites.Select(s => s.icon).ToArray(), -1, crate, false, crateEntry);
                Expect(crateCounter.extraObject == baseSprite, "Crate HUD icon must be the crate without planks.");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/board-goals.result", "PASS: automatic ice count from authored cells; thin/thick count once; multiple fields and legacy cells; primary-goal loading; popup and HUD count 3 instead of 999; stale crate icon resolves to base crate.");
            }
            finally
            {
                if (data?.targetObject != null) TargetComponent.OnDestroyEvent -= data.targetObject.DestroyEvent;
                LevelData.THIS = previous;
            }
        }

        static void Expect(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}

// Applies the Correio Mágico (Royal Aves) art to the Sweet Sugar match-3 template; music and sound effects stay Sweet Sugar's.
// Menu: Royal Aves > Aplicar visual do Correio Mágico, and Royal Aves > Restaurar sons do Sweet Sugar (undoes the audio
// swap made by earlier versions of this tool).
//
// Every reference to a Sweet Sugar candy, bonus or block sprite is swapped for its Correio Mágico counterpart in the item
// and block prefabs, the level and target assets and the scenes. Swapping references (instead of overwriting the PNGs)
// matters because the target system compares sprite names: a piece and its goal must point to the same sprite.
// Correio Mágico has 7 pieces and Sweet Sugar 6 candy colours, so a 7th colour is added to every colour list.
// Safe to run more than once. The originals are in <projeto>/Backups/SweetSugar-original, because Assets/SweetSugar is
// not under git.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesReskin
    {
        const string ArtRoot = "Assets/RoyalAves/Art/";
        const string SweetSugarRoot = "Assets/SweetSugar/";
        const string SweetSugarAudio = SweetSugarRoot + "Audio/wav/";
        const string OldItemArt = SweetSugarRoot + "Textures_png/Items/";

        // In colour order: Sweet Sugar colour i becomes piece i.
        static readonly string[] PieceNames = { "envelope", "gift", "key", "stamp", "bow", "quill", "ink" };
        static readonly int PieceCount = PieceNames.Length;
        static readonly string[] PrefabFolders = { SweetSugarRoot + "Resources/Items", SweetSugarRoot + "Resources/Blocks", SweetSugarRoot + "Prefabs" };
        static readonly string[] ScenePaths = { SweetSugarRoot + "Scenes/game.unity", SweetSugarRoot + "Scenes/gameStatic.unity", SweetSugarRoot + "Scenes/main.unity" };

        [MenuItem("Royal Aves/Aplicar visual do Correio Mágico")]
        static void ApplyFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Correio Mágico",
                    "Troca doces, bônus, blocos e o fundo do tabuleiro do Sweet Sugar pelos do Correio Mágico, " +
                    "com 7 peças (envelope, presente, chave, selo, laço, pena e tinta). Música e efeitos continuam os do Sweet Sugar.\n\n" +
                    "Os originais estão em Backups/SweetSugar-original.",
                    "Aplicar", "Cancelar")) return;
            if (!RoyalAvesTools.CanRunTool()) return;

            var report = Apply();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Correio Mágico", report, "OK");
        }

        [MenuItem("Royal Aves/Restaurar sons do Sweet Sugar")]
        static void RestoreAudioFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Sons do Sweet Sugar",
                    "Volta a música e os efeitos originais do Sweet Sugar (no prefab SoundBase e nas cenas). Peças, fundo e lobby não mudam.",
                    "Restaurar", "Cancelar")) return;
            if (!RoyalAvesTools.CanRunTool()) return;

            var report = RestoreSweetSugarAudio();
            Debug.Log(report);
            EditorUtility.DisplayDialog("Sons do Sweet Sugar", report, "OK");
        }

        public static string Apply()
        {
            EnsureImportSettings();
            var pieces = PieceNames.Select(n => LoadSprite(ArtRoot + "Pieces/" + n)).ToArray();
            // The package's Animator always shows its "Idle" state's sprite (dinamite-idle), so the
            // reskin must point everywhere else at the same art, not the older bomb.png.
            var bomb = LoadSprite(ArtRoot + "Specials/dinamite-idle");
            var map = BuildSpriteMap(pieces, bomb);

            var log = new StringBuilder("Correio Mágico aplicado.\n\n");
            log.AppendLine($"Prefabs alterados: {ReskinPrefabs(map, pieces, bomb)}");
            log.AppendLine($"Fases e objetivos alterados: {ReskinLevels(map, pieces)}");
            ReskinScenes(map, LoadSprite(ArtRoot + "Backgrounds/area-central-stage-0"), log);
            AssetDatabase.SaveAssets();
            return log.ToString();
        }

        // Art imported before RoyalAvesAssetImporter compiled keeps Unity's defaults; reimporting lets the importer apply.
        internal static void EnsureImportSettings()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/RoyalAves/Art" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                    (importer.textureType != TextureImporterType.Sprite ||
                     !Mathf.Approximately(importer.spritePixelsPerUnit, RoyalAvesAssetImporter.PixelsPerUnit(path))))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        static Dictionary<Object, Object> BuildSpriteMap(Sprite[] pieces, Sprite bomb)
        {
            var map = new Dictionary<Object, Object>();
            // The 6 Sweet Sugar candy colours become the first 6 pieces; FitColors adds the 7th.
            MapColors(map, "Item", i => pieces[Mathf.Min(i, PieceCount - 1)]);
            // Correio Mágico bonus pieces have no colour: every colour of a bonus shows the same sprite.
            // "Rocket" (HORIZONTAL_STRIPED/VERTICAL_STRIPED) art is the emerald capsule, rotated level from its
            // original diagonal reference art - see RocketSplitEffect for the matching split-and-fly halves.
            MapColors(map, "HORIZONTAL_STRIPED", _ => LoadSprite(ArtRoot + "Specials/capsule-horizontal"));
            // Same artwork as HORIZONTAL_STRIPED, rotated 90° on the prefab's transform.
            MapColors(map, "VERTICAL_STRIPED", _ => LoadSprite(ArtRoot + "Specials/capsule-horizontal"));
            MapColors(map, "MARMALADE", _ => LoadSprite(ArtRoot + "Specials/plane"));
            MapColors(map, "MULTICOLOR", _ => LoadSprite(ArtRoot + "Specials/orb"));
            MapSprite(map, OldItemArt + "game_item_g", bomb);                                          // package wrapper
            MapSprite(map, OldItemArt + "game_item_c_1", LoadSprite(ArtRoot + "Obstacles/crate"));     // solid block, last layer
            MapSprite(map, OldItemArt + "game_item_c_2", LoadSprite(ArtRoot + "Obstacles/parcel"));
            MapSprite(map, OldItemArt + "game_item_c_3", LoadSprite(ArtRoot + "Obstacles/parcel"));
            MapSprite(map, OldItemArt + "game_item_a_1", LoadSprite(ArtRoot + "Obstacles/grass-sparse")); // sugar square, last layer
            MapSprite(map, OldItemArt + "game_item_a_2", LoadSprite(ArtRoot + "Obstacles/grass"));
            MapSprite(map, OldItemArt + "game_item_b", LoadSprite(ArtRoot + "Obstacles/ice"));         // wire block

            // Level assets keep editor thumbnails as textures, not sprites.
            foreach (var pair in map.ToList())
                map[((Sprite)pair.Key).texture] = ((Sprite)pair.Value).texture;
            return map;
        }

        static void MapColors(Dictionary<Object, Object> map, string itemPrefab, Func<int, Sprite> replacement)
        {
            var colorable = AssetDatabase.LoadAssetAtPath<GameObject>(SweetSugarRoot + "Resources/Items/" + itemPrefab + ".prefab")
                .GetComponentInChildren<IColorableComponent>(true);
            var sprites = colorable.Sprites[0].Sprites;
            for (var i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) map[sprites[i]] = replacement(i);
        }

        static void MapSprite(Dictionary<Object, Object> map, string oldPathWithoutExtension, Sprite replacement)
        {
            var old = AssetDatabase.LoadAssetAtPath<Sprite>(oldPathWithoutExtension + ".png");
            if (old != null) map[old] = replacement;
        }

        static int ReskinPrefabs(Dictionary<Object, Object> map, Sprite[] pieces, Sprite bomb)
        {
            var count = 0;
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", PrefabFolders).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var isPackage = root.name == "PACKAGE";
                var changed = RemapHierarchy(root, map) > 0;
                foreach (var colorable in root.GetComponentsInChildren<IColorableComponent>(true))
                    changed |= FitColors(colorable, pieces, isPackage ? bomb : null);
                if (isPackage)
                {
                    foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true).Where(r => pieces.Contains(r.sprite)))
                    {
                        renderer.sprite = bomb;
                        EditorUtility.SetDirty(renderer);
                        changed = true;
                    }
                }
                if (changed) count++;
            }
            return count;
        }

        // Gives every colour list one entry per piece. Candy lists become the pieces; bonus lists repeat their colourless
        // sprite (the package shows the bomb for every colour); other lists repeat their last sprite.
        static bool FitColors(IColorableComponent colorable, Sprite[] pieces, Sprite sameForAll)
        {
            var changed = false;
            foreach (var perLevel in colorable.Sprites.Where(l => l?.Sprites != null && l.Sprites.Length > 1))
            {
                var old = perLevel.Sprites;
                var isPieceList = old.Select((sprite, i) => i < pieces.Length && sprite == pieces[i]).All(same => same);
                var sprites = Enumerable.Range(0, PieceCount)
                    .Select(i => sameForAll != null ? sameForAll : i < old.Length ? old[i] : isPieceList ? pieces[i] : old[old.Length - 1])
                    .ToArray();
                if (sprites.SequenceEqual(old)) continue;
                perLevel.Sprites = sprites;
                changed = true;
            }
            if (changed) EditorUtility.SetDirty(colorable);
            return changed;
        }

        static int ReskinLevels(Dictionary<Object, Object> map, Sprite[] pieces)
        {
            var count = 0;
            var paths = AssetDatabase.FindAssets("t:ScriptableObject", new[] { SweetSugarRoot + "Resources/Levels" }).Select(AssetDatabase.GUIDToAssetPath);
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                var changed = Remap(asset, map);
                if (asset is LevelContainer level) changed |= LimitLevelColors(level.levelData);
                if (asset is TargetLevel targetLevel)
                    foreach (var target in targetLevel.targets)
                        changed |= KeepOnlyPieces(target.sprites, pieces);
                if (asset is TargetEditorScriptable targetEditor) changed |= FitCollectTargets(targetEditor, pieces);
                if (!changed) continue;
                EditorUtility.SetDirty(asset);
                count++;
            }
            return count;
        }

        static bool LimitLevelColors(LevelData data)
        {
            if (data == null) return false;
            var changed = data.colorLimit > PieceCount;
            data.colorLimit = Mathf.Min(data.colorLimit, PieceCount);
            var squares = data.fields.Where(f => f?.levelSquares != null).SelectMany(f => f.levelSquares);
            foreach (var square in squares.Where(s => s?.item != null && s.item.Color >= PieceCount))
            {
                square.item.Color = PieceCount - 1;
                changed = true;
            }
            return changed;
        }

        // A Sweet Sugar colour goal counted the candy plus the striped and marmalade candies of that colour. Correio
        // Mágico bonus pieces have no colour, so a goal keeps only its piece; otherwise a rocket would count for every colour.
        static bool KeepOnlyPieces(SpriteList sprites, Sprite[] pieces)
        {
            if (sprites == null || !sprites.Any(s => pieces.Contains(s.icon))) return false;
            var changed = false;
            for (var i = sprites.Count - 1; i >= 0; i--)
            {
                if (pieces.Contains(sprites[i].icon)) continue;
                sprites.RemoveAt(i);
                changed = true;
            }
            return changed;
        }

        // The "collect items" goal has one sprite group per colour; Sweet Sugar ships 5, and each piece needs its own so the
        // Level Maker can use it as a goal.
        static bool FitCollectTargets(TargetEditorScriptable targetEditor, Sprite[] pieces)
        {
            var collect = targetEditor.targets.FirstOrDefault(t => t.name == "CollectItems");
            if (collect == null) return false;
            var changed = false;
            foreach (var group in collect.defaultSprites)
                changed |= KeepOnlyPieces(group.sprites, pieces);
            foreach (var piece in pieces.Where(p => !collect.defaultSprites.Any(g => g.sprites.Any(s => s.icon == p))))
            {
                var sprites = new SpriteList();
                sprites.Add(new SpriteObject { icon = piece });
                collect.defaultSprites.Add(new SprArray { sprites = sprites });
                changed = true;
            }
            return changed;
        }

        static void ReskinScenes(Dictionary<Object, Object> map, Sprite background, StringBuilder log)
        {
            var oldBackground = AssetDatabase.LoadAssetAtPath<Sprite>(SweetSugarRoot + "Textures_png/Backgrounds/background_01.png");
            ForEachScene(scene =>
            {
                var changes = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    changes += ReplaceBackground(root, oldBackground, background);
                    changes += RemapHierarchy(root, map);
                }
                log.AppendLine($"Cena {scene.name}: {changes} alterações");
                return changes > 0;
            });
        }

        // Opens each scene, lets the action change it and saves it when the action says so; restores the open scenes after.
        static void ForEachScene(Func<UnityEngine.SceneManagement.Scene, bool> action)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (var path in ScenePaths)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    if (!action(scene)) continue;
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (setup.Length > 0 && setup.All(s => !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static int ReplaceBackground(GameObject root, Sprite from, Sprite to)
        {
            if (from == null) return 0;
            var count = 0;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite == from))
            {
                // Cover at least the area the old background covered; the new one is 9:16.
                var oldSize = from.bounds.size;
                var newSize = to.bounds.size;
                renderer.transform.localScale *= Mathf.Max(oldSize.x / newSize.x, oldSize.y / newSize.y);
                renderer.sprite = to;
                RecordChange(renderer.transform);
                RecordChange(renderer);
                count++;
            }
            foreach (var image in root.GetComponentsInChildren<Image>(true).Where(i => i.sprite == from))
            {
                image.sprite = to;
                RecordChange(image);
                count++;
            }
            return count;
        }

        // Earlier versions of this tool replaced the Sweet Sugar audio with the prototype's test sounds. This puts back the
        // clips the template ships with: in the SoundBase prefab, and by dropping the scene overrides the tool had added.
        public static string RestoreSweetSugarAudio()
        {
            var log = new StringBuilder("Sons do Sweet Sugar restaurados.\n\n");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SweetSugarRoot + "Prefabs/SoundBase.prefab");
            foreach (var sound in prefab.GetComponentsInChildren<SoundBase>(true))
                SetSweetSugarSounds(sound);
            AssetDatabase.SaveAssets();
            log.AppendLine("Prefab SoundBase: efeitos originais");
            ForEachScene(scene =>
            {
                var changes = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var sound in root.GetComponentsInChildren<SoundBase>(true))
                    {
                        RestoreComponent(sound, () => SetSweetSugarSounds(sound));
                        changes++;
                    }
                    foreach (var music in root.GetComponentsInChildren<MusicBase>(true))
                    {
                        RestoreComponent(music, () => SetSweetSugarMusic(music));
                        changes++;
                    }
                }
                log.AppendLine($"Cena {scene.name}: {changes} componentes de áudio");
                return changes > 0;
            });
            return log.ToString();
        }

        static void RestoreComponent(Component component, Action setOriginal)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RevertObjectOverride(component, InteractionMode.AutomatedAction);
            else
                setOriginal();
        }

        static void SetSweetSugarSounds(SoundBase sound)
        {
            AudioClip Clip(string name) => LoadClip(SweetSugarAudio + name + ".wav");
            sound.click = Clip("Button");
            sound.destroy = new[] { Clip("Jelly_destroy"), Clip("Jelly_destroy_02") };
            sound.strippedExplosion = Clip("Stripes_bonus");
            sound.explosion = sound.explosion2 = Clip("Explosion");
            sound.colorBombExpl = Clip("Color_bomb");
            sound.boostBomb = Clip("Burning_wick");
            sound.block_destroy = Clip("Block_jelly_choco_destroy");
            sound.complete = new[] { Clip("Level_complete"), Clip("Cheers") };
            sound.gameOver = new[] { Clip("Game_over_1"), Clip("Game_over_2") };
            RecordChange(sound);
        }

        static void SetSweetSugarMusic(MusicBase music)
        {
            music.music = new[] { "game_map_music", "game_music", "game_music_2" }.Select(n => LoadClip(SweetSugarAudio + n + ".wav")).ToArray();
            RecordChange(music);
        }

        static int RemapHierarchy(GameObject root, Dictionary<Object, Object> map)
        {
            var count = 0;
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component != null && Remap(component, map)) count++;
            return count;
        }

        // Replaces every object reference found in the map, at any depth of the serialized data.
        static bool Remap(Object target, Dictionary<Object, Object> map)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            var changed = false;
            var enterChildren = true;
            while (property.Next(enterChildren))
            {
                enterChildren = property.propertyType != SerializedPropertyType.String;
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null) continue;
                if (!map.TryGetValue(property.objectReferenceValue, out var replacement) || replacement == property.objectReferenceValue) continue;
                property.objectReferenceValue = replacement;
                changed = true;
            }
            if (!changed) return false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            return true;
        }

        static void RecordChange(Object target)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        static Sprite LoadSprite(string pathWithoutExtension)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pathWithoutExtension + ".png");
            if (sprite == null) sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pathWithoutExtension + ".jpg");
            if (sprite == null)
                throw new InvalidOperationException($"Sprite não encontrado: {pathWithoutExtension}. Reimporte Assets/RoyalAves/Art.");
            return sprite;
        }

        static AudioClip LoadClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new InvalidOperationException($"Áudio não encontrado: {path}");
            return clip;
        }
    }
}

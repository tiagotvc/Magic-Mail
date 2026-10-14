using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.System.Combiner;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class PowerUpFormationTestTools
    {
        static double reportAt;
        static double captureAt;
        static SweetSugar.Scripts.Blocks.Square destination;
        static ItemsTypes expected;
        static int initialCount;
        static bool rocketTest;
        [MenuItem("Royal Aves/Efeitos/Testar criação de power-up no laboratório")]
        static void Request() { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/power-up-formation.request", "line4"); }
        [InitializeOnLoadMethod]
        static void Initialize() { EditorApplication.update -= Poll; EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !Application.isPlaying) return;
            var manager = LevelManager.THIS;
            if (manager?.field == null) return;
            bool laboratory = FrankingMachineLaboratory.IsActive || MailCubbyLaboratory.IsActive || InkLaboratory.IsActive || ParcelStackLaboratory.IsActive;
            if (File.Exists("Logs/power-up-formation.request") && laboratory && manager.gameStatus == GameState.WaitForPopup)
                manager.gameStatus = GameState.Playing;
            if (manager.gameStatus != GameState.Playing) return;
            if (captureAt > 0 && EditorApplication.timeSinceStartup >= captureAt)
            {
                captureAt = 0;
                InkTools.CapturePreview();
                if (File.Exists("Logs/grass-preview.png")) File.Copy("Logs/grass-preview.png", "Logs/power-up-formation.png", true);
            }
            if (reportAt > 0 && EditorApplication.timeSinceStartup >= reportAt)
            {
                reportAt = 0;
                if (rocketTest)
                    File.WriteAllText("Logs/rocket-falling-debris.result", $"Cacos em queda={Object.FindObjectsByType<RoyalAves.Effects.FallingBoardDebris>(FindObjectsSortMode.None).Length}; foguete com rastro={Object.FindObjectsByType<RoyalAves.Effects.RocketBeamMaterial>(FindObjectsSortMode.None).Length}.");
                else File.WriteAllText("Logs/power-up-formation.result", $"Esperado={expected}; criado={RoyalAves.Effects.PowerUpFormation.LastCreatedType}; criações={RoyalAves.Effects.PowerUpFormation.CreatedCount - initialCount}; ícones atraídos={RoyalAves.Effects.PowerUpFormation.LastAttractedCount}; fila de animações={manager.animateItems.Count}.");
                manager.findMatchesStarted = false;
                manager.DragBlocked = false;
                manager.FindMatches();
            }
            const string path = "Logs/power-up-formation.request";
            if (!File.Exists(path) || !(FrankingMachineLaboratory.IsActive || MailCubbyLaboratory.IsActive || InkLaboratory.IsActive || ParcelStackLaboratory.IsActive)) return;
            string mode = File.ReadAllText(path).Trim();
            File.Delete(path);
            rocketTest = mode == "rocket-fire";
            if (rocketTest)
            {
                var source = manager.field.GetSquare(2, 4).Item;
                source.SetType(ItemsTypes.HORIZONTAL_STRIPED, created => created.DestroyItem(true), false);
                captureAt = EditorApplication.timeSinceStartup + 0.3d;
                reportAt = EditorApplication.timeSinceStartup + 0.4d;
                return;
            }
            foreach (var square in manager.field.GetSquares())
                if (square.Item != null && square.Item.currentType == ItemsTypes.NONE)
                    square.Item.colorableComponent.SetColor((square.col + square.row * 2) % 5);
            var points = mode == "square4" ? new[] { new Vector2Int(0,4), new Vector2Int(1,4), new Vector2Int(0,5), new Vector2Int(1,5) }
                : mode == "cross5" ? new[] { new Vector2Int(1,3), new Vector2Int(1,4), new Vector2Int(1,5), new Vector2Int(0,4), new Vector2Int(2,4) }
                : Enumerable.Range(0, mode == "line5" ? 5 : 4).Select(c => new Vector2Int(c,4)).ToArray();
            foreach (var point in points) manager.field.GetSquare(point.x, point.y).Item?.colorableComponent.SetColor(0);
            var combined = new CombineManager().GetCombinedItems(manager.field, true);
            var match = combined.FirstOrDefault(group => group.Any(item => item.NextType != ItemsTypes.NONE));
            if (match == null) { File.WriteAllText("Logs/power-up-formation.result", "Nenhum especial encontrado no fixture."); return; }
            var target = match.First(item => item.NextType != ItemsTypes.NONE);
            destination = target.square; expected = target.NextType;
            initialCount = RoyalAves.Effects.PowerUpFormation.CreatedCount;
            // Hold secondary cascades until the exact match has been observed.
            manager.findMatchesStarted = true;
            manager.DragBlocked = true;
            DestroyingPipeline.THIS.DestroyItems(match, new Delays(), null);
            captureAt = EditorApplication.timeSinceStartup + 0.34d;
            reportAt = EditorApplication.timeSinceStartup + 0.9d;
        }
    }
}

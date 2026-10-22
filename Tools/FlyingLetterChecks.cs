// Managed regression checks for the compiled prototype; no Unity scene/native calls.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

class FlyingLetterChecks
{
    static int Main()
    {
        string engine = @"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed\UnityEngine";
        string build = "Temp/FlyingLetterBuild";
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            string name = new AssemblyName(args.Name).Name + ".dll";
            foreach (string folder in new[] { engine, Path.GetDirectoryName(engine), build })
            {
                string path = Path.Combine(folder, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        var assembly = Assembly.LoadFrom(Path.GetFullPath(build + "/Assembly-CSharp.dll"));
        var letterType = assembly.GetType("RoyalAves.Gameplay.ItemFlyingLetter", true);
        var dataType = assembly.GetType("SweetSugar.Scripts.Level.LevelData", true);
        var fieldType = assembly.GetType("SweetSugar.Scripts.Level.FieldData", true);
        var cellType = assembly.GetType("SweetSugar.Scripts.Level.SquareBlocks", true);
        var itemType = assembly.GetType("SweetSugar.Scripts.Level.ItemForEditor", true);
        var kind = assembly.GetType("SweetSugar.Scripts.Items.ItemsTypes", true);
        var squareKind = assembly.GetType("SweetSugar.Scripts.Blocks.SquareTypes", true);
        var countMethod = letterType.GetMethod("CountLetters");
        var data = FormatterServices.GetUninitializedObject(dataType);
        int checks = 0;
        var route = assembly.GetType("RoyalAves.Gameplay.RouteCentralCell", true);
        var select = route.GetMethod("SelectPotion");
        var selectPiece = route.GetMethod("SelectPotionForPiece");
        string[] pieceNames = { "gift", "envelope", "stamp", "bow", "key", "ink", "quill" };
        for (int i = 0; i < pieceNames.Length; i++)
        {
            int expected = i < 4 ? i : -1;
            int actual = (int)selectPiece.Invoke(null, new object[] { new bool[4], pieceNames[i], false });
            if (actual != expected) throw new Exception("Incorrect matched sprite identity: " + pieceNames[i]);
            checks++;
        }
        var done = new bool[4];
        int[] colors = { 1, 0, 2, 6 };
        for (int i = 0; i < 4; i++)
        {
            if ((int)select.Invoke(null, new object[] { done, colors[i], false }) != i)
                throw new Exception("Wrong potion color mapping.");
            checks++;
        }
        if ((int)select.Invoke(null, new object[] { done, 3, false }) != -1)
            throw new Exception("Wrong color accepted.");
        checks++;
        done[0] = true;
        if ((int)select.Invoke(null, new object[] { done, 1, false }) != -1)
            throw new Exception("Broken potion accepted again.");
        checks++;
        if ((int)select.Invoke(null, new object[] { done, -1, true }) != 1)
            throw new Exception("Special must select first unbroken potion.");
        checks++;
        for (int i = 0; i < 4; i++) done[i] = true;
        if ((int)select.Invoke(null, new object[] { done, -1, true }) != -1)
            throw new Exception("Completed central accepted damage.");
        checks++;
        var adjacent = letterType.GetMethod("IsAdjacentMatch");
        for (int dr = -2; dr <= 2; dr++)
            for (int dc = -2; dc <= 2; dc++)
            {
                bool expected = Math.Abs(dr) + Math.Abs(dc) == 1;
                bool actual = (bool)adjacent.Invoke(null, new object[] { 4, 4, 4 + dr, 4 + dc });
                if (actual != expected) throw new Exception("Incorrect egg adjacency: " + dr + "," + dc);
                checks++;
            }
        Action<object, int> count = (value, expected) =>
        {
            int actual = (int)countMethod.Invoke(null, new[] { value });
            if (actual != expected) throw new Exception("Count: expected " + expected + ", got " + actual);
            checks++;
        };
        count(null, 0); count(data, 0);
        var fields = (IList)Activator.CreateInstance(dataType.GetField("fields").FieldType);
        dataType.GetField("fields").SetValue(data, fields);
        count(data, 0);
        Func<int, bool, int, object> cell = (type, empty, block) =>
        {
            var value = Activator.CreateInstance(cellType);
            var item = Activator.CreateInstance(itemType);
            itemType.GetField("ItemType").SetValue(item, Enum.ToObject(kind, type));
            cellType.GetField("item").SetValue(value, item);
            cellType.GetField("block").SetValue(value, Enum.ToObject(squareKind, block));
            cellType.GetField("startWithoutItem").SetValue(value, empty);
            return value;
        };
        Action<object[]> field = values =>
        {
            var value = Activator.CreateInstance(fieldType);
            var cells = Array.CreateInstance(cellType, values.Length);
            for (int i = 0; i < values.Length; i++) cells.SetValue(values[i], i);
            fieldType.GetField("levelSquares").SetValue(value, cells);
            fields.Add(value);
        };
        field(new[] { cell(9, false, 1), cell(0, false, 1), cell(9, true, 1), cell(9, false, 0), null });
        count(data, 1);
        field(new[] { cell(9, false, 1), cell(9, false, 1) }); count(data, 3);
        var instance = FormatterServices.GetUninitializedObject(letterType);
        if ((bool)letterType.GetProperty("IsBroken").GetValue(instance)) throw new Exception("Fresh break state.");
        checks++;
        letterType.GetField("broken", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, true);
        if ((bool)letterType.GetMethod("TryBreak").Invoke(instance, null)) throw new Exception("Repeated break accepted.");
        checks++;
        if (!(bool)letterType.GetMethod("IsExplodable").Invoke(instance, null)) throw new Exception("Special hits rejected.");
        checks++;
        var baseItem = assembly.GetType("SweetSugar.Scripts.Items.Item", true);
        if ((bool)baseItem.GetMethod("IsPlayerPowerUpType").Invoke(null, new[] { Enum.ToObject(kind, 9) }))
            throw new Exception("Letter must not activate as a power-up.");
        checks++;
        if ((int)Enum.Parse(kind, "FLYING_LETTER") != 9 || (int)Enum.Parse(kind, "TimeBomb") != 8 ||
            (int)Enum.Parse(kind, "INGREDIENT") != 6) throw new Exception("Serialized item IDs changed.");
        checks++;
        if (assembly.GetType("SweetSugar.Scripts.TargetScripts.FlyingLetters", true) == null) throw new Exception("Goal cannot load.");
        checks++;
        if (assembly.GetType("RoyalAves.Gameplay.FlyingLetterDelivery") != null) throw new Exception("Retired HUD flight still exists.");
        checks++;
        if (letterType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic) != null)
            throw new Exception("Retired floating behavior still exists.");
        checks++;
        string result = "PASS: " + checks + " compiled checks: 15 Route Central cases (four sprite identities accepted; yellow/ink/blue quill rejected; four color mappings; wrong color; broken potion; special selection; completed central); 38 Magic Egg checks including 25 adjacency positions, finite authored count, idempotent break, special hits, stable IDs and retired behavior. Native movement/break need Play Mode.";
        File.WriteAllText("Logs/flying-letter-managed-tests.result", result);
        Console.WriteLine(result);
        return 0;
    }
}

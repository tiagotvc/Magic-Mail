// Pure managed checks against the actual compiled gameplay selectors.
using System;
using System.IO;
using System.Reflection;

class RouteCentralHitChecks
{
    static int Main()
    {
        string engine = @"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed\UnityEngine";
        string build = Path.GetFullPath("Temp/bin/Debug");
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args)
        {
            string file = new AssemblyName(args.Name).Name + ".dll";
            foreach (string folder in new[] { engine, Path.GetDirectoryName(engine), build })
                if (File.Exists(Path.Combine(folder,file))) return Assembly.LoadFrom(Path.Combine(folder,file));
            return null;
        };
        var type = Assembly.LoadFrom(Path.Combine(build,"Assembly-CSharp.dll"))
            .GetType("RoyalAves.Gameplay.RouteCentralCell",true);
        var at = type.GetMethod("SelectPotionAtCell");
        int checks = 0;
        Func<bool[],int,int,int> select = (done,col,row) => (int)at.Invoke(null,new object[] {done,col,row});
        for (int mask = 0; mask < 16; mask++)
        {
            var done = new bool[4];
            for (int i = 0; i < 4; i++) done[i] = (mask & (1 << i)) != 0;
            for (int row = -1; row <= 2; row++)
            for (int col = -1; col <= 2; col++)
            {
                int expected = col >= 0 && col <= 1 && row >= 0 && row <= 1 ? row*2+col : -1;
                if (expected >= 0 && done[expected]) expected = -1;
                if (select(done,col,row) != expected) throw new Exception("Wrong cell mapping or off-footprint fallback.");
                checks++;
            }
            // Rocket footprint: top/bottom row, left/right column. Check both
            // traversal orders and repeated callbacks on each affected cell.
            int[][] lines = { new[] {0,1},new[] {2,3},new[] {0,2},new[] {1,3} };
            foreach (var line in lines)
            for (int reverse = 0; reverse < 2; reverse++)
            {
                var state = (bool[])done.Clone();
                foreach (int step in new[] {0,1})
                {
                    int cell = line[reverse == 0 ? step : 1-step];
                    int result = select(state,cell%2,cell/2);
                    if (result != (state[cell] ? -1 : cell)) throw new Exception("Rocket selected another bottle.");
                    if (result >= 0) state[result] = true;
                    if (select(state,cell%2,cell/2) != -1) throw new Exception("Repeated cell callback accepted.");
                    checks += 2;
                }
                for (int i = 0; i < 4; i++)
                {
                    bool expected = done[i] || i == line[0] || i == line[1];
                    if (state[i] != expected) throw new Exception("Rocket damaged a bottle outside its line.");
                    checks++;
                }
            }
        }
        if (select(null,0,0) != -1 || select(new bool[3],0,0) != -1) throw new Exception("Invalid state accepted.");
        checks += 2;
        var match = type.GetMethod("SelectPotionForPiece");
        string[] sprites = { "gift","envelope","stamp","bow","key","ink","quill" };
        for (int i = 0; i < sprites.Length; i++)
        {
            if ((int)match.Invoke(null,new object[] {new bool[4],sprites[i],false}) != (i < 4 ? i : -1))
                throw new Exception("Adjacent match color mapping regressed.");
            checks++;
        }
        if ((int)match.Invoke(null,new object[] {new bool[4],null,true}) != -1)
            throw new Exception("Special without footprint selected a bottle.");
        checks++;
        string resultText = "PASS: " + checks + " compiled selector checks; all 16 broken-state masks, both rows/columns in both orders, repeated hits, missing/outside cells, match identities. Native rocket traversal still requires Play Mode.";
        File.WriteAllText("Logs/route-central-hit-checks.result",resultText);
        Console.WriteLine(resultText);
        return 0;
    }
}

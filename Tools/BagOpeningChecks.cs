using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

class BagOpeningChecks
{
    static int Main()
    {
        string engine=@"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed\UnityEngine";
        string build=Path.GetFullPath("Temp/bin/Debug");
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender,ResolveEventArgs args)
        {
            string file=new AssemblyName(args.Name).Name+".dll";
            foreach (string folder in new[]{engine,Path.GetDirectoryName(engine),build,"Library/ScriptAssemblies"})
                if (File.Exists(Path.Combine(folder,file))) return Assembly.LoadFrom(Path.Combine(folder,file));
            return null;
        };
        var editor=Assembly.LoadFrom(Path.Combine(build,"Assembly-CSharp-Editor.dll"));
        var method=editor.GetType("RoyalAves.EditorTools.BagOpeningLaboratoryTools",true).GetMethod("IsUBorder");
        var expected=new HashSet<string>{"0,0","6,0","0,1","6,1","0,2","6,2","0,3","6,3",
            "0,4","6,4","0,5","6,5","0,6","1,6","2,6","3,6","4,6","5,6","6,6"};
        int count=0,checks=0;
        for (int row=-1;row<=7;row++)
        for (int col=-1;col<=7;col++)
        {
            bool actual=(bool)method.Invoke(null,new object[]{col,row,7,7});
            if (actual!=expected.Contains(col+","+row)) throw new Exception("Incorrect U cell: "+col+","+row);
            if (actual) count++;
            checks++;
        }
        if (count!=19) throw new Exception("U must contain 19 bags.");checks++;
        var runtime=Assembly.LoadFrom(Path.Combine(build,"Assembly-CSharp.dll"));
        if (runtime.GetType("RoyalAves.Gameplay.BagOpeningStation",true)==null
            || runtime.GetType("SweetSugar.Scripts.TargetScripts.BagOpenings",true)==null)
            throw new Exception("Station/temporary goal missing.");
        checks++;
        string result="PASS: "+checks+" compiled checks: explicit 19-cell U footprint, open top, clear interior, out-of-range cells, station and temporary goal. Native gameplay remains to validate.";
        File.WriteAllText("Logs/bag-opening-u-checks.result",result);Console.WriteLine(result);return 0;
    }
}

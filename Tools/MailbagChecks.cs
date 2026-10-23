using System;
using System.IO;
using System.Reflection;

class MailbagChecks
{
    static int Main()
    {
        string engine=@"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Managed\UnityEngine";
        string build=Path.GetFullPath("Temp/bin/Debug");
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender,ResolveEventArgs args)
        {
            string file=new AssemblyName(args.Name).Name+".dll";
            foreach (string folder in new[] {engine,Path.GetDirectoryName(engine),build,"Library/ScriptAssemblies"})
                if (File.Exists(Path.Combine(folder,file))) return Assembly.LoadFrom(Path.Combine(folder,file));
            return null;
        };
        var assembly=Assembly.LoadFrom(Path.Combine(build,"Assembly-CSharp.dll"));
        var type=assembly.GetType("RoyalAves.Gameplay.MailbagDamageState",true);
        var state=Activator.CreateInstance(type);
        var hit=type.GetMethod("TryHit");
        var remaining=type.GetProperty("Remaining");
        int checks=0;
        Action<int> expect = n => {if ((int)remaining.GetValue(state)!=n) throw new Exception("Incorrect resistance.");checks++;};
        Action<float,int,bool> attempt = (time,id,accepted) => {
            if ((bool)hit.Invoke(state,new object[] {time,id}) != accepted) throw new Exception("Incorrect duplicate/cooldown decision.");checks++;};
        expect(2);attempt(0,100,true);expect(1);
        attempt(0,100,false);attempt(.1f,101,false);expect(1);
        attempt(1,100,false);expect(1); // same match cannot take the second resistance later
        attempt(1,101,true);expect(0);attempt(2,102,false);expect(0);
        type.GetMethod("Reset").Invoke(state,null);expect(2);
        attempt(0,-1,true);expect(1);attempt(.1f,-1,false);attempt(.3f,-1,true);expect(0);
        for (int i=0;i<50;i++)
        {
            type.GetMethod("Reset").Invoke(state,null);expect(2);
            attempt(10,i,true);expect(1);attempt(11,i+100,true);expect(0);attempt(12,i+200,false);
        }
        var kinds=assembly.GetType("SweetSugar.Scripts.Items.ItemsTypes",true);
        if (Convert.ToInt32(Enum.Parse(kinds,"MAILBAG"))!=10 || Convert.ToInt32(Enum.Parse(kinds,"FLYING_LETTER"))!=9)
            throw new Exception("Serialized item identities changed.");
        checks++;
        var item=assembly.GetType("SweetSugar.Scripts.Items.Item",true);
        if ((bool)item.GetMethod("IsPlayerPowerUpType").Invoke(null,new[] {Enum.ToObject(kinds,10)}))
            throw new Exception("Mailbag treated as player power-up.");
        checks++;
        if (assembly.GetType("SweetSugar.Scripts.TargetScripts.Mailbags",true)==null) throw new Exception("Goal missing.");
        checks++;
        string result="PASS: "+checks+" compiled mailbag checks: two hits, cooldown, match deduplication, final idempotence, 50 pool resets, item IDs, goal and power-up exclusion. Native gameplay remains to validate.";
        File.WriteAllText("Logs/mailbag-checks.result",result);Console.WriteLine(result);return 0;
    }
}

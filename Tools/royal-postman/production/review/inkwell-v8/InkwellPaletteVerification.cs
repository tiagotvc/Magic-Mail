using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SweetSugar.Scripts.Editor;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level.ItemsPerLevel.Editor;
using UnityEditor;
using UnityEngine;

public class InkwellPaletteVerification : EditorWindow
{
    LevelMakerEditor editor;
    int scenario, frames;
    bool clicked;
    string failure;
    static readonly int[] Widths = {390,640,390,640};
    static readonly BindingFlags Flags = BindingFlags.Instance|BindingFlags.NonPublic;
    const string Key="MagicMail.InkwellPaletteV8";
    static InkwellPaletteVerification active;
    static double readyAt;
    [InitializeOnLoadMethod] static void Init() { readyAt=EditorApplication.timeSinceStartup+8; EditorApplication.update+=Boot; }
    static void Boot()
    {
        if(!SessionState.GetBool(Key,false)||active!=null||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<readyAt)return;
        CreateHost();
    }
    public static void Run()
    {
        SessionState.SetBool(Key,true);
        readyAt=EditorApplication.timeSinceStartup+8;
    }
    static void CreateHost()
    {
        if (!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))
            throw new Exception("Isolated copy only");
        var host=CreateInstance<InkwellPaletteVerification>(); active=host;
        host.editor=CreateInstance<LevelMakerEditor>();
        var itemsField=typeof(LevelMakerEditor).GetField("itemsForEditor",Flags);
        itemsField.SetValue(host.editor,Array.CreateInstance(itemsField.FieldType.GetElementType(),0));
        host.titleContent=new GUIContent("Tinteiro palette verification");
        host.Setup(); host.ShowUtility();
        EditorApplication.update+=host.Tick;
    }
    void Setup()
    {
        position=new Rect(100,100,Widths[scenario],600);
        editor.position=new Rect(0,0,Widths[scenario],600);
        Set("section",scenario/2);
        Set("squareType",SquareTypes.EmptySquare);
        Set("deleteBlock",true); Set("separateBarBrush",true); Set("startWithoutItemBrush",true);
        clicked=false; frames=0;
    }
    void Set(string name,object value) {typeof(LevelMakerEditor).GetField(name,Flags).SetValue(editor,value);}
    object Get(string name) {return typeof(LevelMakerEditor).GetField(name,Flags).GetValue(editor);}
    void OnGUI()
    {
        if(editor==null)return;
        try { typeof(LevelMakerEditor).GetMethod(scenario<2?"GUIBlocks":"GUIItems",Flags).Invoke(editor,null); }
        catch(Exception ex){failure=ex.ToString();}
    }
    void Tick()
    {
        try
        {
            if(failure!=null)throw new Exception(failure);
            Repaint();
            if(++frames<15)return;
            var rect=editor.InkwellBrushAccessRect;
            if(rect.width<100||rect.xMin<0||rect.xMax>position.width||rect.yMin<0||rect.yMax>position.height)
                throw new Exception("Invisible shortcut: "+rect+" / "+position);
            if(!clicked)
            {
                SendEvent(new Event {type=EventType.MouseDown,button=0,mousePosition=rect.center});
                SendEvent(new Event {type=EventType.MouseUp,button=0,mousePosition=rect.center});
                clicked=true;return;
            }
            if((int)Get("section")!=0 || (SquareTypes)Get("squareType")!=SquareTypes.Inkwell ||
                (bool)Get("deleteBlock") || (bool)Get("separateBarBrush") || (bool)Get("startWithoutItemBrush"))
                throw new Exception("Click did not select a clean Inkwell brush");
            if(++scenario<Widths.Length){Setup();return;}
            Finish("PASS: rendered actual Blocks and Items GUI at 390/640 px; visible labeled shortcut; IMGUI mouse down/up selected Inkwell and reset conflicting brushes in all four cases.",0);
        }
        catch(Exception ex){Finish("FAIL: "+ex,1);}
    }
    void Finish(string result,int code)
    {
        SessionState.SetBool(Key,false);
        EditorApplication.update-=Tick;
        File.WriteAllText("Logs/inkwell-palette-verification.result",result);
        Debug.Log(result);
        if(editor!=null)DestroyImmediate(editor);
        Close();EditorApplication.Exit(code);
    }
}

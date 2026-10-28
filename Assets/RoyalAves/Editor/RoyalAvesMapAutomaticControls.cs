using UnityEditor;
using UnityEngine;
using RoyalAves.Meta.WorldMap;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesMapAutomaticControls
    {
        static bool open;
        public static void Recompose(WorldMapIslands asset)
        {
            Undo.RecordObject(asset,"Recompose automatic islands");
            while(asset.islands.Count<Mathf.CeilToInt(asset.plannedLevels/(float)asset.levelsPerIsland))asset.Append();
            for(int i=1;i<asset.ActiveIslandCount;i++)WorldMapIslandRecipe.Configure(asset,i);
            foreach(var name in new[]{WorldMapLayout.ResourcePath,WorldMapLayout.SimpleResourcePath,WorldMapMission100.LayoutResource})
            {
                var layout=Resources.Load<WorldMapLayout>(name);if(layout==null)continue;
                Undo.RecordObject(layout,"Reset automatic island overrides");
                layout.chapters.RemoveAll(c=>c.index>0&&c.index<asset.ActiveIslandCount);
                layout.hiddenExtras.RemoveAll(id=>id!=null&&id.StartsWith("river-")&&int.TryParse(id.Split('-')[1],out var level)&&level>asset.levelsPerIsland&&level<=asset.plannedLevels);
                EditorUtility.SetDirty(layout);
            }
            EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
        }
        public static bool Draw(WorldMapIslands asset)
        {
            open=EditorGUILayout.Foldout(open,"Automatic island sequence",true);if(!open)return false;
            EditorGUILayout.HelpBox("Island 1 is preserved. Five normal recipes repeat with art variations. After every two normal islands, one of two Christmas recipes appears. Saving a manual layout creates an override; recompose resets overrides from island 2 onward.",MessageType.Info);
            var data=new SerializedObject(asset);data.Update();EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(data.FindProperty("automaticSequence"));
            EditorGUILayout.PropertyField(data.FindProperty("plannedLevels"));
            EditorGUILayout.PropertyField(data.FindProperty("recipes"),new GUIContent("Shared island recipes"),true);
            bool changed=EditorGUI.EndChangeCheck();if(changed){data.ApplyModifiedProperties();EditorUtility.SetDirty(asset);}
            if(GUILayout.Button("Recompose islands 2+ and save (keep island 1)")){Recompose(asset);return true;}
            return changed;
        }
    }
}

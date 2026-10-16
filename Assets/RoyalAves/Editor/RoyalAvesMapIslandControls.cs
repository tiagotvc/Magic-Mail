using System;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class RoyalAvesMapIslandControls
    {
        public const string AssetPath="Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset";
        int selected;
        bool expanded=true;
        static float Focus(WorldMapIslands asset,int index,WorldMapView world)
        {var route=new WorldMapRoute(asset,WorldMapLevelCatalog.LastLevel);return route.LevelAt(route.Pos(asset.FirstLevel(index))-world.CameraModel.Kit.focusAhead);}
        public static WorldMapIslands GetOrCreate()
        {
            var asset=AssetDatabase.LoadAssetAtPath<WorldMapIslands>(AssetPath);
            if(asset!=null)return asset;
            asset=ScriptableObject.CreateInstance<WorldMapIslands>();
            var count=Mathf.Max(10,Mathf.CeilToInt(WorldMapLevelCatalog.LastLevel/20f));
            for(var i=0;i<count;i++)asset.Append();
            AssetDatabase.CreateAsset(asset,AssetPath);AssetDatabase.SaveAssets();return asset;
        }

        public bool Draw(ref float cursor,WorldMapView world,int authoredCount)
        {
            var asset=GetOrCreate();
            if(asset.islands.Count==0){Undo.RecordObject(asset,"Criar primeira ilha");asset.Append();EditorUtility.SetDirty(asset);}
            if(world!=null)selected=world.Route.IslandIndexAt(world.Route.MetersAt(cursor)+world.CameraModel.Kit.focusAhead);
            EditorGUI.BeginChangeCheck();
            var curvature=EditorGUILayout.Slider("Curvatura das laterais",asset.lateralCurvature,.6f,2.2f);
            if(EditorGUI.EndChangeCheck()){Undo.RecordObject(asset,"Ajustar curvatura lateral");asset.lateralCurvature=curvature;EditorUtility.SetDirty(asset);return true;}
            selected=Mathf.Clamp(selected,0,asset.islands.Count-1);
            var labels=new string[asset.islands.Count];
            for(var i=0;i<labels.Length;i++)labels[i]=$"{i+1}. {asset.islands[i].title} — {asset.FirstLevel(i)}–{asset.FirstLevel(i+1)-1}";
            using(new EditorGUILayout.HorizontalScope())
            {
                var choice=EditorGUILayout.Popup("Ilha",selected,labels);
                if(choice!=selected){selected=choice;cursor=Focus(asset,selected,world);}
                if(GUILayout.Button("+ Nova ilha",GUILayout.Width(105)))
                {
                    Undo.RecordObject(asset,"Adicionar ilha no fim do mapa");
                    asset.Append();selected=asset.islands.Count-1;cursor=Focus(asset,selected,world);
                    EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();return true;
                }
            }
            EditorGUILayout.LabelField($"{authoredCount} fases criadas • {asset.islands.Count} ilhas • {asset.Capacity} posições planejadas",EditorStyles.miniLabel);
            expanded=EditorGUILayout.Foldout(expanded,"Chão, fundo e clima desta ilha",true);
            if(!expanded)return false;
            var data=new SerializedObject(asset);data.Update();
            var item=data.FindProperty("islands").GetArrayElementAtIndex(selected);
            EditorGUI.BeginChangeCheck();
            void Field(string name,string label)=>EditorGUILayout.PropertyField(item.FindPropertyRelative(name),new GUIContent(label));
            Field("title","Nome da ilha");
            EditorGUILayout.PropertyField(data.FindProperty("levelsPerIsland"),new GUIContent("Fases por ilha (todas)"));
            if(asset.unifiedBase)
            {
                var floor=item.FindPropertyRelative("groundStyle");
                floor.intValue=EditorGUILayout.Popup("Chão desta ilha",floor.intValue,new[]{"Verde","Floresta lilás","Vulcânico","Lagoa turquesa","Areia dourada","Gelo azul","Jardim rosado","Cristal violeta"});
                EditorGUILayout.HelpBox("O chão muda por ilha. Os objetos, o background e o clima são editados separadamente.",MessageType.None);
                var palette=item.FindPropertyRelative("palette");
                if(floor.intValue==0)palette.intValue=EditorGUILayout.Popup("Cor da trilha",palette.intValue==1?1:0,new[]{"Areia dourada","Verde menta"});
            }
            else{Field("terrain","Tema do chão");Field("palette","Paleta do chão (0–9)");}
            Field("customColors","Personalizar cores");
            if(item.FindPropertyRelative("customColors").boolValue){Field("ground","Chão lateral");Field("path","Trilha");Field("water","Água / lava");}
            Field("customPathColor","Cor independente da estrada");
            if(item.FindPropertyRelative("customPathColor").boolValue&&!item.FindPropertyRelative("customColors").boolValue)Field("path","Cor da estrada");
            Field("groundMaterials","Textura do chão (opcional)");
            EditorGUILayout.Space(5);EditorGUILayout.LabelField("Passagem para a próxima ilha",EditorStyles.boldLabel);
            Field("channelWidth","Largura da água");Field("channelDepth","Profundidade do rebaixo");
            var bank=item.FindPropertyRelative("bankShape");bank.intValue=EditorGUILayout.Popup("Formato das margens",bank.intValue,new[]{"Orgânico suave","Recortado angular","Ondas e enseadas"});
            var crossing=item.FindPropertyRelative("crossing");crossing.intValue=EditorGUILayout.Popup("Travessia",crossing.intValue,new[]{"Ponte de troncos","Ponte de tábuas","Pedras de passagem","Água livre"});
            var motion=item.FindPropertyRelative("waterMotion");motion.intValue=EditorGUILayout.Popup("Animação da água",motion.intValue,new[]{"Sprites em V","Reflexos suaves","Água calma"});
            Field("channelRocks","Pedras decorativas na água");Field("customChannelColor","Personalizar cor da água");
            if(item.FindPropertyRelative("customChannelColor").boolValue)Field("channelColor","Cor da água");
            EditorGUILayout.Space(5);
            Field("background","Background separado");
            if(item.FindPropertyRelative("background").objectReferenceValue==null){Field("skyTop","Céu superior");Field("skyBottom","Céu no horizonte");}
            Field("cloudSmall","Nuvem pequena");Field("cloudWide","Nuvem larga");
            Field("meteors","Meteoros no horizonte");Field("weather","Clima");Field("weatherIntensity","Intensidade do clima");
            EditorGUILayout.PropertyField(data.FindProperty("postProcessing"),new GUIContent("Luz e pós-processamento"));
            if(data.FindProperty("postProcessing").boolValue){EditorGUILayout.PropertyField(data.FindProperty("bloom"),new GUIContent("Brilho suave"));EditorGUILayout.PropertyField(data.FindProperty("vibrance"),new GUIContent("Vivacidade"));}
            var changed=EditorGUI.EndChangeCheck();
            if(changed)
            {
                data.FindProperty("levelsPerIsland").intValue=Mathf.Max(20,data.FindProperty("levelsPerIsland").intValue);
                data.ApplyModifiedProperties();EditorUtility.SetDirty(asset);
            }
            if(GUILayout.Button("Salvar ilhas e ambiente")){data.ApplyModifiedProperties();AssetDatabase.SaveAssets();}
            if(WorldMapLevelCatalog.LastLevel>asset.Capacity)
                EditorGUILayout.HelpBox("Há fases além das posições planejadas. Elas usam o ambiente da última ilha até você acrescentar ilhas.",MessageType.Info);
            EditorGUILayout.HelpBox("Posições vazias aparecem para edição do cenário. O jogo só mostra botões de fases criadas. O tamanho é igual para todas as ilhas. Alterar esse tamanho desloca as seguintes; objetos manuais mantêm sua fase salva.",MessageType.None);
            return changed;
        }
    }
}

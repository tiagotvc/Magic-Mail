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
        bool expanded=true,riverArtOpen;
        static float Focus(WorldMapIslands asset,int index,WorldMapView world)
        {var route=new WorldMapRoute(asset,WorldMapLevelCatalog.LastLevel);return route.LevelAt(route.Pos(asset.FirstLevel(index))-world.CameraModel.Kit.focusAhead);}
        public static WorldMapIslands GetOrCreate()
        {
            var asset=AssetDatabase.LoadAssetAtPath<WorldMapIslands>(AssetPath);
            if(asset!=null)return asset;
            asset=ScriptableObject.CreateInstance<WorldMapIslands>();
            var count=Mathf.Max(10,Mathf.CeilToInt(WorldMapLevelCatalog.LastLevel/15f));
            for(var i=0;i<count;i++)asset.Append();
            AssetDatabase.CreateAsset(asset,AssetPath);AssetDatabase.SaveAssets();return asset;
        }

        public bool Draw(ref float cursor,WorldMapView world,int authoredCount)
        {
            var asset=GetOrCreate();
            if(asset.islands.Count==0){Undo.RecordObject(asset,"Criar primeira ilha");asset.Append();EditorUtility.SetDirty(asset);}
            if(RoyalAvesMapAutomaticControls.Draw(asset))return true;
            if(world!=null)selected=world.Route.IslandIndexAt(world.Route.MetersAt(cursor)+world.CameraModel.Kit.focusAhead);
            var curveKit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
            if(curveKit!=null)
            {
                EditorGUI.BeginChangeCheck();
                var roll=EditorGUILayout.Slider("Curvatura do chão (para baixo)",curveKit.GroundCurveStrength,.8f,1.4f);
                if(EditorGUI.EndChangeCheck()){Undo.RecordObject(curveKit,"Ajustar curvatura do chão");curveKit.SetGroundCurveStrength(roll);EditorUtility.SetDirty(curveKit);return true;}
            }
            EditorGUI.BeginChangeCheck();
            var christmasSky=EditorGUILayout.Toggle(asset.automaticSequence?"Christmas sky (authored island)":"Christmas sky (all islands)",asset.christmasSky);
            var curvature=EditorGUILayout.Slider("Arco horizontal do chão",asset.lateralCurvature,.6f,2.2f);
            var sway=EditorGUILayout.Slider("Curvas da trilha (esq./dir.)",asset.pathSway,.5f,1.8f);
            var frequency=EditorGUILayout.Slider("Frequencia das curvas",asset.pathFrequency,.75f,1.4f);
            if(EditorGUI.EndChangeCheck()){Undo.RecordObject(asset,"Ajustar curvas do mapa");asset.lateralCurvature=curvature;asset.pathSway=sway;asset.pathFrequency=frequency;asset.christmasSky=christmasSky;EditorUtility.SetDirty(asset);return true;}
            if(asset.christmasSky)
            {
                var skyData=new SerializedObject(asset);skyData.Update();EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(skyData.FindProperty("staticSky"),new GUIContent("Static sky (all islands)"));
                EditorGUILayout.PropertyField(skyData.FindProperty("christmasBackdrop"),new GUIContent("Christmas backdrop"));
                EditorGUILayout.PropertyField(skyData.FindProperty("christmasVillain"),new GUIContent("Villain silhouette"));
                EditorGUILayout.PropertyField(skyData.FindProperty("villainPosition"),new GUIContent("Silhouette position"));
                EditorGUILayout.PropertyField(skyData.FindProperty("villainSize"),new GUIContent("Silhouette size"));
                EditorGUILayout.PropertyField(skyData.FindProperty("villainOpacity"),new GUIContent("Silhouette opacity"));
                EditorGUILayout.PropertyField(skyData.FindProperty("christmasVillainCharacter"),new GUIContent("Full-color villain sprite"));
                EditorGUILayout.PropertyField(skyData.FindProperty("villainMapSize"),new GUIContent("Villain size on islands"));
                if(!asset.staticSky){EditorGUILayout.PropertyField(skyData.FindProperty("villainHop"),new GUIContent("Jump height"));EditorGUILayout.PropertyField(skyData.FindProperty("villainSpeed"),new GUIContent("Jump speed"));}
                if(EditorGUI.EndChangeCheck()){skyData.ApplyModifiedProperties();EditorUtility.SetDirty(asset);return true;}
            }
            riverArtOpen=EditorGUILayout.Foldout(riverArtOpen,"River sprites (all islands)",true);
            if(riverArtOpen)
            {
                var artData=new SerializedObject(asset);artData.Update();EditorGUI.BeginChangeCheck();
                foreach(var field in new[]{"stoneLeft","stoneRight","bankPlant","inkFall","waterFall","lavaFall","lavaStoneLeft","lavaStoneRight","lavaSurface","bridgeDeck"})
                    EditorGUILayout.PropertyField(artData.FindProperty(field));
                if(EditorGUI.EndChangeCheck()){artData.ApplyModifiedProperties();EditorUtility.SetDirty(asset);return true;}
            }
            selected=Mathf.Clamp(selected,0,asset.ActiveIslandCount-1);
            var labels=new string[asset.ActiveIslandCount];
            for(var i=0;i<labels.Length;i++)labels[i]=$"{i+1}. {asset.islands[i].title} — {asset.FirstLevel(i)}–{Mathf.Min(asset.Capacity,asset.FirstLevel(i+1)-1)}";
            using(new EditorGUILayout.HorizontalScope())
            {
                var choice=EditorGUILayout.Popup("Ilha",selected,labels);
                if(choice!=selected){selected=choice;cursor=Focus(asset,selected,world);}
                if(GUILayout.Button("+ Nova ilha",GUILayout.Width(105)))
                {
                    Undo.RecordObject(asset,"Adicionar ilha no fim do mapa");
                    if(asset.automaticSequence){selected=asset.ActiveIslandCount;asset.plannedLevels=(selected+1)*asset.levelsPerIsland;if(selected>=asset.islands.Count)asset.Append();WorldMapIslandRecipe.Configure(asset,selected);}
                    else{asset.Append();selected=asset.islands.Count-1;}
                    cursor=Focus(asset,selected,world);
                    EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();return true;
                }
            }
            EditorGUILayout.LabelField($"{authoredCount} fases criadas • {asset.ActiveIslandCount} ilhas • {asset.Capacity} posições planejadas",EditorStyles.miniLabel);
            if(asset.automaticSequence&&selected==0){EditorGUILayout.HelpBox("Island 1: authored layout preserved. Automatic generation starts on island 2.",MessageType.Info);return false;}
            expanded=EditorGUILayout.Foldout(expanded,"Chão, fundo e clima desta ilha",true);
            if(!expanded)return false;
            var data=new SerializedObject(asset);data.Update();
            var item=data.FindProperty("islands").GetArrayElementAtIndex(selected);
            EditorGUI.BeginChangeCheck();
            void Field(string name,string label)=>EditorGUILayout.PropertyField(item.FindPropertyRelative(name),new GUIContent(label),true);
            Field("title","Nome da ilha");
            if(asset.automaticSequence){Field("recipeIndex","Recipe (0–4 normal, 5–6 Christmas)");Field("sceneryVariant","Art variation");}
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
            Field("liquid","Liquid theme");Field("waterGloss","Gloss intensity");
            if(item.FindPropertyRelative("liquid").enumValueIndex==2){Field("musicNotes","Rising notes");Field("noteSpeed","Note rise speed");}
            Field("lakeLevels","Levels on lake stones");Field("stoneSize","Stone size");
            Field("stones","Individual stones (offset from last island level)");Field("waterPlants","Aquatic plants");Field("plants","Individual aquatic plants");
            Field("sideWaterfall","Static side waterfall");
            if(item.FindPropertyRelative("sideWaterfall").boolValue)
            {
                Field("waterfallOnRight","Waterfall on right");Field("waterfallProgress","Waterfall position along river");
                Field("waterfallLateral","Waterfall lateral position");Field("waterfallSize","Waterfall size");Field("waterfallSprite","Waterfall sprite override");
            }
            EditorGUILayout.PropertyField(data.FindProperty("specialLevelEvery"),new GUIContent("Special level every"));
            Field("channelWidth","Largura da água");Field("channelDepth","Profundidade do rebaixo");
            if(item.FindPropertyRelative("crossing").enumValueIndex<2){Field("bridgeWidth","Bridge width");Field("bridgeBankOverlap","Bridge bank overlap");}
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
                data.FindProperty("levelsPerIsland").intValue=Mathf.Max(15,data.FindProperty("levelsPerIsland").intValue);
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

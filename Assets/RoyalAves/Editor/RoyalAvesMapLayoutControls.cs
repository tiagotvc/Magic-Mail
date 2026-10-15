using System;
using System.Linq;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Controls for the real map preview, persisted as a Resource consumed by WorldMapScenery.
    public sealed class RoyalAvesMapLayoutControls
    {
        const string AssetPath="Assets/RoyalAves/Resources/RoyalAvesWorldMapSimpleLayout.asset";
        bool editing;
        bool snowOpen=true,lightsOpen,iceOpen;
        public bool Editing=>editing;
        WorldMapLibraryItem addItem;
        WorldMapAssetCategory addCategory;
        WorldMapLibraryItem[] libraryItems;
        string librarySearch="";
        WorldMapLayout layout;
        WorldMapLayout.Entry selected;
        WorldMapLayout.Chapter owner;
        int dragControl;
        Vector2 dragOffset;
        static float ClampPhase(float phase,WorldMapLayout.Chapter chapter) =>
            Mathf.Clamp(phase,chapter.index*20-2,chapter.index*20+22);

        public static WorldMapLayout GetOrCreate()
        {
            var asset=AssetDatabase.LoadAssetAtPath<WorldMapLayout>(AssetPath);
            if(asset!=null)return asset;
            asset=ScriptableObject.CreateInstance<WorldMapLayout>();
            AssetDatabase.CreateAsset(asset,AssetPath);AssetDatabase.SaveAssets();return asset;
        }
        public void ClearSelection(){selected=null;owner=null;}
        void Changed(WorldMapView world)
        {
            EditorUtility.SetDirty(layout);world.ReloadScenery();
        }
        WorldMapLayout.Chapter EnsureChapter(int chapter,WorldMapView world)
        {
            layout=layout??GetOrCreate();
            var data=layout.Find(chapter);
            if(data?.manual==true)
            {
                var extras=world.Props.Where(p=>WorldMapLayout.IsExtra(p.id)&&world.Route.ChapterAtMeters(p.s)==chapter&&!layout.ExtraClaimed(p.id)).ToArray();
                if(extras.Length>0){Undo.RecordObject(layout,"Editar gêiseres do mapa");data.entries.AddRange(extras.Select(p=>WorldMapLayout.Entry.From(p,world.Route)));Changed(world);}
                return data;
            }
            Undo.RecordObject(layout,"Montar trecho do mapa");
            if(data==null){data=new WorldMapLayout.Chapter{index=chapter};layout.chapters.Add(data);}
            data.entries=world.Props.Where(p=>world.Route.ChapterAtMeters(p.s)==chapter)
                .Select(p=>WorldMapLayout.Entry.From(p,world.Route)).ToList();
            data.manual=true;Changed(world);return data;
        }

        public void Toolbar(WorldMapView world,ref float cursor)
        {
            if(world==null)return;
            using(new EditorGUILayout.HorizontalScope())
            {
                var next=GUILayout.Toggle(editing,"Editar objetos",EditorStyles.miniButton);
                if(next!=editing){editing=next;ClearSelection();if(editing)EnsureChapter(world.Route.ChapterAtMeters(world.Route.MetersAt(cursor)),world);world.ReloadScenery();}
                if(GUILayout.Button("Salvar disposição",EditorStyles.miniButton)&&layout!=null)
                {EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();}
                if(GUILayout.Button("Ver dados",EditorStyles.miniButton))Selection.activeObject=GetOrCreate();
            }
            var addLabels=new[]{"Adicionar objeto","Adicionar árvore","Adicionar props","Adicionar efeitos","Adicionar personagens"};
            for(int row=0;row<3;row++)using(new EditorGUILayout.HorizontalScope())
                for(int col=0;col<2&&row*2+col<5;col++)
                {
                    int category=row*2+col;
                    if(GUILayout.Button(addLabels[category],GUILayout.Height(25)))
                    {
                        editing=true;ClearSelection();addItem=null;librarySearch="";addCategory=(WorldMapAssetCategory)category;
                        libraryItems=RoyalAvesMapLibrary.Items(addCategory);
                        EnsureChapter(world.Route.ChapterAtMeters(world.Route.MetersAt(cursor)),world);world.ReloadScenery();
                    }
                }
            if(!editing)return;
            EditorGUILayout.LabelField("Clique e arraste o objeto no mapa. A disposição salva é usada no jogo.",EditorStyles.wordWrappedMiniLabel);
            var chapter=EnsureChapter(world.Route.ChapterAtMeters(world.Route.MetersAt(cursor)),world);
            var entries=chapter.entries.Where(x=>!WorldMapScenery.IsDog(x.sprite,x.kind,world.CameraModel.Kit)&&x.kind!=PropKind.FloatingMail).ToArray();
            var choices=new string[entries.Length+1];choices[0]="Escolha uma peça (inclusive escondida)";
            var current=0;
            for(var i=0;i<entries.Length;i++)
            {
                var entry=entries[i];
                choices[i+1]=$"{i+1}. {(entry.sprite!=null?entry.sprite.name:"Sem sprite")} | fase {entry.level:0.0} | {(entry.lateral<0?"esquerda":"direita")}";
                if(entry==selected)current=i+1;
            }
            var chosen=EditorGUILayout.Popup("Selecionar objeto",current,choices);
            if(chosen!=current){if(chosen>0){owner=chapter;selected=entries[chosen-1];}else ClearSelection();}
            if(libraryItems==null)libraryItems=RoyalAvesMapLibrary.Items(addCategory);
            librarySearch=EditorGUILayout.TextField("Buscar na categoria",librarySearch);
            var visibleItems=libraryItems.Where(i=>string.IsNullOrWhiteSpace(librarySearch)||i.Label.IndexOf(librarySearch,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            if(addItem!=null&&!visibleItems.Contains(addItem))addItem=null;
            var itemNames=new[]{"Escolha da categoria"}.Concat(visibleItems.Select(i=>i.Label)).ToArray();
            var itemIndex=Array.IndexOf(visibleItems,addItem)+1;
            var selectedItem=EditorGUILayout.Popup(RoyalAvesMapLibrary.Folders[(int)addCategory],itemIndex,itemNames);
            if(selectedItem!=itemIndex)addItem=selectedItem>0?visibleItems[selectedItem-1]:null;
            DrawLibraryTiles(visibleItems);
            using(new EditorGUI.DisabledScope(true))EditorGUILayout.ObjectField("Sprite",addItem?.sprite,typeof(Sprite),false);
            if(GUILayout.Button("Atualizar biblioteca")){RoyalAvesMapLibrary.Ensure(world.CameraModel.Kit);libraryItems=null;}
            using(new EditorGUILayout.HorizontalScope())
            {
                var kit=world.CameraModel.Kit;
                using(new EditorGUI.DisabledScope(addItem==null))
                if(GUILayout.Button("Colocar no mapa",GUILayout.Width(115)))
                {
                    owner=EnsureChapter(world.Route.ChapterAtMeters(world.Route.MetersAt(cursor)),world);
                    Undo.RecordObject(layout,"Adicionar objeto do mapa");
                    selected=new WorldMapLayout.Entry{id=Guid.NewGuid().ToString("N"),sprite=addItem.sprite,keepSprite=true,customSize=true,
                        level=Mathf.Clamp(cursor+1,owner.index*20+1,owner.index*20+20),lateral=1.85f,height=addItem.size,kind=addItem.kind,shadow=addCategory!=WorldMapAssetCategory.Effects&&addItem.kind!=PropKind.GroundDetail,
                        groundPinned=addCategory!=WorldMapAssetCategory.Effects&&addItem.kind!=PropKind.GroundDetail&&addItem.kind!=PropKind.WaterRock};
                    if(addItem.kind==PropKind.WaterRipple&&world.Route.Gaps.Count>0)
                    {
                        var anchor=world.Route.MetersAt(cursor);var gap=world.Route.Gaps.OrderBy(g=>Mathf.Abs((g.start+g.end)*.5f-anchor)).First();
                        selected.level=world.Route.LevelAt((gap.start+gap.end)*.5f);cursor=selected.level;
                    }
                    owner.entries.Add(selected);Changed(world);
                }
            }
            if(selected==null)return;
            EditorGUI.BeginChangeCheck();
            var sprite=(Sprite)EditorGUILayout.ObjectField("Objeto",selected.EffectiveSprite(world.CameraModel.Kit),typeof(Sprite),false);
            var phase=EditorGUILayout.FloatField("Fase / posição",selected.level);
            var side=EditorGUILayout.FloatField("Lateral (− esquerda / + direita)",selected.lateral);
            var size=EditorGUILayout.Slider("Tamanho",selected.EffectiveHeight(world.CameraModel.Kit),.12f,3);
            var kind=(PropKind)EditorGUILayout.EnumPopup("Tipo / animação",selected.kind);
            var rotation=selected.kind==PropKind.LightShaft?EditorGUILayout.Slider("Rotação do raio (°)",selected.rotation,-80,80):selected.rotation;
            var intensity=selected.kind==PropKind.LightShaft?EditorGUILayout.Slider("Intensidade da luz",selected.intensity,.05f,1):selected.intensity;
            var thickness=selected.kind==PropKind.LightShaft?EditorGUILayout.Slider("Espessura do raio",selected.thickness,.2f,4):selected.thickness;
            var flipX=EditorGUILayout.Toggle("Virar horizontalmente",selected.flipX);
            var groundPinned=EditorGUILayout.Toggle("Cravar no chão",selected.groundPinned);
            var groundDepth=selected.groundDepth;
            if(groundPinned&&!selected.groundPinned&&groundDepth<=0)groundDepth=.06f;
            if(groundPinned)groundDepth=EditorGUILayout.Slider("Profundidade (%)",groundDepth*100,0,30)/100;
            var useTheme=EditorGUILayout.Toggle("Adicionar tema",selected.theme!=WorldMapObjectTheme.None);
            var theme=useTheme?(selected.theme==WorldMapObjectTheme.None?WorldMapObjectTheme.Season:selected.theme):WorldMapObjectTheme.None;
            if(useTheme)
                theme=EditorGUILayout.Popup("Tema do objeto",theme==WorldMapObjectTheme.Ice?1:0,new[]{"Da temporada (Natal: gelo)","Gelo"})==1?WorldMapObjectTheme.Ice:WorldMapObjectTheme.Season;
            var snowEffect=selected.snowEffect;var lightsEffect=selected.lightsEffect;var iceEffect=selected.iceEffect;
            EditorGUILayout.Space();EditorGUILayout.LabelField("Encaixe dos efeitos neste objeto",EditorStyles.boldLabel);
            using(new EditorGUI.DisabledScope(!useTheme))
            {
                var snowAvailable=kind==PropKind.Tree||kind==PropKind.Small||kind==PropKind.Mailbox||kind==PropKind.Lamp||kind==PropKind.Landmark;
                snowEffect=DrawEffectPose("Neve",snowEffect,ref snowOpen,snowAvailable);
                lightsEffect=DrawEffectPose("Luzes de Natal",lightsEffect,ref lightsOpen,kind==PropKind.Tree);
                iceEffect=DrawEffectPose("Gelo",iceEffect,ref iceOpen,kind==PropKind.Tree);
            }
            EditorGUILayout.HelpBox("Ajustes relativos à peça. Use o preview de Natal ou o tema Gelo para visualizar; Salvar disposição grava os encaixes.",MessageType.None);
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layout,"Ajustar objeto do mapa");
                selected.sprite=sprite;selected.keepSprite=true;selected.level=ClampPhase(phase,owner);
                selected.lateral=side;selected.height=size;selected.customSize=true;selected.kind=kind;selected.rotation=rotation;selected.intensity=intensity;selected.thickness=thickness;selected.flipX=flipX;selected.theme=theme;
                selected.groundPinned=groundPinned;selected.groundDepth=groundDepth;
                selected.snowEffect=snowEffect;selected.lightsEffect=lightsEffect;selected.iceEffect=iceEffect;Changed(world);
            }
            if(GUILayout.Button("Usar tamanho padrão da categoria"))
            {Undo.RecordObject(layout,"Padronizar tamanho do objeto");selected.customSize=false;Changed(world);}
            if(GUILayout.Button("Remover objeto selecionado"))
            {Undo.RecordObject(layout,"Remover objeto do mapa");if(!string.IsNullOrEmpty(selected.sourceId))layout.hiddenExtras.Add(selected.sourceId);owner.entries.Remove(selected);ClearSelection();Changed(world);}
        }

        static WorldMapEffectPose DrawEffectPose(string label,WorldMapEffectPose pose,ref bool open,bool automatic)
        {
            var changed=GUI.changed;open=EditorGUILayout.Foldout(open,label,true);GUI.changed=changed;
            if(!open)return pose;
            using(new EditorGUI.IndentLevelScope())
            {
                var visible=pose.Visible(automatic);var next=EditorGUILayout.Toggle("Ativo",visible);
                if(next!=visible)pose.visibility=next==automatic?WorldMapEffectVisibility.Automatic:next?WorldMapEffectVisibility.Show:WorldMapEffectVisibility.Hide;
                pose.offset=EditorGUILayout.Vector2Field("Posição (% da altura)",pose.offset*100)/100;
                pose.rotation=EditorGUILayout.Slider("Rotação (°)",pose.rotation,-180,180);
                var size=EditorGUILayout.Vector2Field("Escala X / Y",pose.EffectiveScale);
                pose.scale=new Vector2(Mathf.Clamp(size.x,.05f,5),Mathf.Clamp(size.y,.05f,5));
                if(GUILayout.Button("Restaurar "+label.ToLowerInvariant())){pose=default;GUI.changed=true;}
            }
            return pose;
        }

        void DrawLibraryTiles(WorldMapLibraryItem[] items)
        {
            if(items.Length==0){EditorGUILayout.HelpBox("Nenhum item nesta categoria com esse nome.",MessageType.None);return;}
            var caption=new GUIStyle(EditorStyles.centeredGreyMiniLabel){wordWrap=true,alignment=TextAnchor.MiddleCenter};
            for(int row=0;row<(items.Length+2)/3;row++)using(new EditorGUILayout.HorizontalScope())
                for(int col=0;col<3;col++)
                {
                    int index=row*3+col;
                    var rect=GUILayoutUtility.GetRect(68,100,GUILayout.MinWidth(68),GUILayout.ExpandWidth(true));
                    if(index>=items.Length)continue;
                    var item=items[index];var oldColor=GUI.backgroundColor;
                    if(item==addItem)GUI.backgroundColor=new Color(.68f,.88f,1);
                    if(GUI.Button(rect,new GUIContent("",item.Label)))addItem=item;
                    GUI.backgroundColor=oldColor;
                    var sprite=item.sprite;var region=sprite.rect;var texture=sprite.texture;
                    var preview=new Rect(rect.x+6,rect.y+5,rect.width-12,65);
                    var size=preview.size;var aspect=region.width/Mathf.Max(1,region.height);
                    if(aspect>size.x/size.y)size.y=size.x/aspect;else size.x=size.y*aspect;
                    var image=new Rect(preview.center-size*.5f,size);
                    GUI.DrawTextureWithTexCoords(image,texture,new Rect(region.x/texture.width,region.y/texture.height,region.width/texture.width,region.height/texture.height));
                    GUI.Label(new Rect(rect.x+4,rect.y+72,rect.width-8,24),item.Label,caption);
                }
        }

        public void Overlay(Rect imageRect,WorldMapView world,float cursor)
        {
            if(!editing||world==null)return;
            var aspect=world.Cam.aspect;
            var e=Event.current;
            var uv=new Vector2((e.mousePosition.x-imageRect.x)/imageRect.width,1-(e.mousePosition.y-imageRect.y)/imageRect.height);
            var control=GUIUtility.GetControlID(FocusType.Passive);
            if(e.type==EventType.MouseDown&&e.button==0&&imageRect.Contains(e.mousePosition))
            {
                var index=-1;var depth=float.PositiveInfinity;
                for(var i=0;i<world.Props.Count;i++)
                    if(world.ProjectProp(i,aspect,out var r,out var d)&&r.Contains(uv)&&d<depth){index=i;depth=d;}
                if(index>=0)
                {
                    var p=world.Props[index];
                    dragOffset=world.ProjectPropAnchor(index,aspect,out var anchor)?anchor-uv:Vector2.zero;
                    owner=!string.IsNullOrEmpty(p.id)?layout?.chapters.Find(c=>c.entries.Any(x=>x.id==p.id)):null;
                    owner=owner??EnsureChapter(world.Route.ChapterAtMeters(p.s),world);
                    selected=owner.entries.Find(x=>x.id==p.id&&!string.IsNullOrEmpty(p.id))??
                        owner.entries.Find(x=>x.sprite==p.sprite&&Mathf.Abs(world.Route.MetersAt(x.level)-p.s)<.02f&&Mathf.Abs(x.lateral-p.offset)<.02f);
                    Undo.RecordObject(layout,"Arrastar objeto do mapa");
                    GUIUtility.hotControl=dragControl=control;e.Use();
                }
            }
            if(e.type==EventType.MouseDrag&&selected!=null&&GUIUtility.hotControl==dragControl&&dragControl!=0)
            {
                if(world.GroundAtViewport(uv+dragOffset,cursor,aspect,out var phase,out var side))
                {
                    selected.height=selected.EffectiveHeight(world.CameraModel.Kit);
                    selected.sprite=selected.EffectiveSprite(world.CameraModel.Kit);
                    selected.level=ClampPhase(phase,owner);
                    selected.lateral=side;selected.keepSprite=true;Changed(world);
                }
                e.Use();
            }
            if(e.type==EventType.MouseUp&&dragControl!=0&&GUIUtility.hotControl==dragControl)
            {GUIUtility.hotControl=0;dragControl=0;e.Use();}
            if(selected==null)return;
            for(var i=0;i<world.Props.Count;i++)
                if(world.Props[i].id==selected.id&&world.ProjectProp(i,aspect,out var r,out _))
                {
                    var box=new Rect(imageRect.x+r.x*imageRect.width,imageRect.y+(1-r.yMax)*imageRect.height,r.width*imageRect.width,r.height*imageRect.height);
                    Handles.BeginGUI();Handles.color=Color.yellow;Handles.DrawAAPolyLine(2,new Vector3(box.x,box.y),new Vector3(box.xMax,box.y),
                        new Vector3(box.xMax,box.yMax),new Vector3(box.x,box.yMax),new Vector3(box.x,box.y));Handles.EndGUI();
                }
        }
    }
}

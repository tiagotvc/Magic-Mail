using System;using System.Collections.Generic;using System.IO;using System.Linq;using UnityEditor;using UnityEngine;using UnityEngine.UI;using RoyalAves.UI.RoyalPostman2026;using Object=UnityEngine.Object;
namespace RoyalAves.EditorTools
{
 public sealed class RoyalPostmanScreenEditor : EditorWindow
 {
  RoyalPostmanScreenDocument document;RectTransform selected;RoyalPostmanInventoryHover hovered;readonly List<UnityEditor.Editor> inspectors=new List<UnityEditor.Editor>();
  Vector2 treeScroll,inspectScroll,previewScroll,pressPoint;[SerializeField]string id="home";string filter="";[SerializeField]int resolution=2;[SerializeField]bool autoSave=true;bool saveQueued,dragging;float zoom;Vector3 dragPoint,startPosition;Rect previewRect;
  static readonly Vector2Int[] Sizes={new Vector2Int(360,800),new Vector2Int(720,1280),new Vector2Int(1080,1920)};
  static readonly Vector2Int[] ResizeHandles={new Vector2Int(-1,-1),new Vector2Int(0,-1),new Vector2Int(1,-1),new Vector2Int(1,0),new Vector2Int(1,1),new Vector2Int(0,1),new Vector2Int(-1,1),new Vector2Int(-1,0)};
  RoyalPostmanRectResize resizeDrag;int dragUndoGroup;bool dirtyBeforeDrag;
  [SerializeField]RoyalPostmanButtonPreview buttonPreview;
  [SerializeField]Object spriteToAdd;
  string loadError;
  [MenuItem("Royal Postman/Editor de telas")]
  [MenuItem("Royal Aves/Editor de telas")]
  public static void Open(){
   var window=GetWindow<RoyalPostmanScreenEditor>(false,"Editor de telas",true);window.minSize=new Vector2(900,560);
   if(!window.docked){var visible=VisibleWindowRect(window.position,EditorGUIUtility.GetMainWindowPosition());window.minSize=new Vector2(Mathf.Min(900,visible.width),Mathf.Min(560,visible.height));window.position=visible;}
   window.loadError=null;window.Show();window.Focus();window.Repaint();
  }
  internal static Rect VisibleWindowRect(Rect current,Rect main){
   if(main.width<400||main.height<300)main=new Rect(0,0,1280,800);
   var area=new Rect(main.x+20,main.y+40,main.width-40,main.height-60);
   float width=Mathf.Clamp(current.width,Mathf.Min(900,area.width),area.width),height=Mathf.Clamp(current.height,Mathf.Min(560,area.height),area.height);
   float x=current.x,y=current.y;if(!area.Overlaps(current)){x=area.center.x-width*.5f;y=area.center.y-height*.5f;}
   return new Rect(Mathf.Clamp(x,area.xMin,area.xMax-width),Mathf.Clamp(y,area.yMin,area.yMax-height),width,height);
  }
  void OnEnable(){resolution=Mathf.Clamp(resolution,0,Sizes.Length-1);wantsMouseMove=true;wantsMouseEnterLeaveWindow=true;Undo.undoRedoPerformed+=UndoChanged;EditorApplication.projectChanged+=ProjectChanged;EditorApplication.playModeStateChanged+=PlayChanged;}
  void OnDisable(){if(document!=null&&document.Dirty&&!EditorApplication.isPlaying)Save();Dispose();Undo.undoRedoPerformed-=UndoChanged;EditorApplication.projectChanged-=ProjectChanged;EditorApplication.playModeStateChanged-=PlayChanged;}
  void PlayChanged(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingEditMode){if(document!=null&&document.Dirty)Save();Dispose();}Repaint();}
  void ProjectChanged(){if(document!=null&&!document.Dirty&&!dragging&&!EditorApplication.isPlaying&&document.SourceChanged)Load(id);Repaint();}
  void UndoChanged(){if(document==null)return;document.Dirty=true;document.Render();QueueSave();Repaint();}
  void ClearHover(){if(hovered!=null)hovered.PreviewHover(false);hovered=null;}
  void OnLostFocus(){ClearHover();if(dragging)FinishDrag(false);if(document!=null)document.Render();Repaint();}
  void Dispose(){ClearHover();if(dragging){dragging=false;resizeDrag=null;GUIUtility.hotControl=0;}foreach(var e in inspectors)if(e!=null)DestroyImmediate(e);inspectors.Clear();document?.Dispose();document=null;selected=null;}
  internal bool Load(string next){
   if(document!=null&&document.Dirty&&!Save())return false;
   Dispose();loadError=null;
   try{
    var kit=RoyalPostmanRuntimeKit.Load();if(kit==null)throw new InvalidOperationException("O RuntimeKit das telas não foi encontrado no projeto.");
    id=next;if(kit.ScreenPrefab(id)==null){id=kit.home!=null?"home":kit.additionalScreens?.FirstOrDefault(s=>s.prefab!=null)?.id;}
    if(string.IsNullOrEmpty(id)||kit.ScreenPrefab(id)==null)throw new InvalidOperationException("O RuntimeKit não possui uma tela válida para abrir.");
    resolution=Mathf.Clamp(resolution,0,Sizes.Length-1);var size=Sizes[resolution];document=new RoyalPostmanScreenDocument(kit,id,size.x,size.y);document.ButtonPreview=buttonPreview;document.Render();Repaint();return true;
   }catch(Exception error){document?.Dispose();document=null;loadError=error.Message;Debug.LogException(error);Repaint();return false;}
  }
  void Select(RectTransform rect){selected=rect;foreach(var e in inspectors)if(e!=null)DestroyImmediate(e);inspectors.Clear();if(rect==null)return;
   foreach(var component in rect.GetComponents<Component>())if(component is RoyalPostmanCurrentPet||component is Graphic||component is RoyalPostmanAvatar||component is RoyalPostmanPaintedButton||component is ScrollRect)inspectors.Add(UnityEditor.Editor.CreateEditor(component));
  }
  void QueueSave(){if(!autoSave||saveQueued)return;saveQueued=true;EditorApplication.delayCall+=()=>{saveQueued=false;if(this!=null&&document!=null&&document.Dirty&&!dragging&&!EditorApplication.isPlaying)Save();};}
  void Changed(){document.Dirty=true;document.Render();QueueSave();Repaint();}
  bool Save(){try{ClearHover();document?.Save();return true;}catch(Exception error){Debug.LogException(error);ShowNotification(new GUIContent("Não foi possível salvar: "+error.Message));return false;}}
  void OnGUI(){
   if(Event.current.type==EventType.MouseLeaveWindow){ClearHover();if(document!=null)document.Render();Repaint();}
   if(EditorApplication.isPlaying){EditorGUILayout.HelpBox("Saia do Play para editar os arquivos das telas.",MessageType.Info);return;}
   if(document==null&&string.IsNullOrEmpty(loadError))Load(id);
   using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)){
    var ids=new List<string>{"home"};var titles=new List<string>{"Home"};var kit=RoyalPostmanRuntimeKit.Load();
    if(kit!=null)foreach(var s in kit.additionalScreens)if(s.prefab!=null&&s.id!="home"&&!ids.Contains(s.id)){ids.Add(s.id);titles.Add(string.IsNullOrEmpty(s.title)?s.id:s.title);}
    int screen=EditorGUILayout.Popup(Mathf.Max(0,ids.IndexOf(id)),titles.ToArray(),EditorStyles.toolbarPopup,GUILayout.Width(130));if(ids[screen]!=id)Load(ids[screen]);
    int next=EditorGUILayout.Popup(resolution,new[]{"360 × 800","720 × 1280","1080 × 1920"},EditorStyles.toolbarPopup,GUILayout.Width(130));if(next!=resolution){resolution=next;var size=Sizes[next];document?.Resize(size.x,size.y);}
    if(GUILayout.Button("Ajustar",EditorStyles.toolbarButton))zoom=0;
    if(GUILayout.Button("100%",EditorStyles.toolbarButton))zoom=1;
    if(GUILayout.Button("200%",EditorStyles.toolbarButton))zoom=2;
    autoSave=GUILayout.Toggle(autoSave,"Salvar automaticamente",EditorStyles.toolbarButton);
    if(GUILayout.Button(document!=null&&document.Dirty?"Salvar *":"Salvar",EditorStyles.toolbarButton))Save();
    if(GUILayout.Button("Recarregar",EditorStyles.toolbarButton))Load(id);
    if(GUILayout.Button("PNG",EditorStyles.toolbarButton)&&document!=null){string file=EditorUtility.SaveFilePanel("Exportar tela","","home.png","png");if(!string.IsNullOrEmpty(file))document.Export(file);}
   }
   if(document==null){EditorGUILayout.HelpBox(string.IsNullOrEmpty(loadError)?"Nenhuma tela disponível.":"Não foi possível carregar a tela: "+loadError,MessageType.Error);if(GUILayout.Button("Tentar abrir novamente"))Load(id);return;}
   using(new EditorGUILayout.HorizontalScope()){
    using(new EditorGUILayout.VerticalScope(GUILayout.Width(230))){
     GUILayout.Label("Elementos",EditorStyles.boldLabel);filter=EditorGUILayout.TextField(filter,EditorStyles.toolbarSearchField);
     treeScroll=EditorGUILayout.BeginScrollView(treeScroll);Tree(document.Root.transform,0);EditorGUILayout.EndScrollView();
     EditorGUILayout.HelpBox(id=="home"?"A navegação inferior é compartilhada. Edite-a aqui.":document.Kit.ShowsNavigation(id)?"A navegação inferior vem da Home e está bloqueada.":"Esta tela usa o rodapé próprio do seu layout.",MessageType.None);
     if(GUILayout.Button("Cadastro de telas"))Selection.activeObject=document.Kit;
    }
    DrawPreview();
    using(new EditorGUILayout.VerticalScope(GUILayout.Width(310))){
     GUILayout.Label("Propriedades",EditorStyles.boldLabel);inspectScroll=EditorGUILayout.BeginScrollView(inspectScroll);
     float oldLabelWidth=EditorGUIUtility.labelWidth,oldFieldWidth=EditorGUIUtility.fieldWidth;bool oldWideMode=EditorGUIUtility.wideMode,oldHierarchyMode=EditorGUIUtility.hierarchyMode;
     try{
     EditorGUIUtility.labelWidth=112;EditorGUIUtility.fieldWidth=48;EditorGUIUtility.wideMode=false;EditorGUIUtility.hierarchyMode=false;
     EditorGUI.BeginChangeCheck();var preview=(RoyalPostmanButtonPreview)EditorGUILayout.Popup(new GUIContent("Botões no preview","Altera apenas a aparência no editor; não muda o estoque nem as regras do jogo e não é salvo no prefab."),(int)buttonPreview,new[]{"Estado do jogo","Ativos (cor real)","Inativos"});
     if(EditorGUI.EndChangeCheck()){buttonPreview=preview;document.ButtonPreview=preview;document.Render();Repaint();}
     GUILayout.Space(8);
     if(selected!=null){
      GUILayout.Label(selected.name,EditorStyles.boldLabel);
      bool locked=document.Locked(selected);if(locked)EditorGUILayout.HelpBox("Edite este componente na Home.",MessageType.Info);
      using(new EditorGUI.DisabledScope(locked)){
       GUILayout.Label("Novo sprite",EditorStyles.boldLabel);
       spriteToAdd=EditorGUILayout.ObjectField("PNG ou Sprite",spriteToAdd,typeof(Object),false);
       using(new EditorGUI.DisabledScope(!(spriteToAdd is Sprite)&&!(spriteToAdd is Texture2D))){
        if(GUILayout.Button(new GUIContent("Adicionar sprite atrás","Cria uma imagem independente imediatamente atrás do elemento selecionado, no mesmo grupo."))){
         var sprite=SpriteFromAsset(spriteToAdd);if(sprite!=null){var added=CreateSpriteBehind(selected,sprite);Select(added);treeScroll=Vector2.zero;filter="";Changed();}
        }
       }
       using(new EditorGUILayout.HorizontalScope()){
        if(GUILayout.Button("Para trás")){MoveLayer(selected,-1);Changed();}
        if(GUILayout.Button("Para frente")){MoveLayer(selected,1);Changed();}
       }
       GUILayout.Space(8);
       DrawRectProperties();
       EditorGUI.BeginChangeCheck();bool active=EditorGUILayout.Toggle("Visível",selected.gameObject.activeSelf);
       if(EditorGUI.EndChangeCheck()){Undo.RecordObject(selected.gameObject,"Visibilidade da tela");selected.gameObject.SetActive(active);Changed();}
       var image=selected.GetComponent<Image>();if(image!=null){
        EditorGUI.BeginChangeCheck();var asset=EditorGUILayout.ObjectField("Imagem (PNG ou Sprite)",image.sprite,typeof(Object),false);
        if(EditorGUI.EndChangeCheck()&&asset!=null)ReplaceImage(image,asset);
       }
       EditorGUI.BeginChangeCheck();foreach(var editor in inspectors){if(editor==null||editor.target==null)continue;EditorGUIUtility.labelWidth=112;EditorGUIUtility.fieldWidth=48;EditorGUIUtility.wideMode=false;editor.OnInspectorGUI();GUILayout.Space(8);}if(EditorGUI.EndChangeCheck())Changed();
      }
      if(new[]{"CoinValue","LivesValue","LifeStatus","LevelLabel"}.Contains(selected.name))EditorGUILayout.HelpBox("O jogo fornece este valor. Posição, fonte, tamanho e cor são salvos.",MessageType.None);
     }else EditorGUILayout.HelpBox("Clique no preview ou escolha um elemento. Arraste para mover; use as setas para ajustar.",MessageType.None);
     }finally{EditorGUIUtility.labelWidth=oldLabelWidth;EditorGUIUtility.fieldWidth=oldFieldWidth;EditorGUIUtility.wideMode=oldWideMode;EditorGUIUtility.hierarchyMode=oldHierarchyMode;}
     EditorGUILayout.EndScrollView();
    }
   }
  }
  internal static void ApplyRectLayout(RectTransform rect,Vector2 position,Vector2 size){
   Undo.RecordObject(rect,"Redimensionar elemento da tela");rect.anchoredPosition=position;
   rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Max(1,size.x));rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(1,size.y));
  }
  void DrawRectProperties(){
   GUILayout.Label("Posição e tamanho",EditorStyles.boldLabel);
   EditorGUI.BeginChangeCheck();var position=selected.anchoredPosition;var size=selected.rect.size;
   position.x=EditorGUILayout.FloatField("Posição X",position.x);position.y=EditorGUILayout.FloatField("Posição Y",position.y);
   size.x=EditorGUILayout.FloatField("Largura",size.x);size.y=EditorGUILayout.FloatField("Altura",size.y);
   if(EditorGUI.EndChangeCheck()){ApplyRectLayout(selected,position,size);Changed();}
   EditorGUILayout.HelpBox("Puxe as alças no preview para redimensionar. Shift nos cantos mantém a proporção; Esc cancela.",MessageType.None);
   EditorGUI.BeginChangeCheck();var scale=EditorGUILayout.Vector3Field("Escala",selected.localScale);
   var rotation=EditorGUILayout.Vector3Field("Rotação",selected.localEulerAngles);
   var min=EditorGUILayout.Vector2Field("Âncoras mínimas",selected.anchorMin);var max=EditorGUILayout.Vector2Field("Âncoras máximas",selected.anchorMax);var pivot=EditorGUILayout.Vector2Field("Pivô",selected.pivot);
   if(EditorGUI.EndChangeCheck()){
    Undo.RecordObject(selected,"Transformar elemento da tela");selected.localScale=scale;selected.localEulerAngles=rotation;selected.anchorMin=min;selected.anchorMax=max;selected.pivot=pivot;Changed();
   }
   GUILayout.Space(8);
  }
  void Tree(Transform node,int depth){
   bool match=string.IsNullOrEmpty(filter)||node.name.IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0;
   if(node!=document.Root.transform&&match){bool locked=document.Locked(node);using(new EditorGUI.DisabledScope(locked)){
    var style=new GUIStyle(EditorStyles.label);if(selected==node)style.normal.textColor=new Color(1,.8f,.35f);
    if(GUILayout.Button(new string(' ',Mathf.Min(depth,6)*2)+node.name+(locked?" 🔒":""),style))Select(node as RectTransform);
   }}foreach(Transform child in node)Tree(child,depth+1);
  }
  void DrawPreview(){
   var area=GUILayoutUtility.GetRect(100,100000,100,100000,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));GUI.Box(area,GUIContent.none);
   var size=Sizes[resolution];
   float scale=zoom>0?zoom:Mathf.Min((area.width-24)/size.x,(area.height-24)/size.y);scale=Mathf.Max(.05f,scale);
   var renderSize=PreviewRenderSize(size,scale,EditorGUIUtility.pixelsPerPoint);
   int renderWidth=renderSize.x,renderHeight=renderSize.y;
   if(document.Width!=renderWidth||document.Height!=renderHeight)document.Resize(renderWidth,renderHeight);
   var content=new Rect(0,0,Mathf.Max(area.width,size.x*scale+24),Mathf.Max(area.height,size.y*scale+24));
   bool pointerInsidePreview=area.Contains(Event.current.mousePosition);
   previewScroll=GUI.BeginScrollView(area,previewScroll,content);
   previewRect=new Rect((content.width-size.x*scale)*.5f,(content.height-size.y*scale)*.5f,size.x*scale,size.y*scale);
   var nextHover=!dragging&&mouseOverWindow==this&&Event.current.type!=EventType.MouseLeaveWindow&&previewRect.Contains(Event.current.mousePosition)?document.Pick(Pixel(Event.current.mousePosition))?.GetComponentInParent<RoyalPostmanInventoryHover>():null;
   if(nextHover!=hovered){ClearHover();hovered=nextHover;if(hovered!=null)hovered.PreviewHover(true);document.Render();Repaint();}
   GUI.DrawTexture(previewRect,document.Texture,ScaleMode.StretchToFill,false);
   DrawSelection();
   Input(Event.current,pointerInsidePreview);GUI.EndScrollView();
  }
  internal static Vector2Int PreviewRenderSize(Vector2Int size,float scale,float pixelsPerPoint){
   // Render at the actual display-pixel density, including Fit mode. Avoid a second shrink of the whole UI.
   float minDensity=Mathf.Max(240f/size.x,240f/size.y),maxDensity=Mathf.Min(4096f/size.x,8192f/size.y);
   float density=Mathf.Clamp(scale*Mathf.Max(.5f,pixelsPerPoint),minDensity,maxDensity);
   return new Vector2Int(Mathf.CeilToInt(size.x*density-.001f),Mathf.CeilToInt(size.y*density-.001f));
  }
  Vector2 PreviewPoint(Vector3 world){var pixel=document.Camera.WorldToScreenPoint(world);return new Vector2(previewRect.x+pixel.x/document.Width*previewRect.width,previewRect.y+(1-pixel.y/document.Height)*previewRect.height);}
  Vector2 HandlePoint(Vector2Int handle){var rect=selected.rect;return PreviewPoint(selected.TransformPoint(new Vector3(Mathf.Lerp(rect.xMin,rect.xMax,(handle.x+1)*.5f),Mathf.Lerp(rect.yMin,rect.yMax,(handle.y+1)*.5f),0)));}
  static Rect HandleRect(Vector2 point,float size)=>new Rect(point.x-size*.5f,point.y-size*.5f,size,size);
  int ResizeHit(Vector2 mouse){
   if(selected==null||document.Locked(selected))return -1;
   for(int i=0;i<ResizeHandles.Length;i++)if(HandleRect(HandlePoint(ResizeHandles[i]),14).Contains(mouse))return i;return -1;
  }
  void DrawSelection(){
   if(selected==null)return;var corners=new Vector3[4];selected.GetWorldCorners(corners);var outline=new Vector3[5];for(int i=0;i<4;i++)outline[i]=PreviewPoint(corners[i]);outline[4]=outline[0];
   Handles.BeginGUI();var oldColor=Handles.color;Handles.color=Color.yellow;Handles.DrawAAPolyLine(2,outline);Handles.color=oldColor;Handles.EndGUI();
   if(document.Locked(selected))return;
   foreach(var handle in ResizeHandles){var point=HandlePoint(handle);EditorGUI.DrawRect(HandleRect(point,10),new Color(.12f,.12f,.12f));EditorGUI.DrawRect(HandleRect(point,8),Color.yellow);EditorGUI.DrawRect(HandleRect(point,4),Color.white);
    var cursor=handle.x==0?MouseCursor.ResizeVertical:handle.y==0?MouseCursor.ResizeHorizontal:handle.x==handle.y?MouseCursor.ResizeUpRight:MouseCursor.ResizeUpLeft;
    EditorGUIUtility.AddCursorRect(HandleRect(point,14),cursor);
   }
  }
  void BeginDrag(int control,string label){
   Undo.IncrementCurrentGroup();dragUndoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName(label);dirtyBeforeDrag=document.Dirty;dragging=true;ClearHover();GUIUtility.keyboardControl=0;GUIUtility.hotControl=control;
  }
  void FinishDrag(bool cancel){
   if(cancel&&selected!=null){Undo.RecordObject(selected,"Cancelar ajuste da tela");if(resizeDrag!=null)resizeDrag.Restore();else selected.position=startPosition;document.Dirty=dirtyBeforeDrag;}
   Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(dragUndoGroup);dragging=false;resizeDrag=null;GUIUtility.hotControl=0;document.Render();QueueSave();Repaint();
  }
  Vector2 Pixel(Vector2 mouse)=>new Vector2((mouse.x-previewRect.x)/previewRect.width*document.Width,(1-(mouse.y-previewRect.y)/previewRect.height)*document.Height);
  void Input(Event e,bool pointerInsidePreview){
   int control=GUIUtility.GetControlID("RoyalPostmanRectEdit".GetHashCode(),FocusType.Passive);
   int handle=ResizeHit(e.mousePosition);
   if(e.type==EventType.MouseDown&&e.button==0&&pointerInsidePreview&&handle>=0){
    if(RectTransformUtility.ScreenPointToLocalPointInRectangle(selected,Pixel(e.mousePosition),document.Camera,out var local)){
     resizeDrag=new RoyalPostmanRectResize(selected,ResizeHandles[handle],local);BeginDrag(control,"Redimensionar elemento da tela");e.Use();
    }
   }else if(e.type==EventType.MouseDown&&e.button==0&&pointerInsidePreview&&previewRect.Contains(e.mousePosition)){
    pressPoint=e.mousePosition;
    var pixel=Pixel(e.mousePosition);var pick=document.Pick(pixel);if(pick!=null)Select(pick);
    if(selected!=null&&!document.Locked(selected)&&selected.parent is RectTransform parent&&RectTransformUtility.ScreenPointToWorldPointInRectangle(parent,pixel,document.Camera,out dragPoint)){
     startPosition=selected.position;BeginDrag(control,"Mover elemento da tela");e.Use();
    }
   }else if(e.type==EventType.MouseDrag&&dragging&&selected!=null){
    Undo.RecordObject(selected,resizeDrag!=null?"Redimensionar elemento da tela":"Mover elemento da tela");
    if(resizeDrag!=null){resizeDrag.ApplyScreen(Pixel(e.mousePosition),document.Camera,e.shift);document.Dirty=true;}
    else if(RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)selected.parent,Pixel(e.mousePosition),document.Camera,out var p)){selected.position=startPosition+p-dragPoint;document.Dirty=true;}
    document.Render();Repaint();e.Use();
   }else if(e.type==EventType.MouseUp&&e.button==0&&dragging){bool click=resizeDrag==null&&(e.mousePosition-pressPoint).sqrMagnitude<9;FinishDrag(false);if(click)document.PreviewClick(document.Pick(Pixel(e.mousePosition)));e.Use();
   }else if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape&&dragging){FinishDrag(true);e.Use();
   }else if(e.type==EventType.KeyDown&&GUIUtility.keyboardControl==0&&selected!=null&&!document.Locked(selected)){
    var delta=e.keyCode==KeyCode.LeftArrow?Vector2.left:e.keyCode==KeyCode.RightArrow?Vector2.right:e.keyCode==KeyCode.UpArrow?Vector2.up:e.keyCode==KeyCode.DownArrow?Vector2.down:Vector2.zero;
    if(delta!=Vector2.zero){Undo.RecordObject(selected,"Ajustar elemento");selected.anchoredPosition+=delta*(e.shift?10:1);Changed();e.Use();}
   }
  }
  internal static RectTransform CreateSpriteBehind(RectTransform reference,Sprite sprite){
   if(reference==null||reference.parent==null||sprite==null)throw new ArgumentException("Selecione um elemento e um Sprite válido.");
   var go=new GameObject(GameObjectUtility.GetUniqueNameForSibling(reference.parent,sprite.name),typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
   var rect=(RectTransform)go.transform;rect.SetParent(reference.parent,false);rect.anchorMin=reference.anchorMin;rect.anchorMax=reference.anchorMax;rect.pivot=reference.pivot;
   rect.anchoredPosition3D=reference.anchoredPosition3D;rect.sizeDelta=reference.sizeDelta;rect.localScale=reference.localScale;rect.localRotation=reference.localRotation;rect.SetSiblingIndex(reference.GetSiblingIndex());
   var image=go.GetComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;image.type=sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;
   Undo.RegisterCreatedObjectUndo(go,"Adicionar sprite atrás");return rect;
  }
  internal static void MoveLayer(RectTransform rect,int direction){
   if(rect==null||rect.parent==null)return;int next=Mathf.Clamp(rect.GetSiblingIndex()+direction,0,rect.parent.childCount-1);
   if(next==rect.GetSiblingIndex())return;Undo.RegisterFullObjectHierarchyUndo(rect.parent.gameObject,"Ordenar camada da tela");rect.SetSiblingIndex(next);
  }
  internal static Sprite SpriteFromAsset(Object asset){
   Sprite sprite=asset as Sprite;
   if(asset is Texture2D texture){
    var path=AssetDatabase.GetAssetPath(texture);var temporary=new Texture2D(2,2,TextureFormat.RGBA32,false);Rect rect=new Rect(0,0,texture.width,texture.height);
    try{if(ImageConversion.LoadImage(temporary,File.ReadAllBytes(path))){var pixels=temporary.GetPixels32();int x0=temporary.width,y0=temporary.height,x1=-1,y1=-1;for(int y=0;y<temporary.height;y++)for(int x=0;x<temporary.width;x++)if(pixels[y*temporary.width+x].a>200){x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}if(x1>=x0)rect=new Rect((float)x0/temporary.width*texture.width,(float)y0/temporary.height*texture.height,(float)(x1-x0+1)/temporary.width*texture.width,(float)(y1-y0+1)/temporary.height*texture.height);}}finally{DestroyImmediate(temporary);}
    sprite=Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect);sprite.name=texture.name;
    string folder="Assets/RoyalAves/Art/UI/Menu/RoyalPostman/EditorSprites";Directory.CreateDirectory(folder);AssetDatabase.Refresh();AssetDatabase.CreateAsset(sprite,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+texture.name+".asset"));
   }
   return sprite;
  }
  void ReplaceImage(Image image,Object asset){
   var sprite=SpriteFromAsset(asset);if(sprite==null)return;Undo.RecordObject(image,"Trocar imagem da tela");image.sprite=sprite;
   if(image.material!=null&&image.material.shader.name=="RoyalPostman/UI/PaintedPalette"){
    var material=new Material(image.material);material.name=image.material.name+"-"+sprite.name;var r=sprite.rect;material.SetVector("_ButtonUVRect",new Vector4(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));
    string folder="Assets/RoyalAves/Art/UI/Button/RoyalPostman/Materials";Directory.CreateDirectory(folder);AssetDatabase.Refresh();AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+material.name+".mat"));image.material=material;
   }Changed();
  }
 }
}


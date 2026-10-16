using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Standalone cutout prototype. Original approved illustration remains untouched.
    public static class RoyalAvesPostmanRigBuilder
    {
        public const string Folder="Assets/RoyalAves/Characters/PostmanV8";
        const string ArtFolder="Assets/RoyalAves/Art/MapKitV3/Characters/";
        static Material material;

        [MenuItem("Royal Aves/Personagens/Criar rig do carteiro")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if(shader==null)throw new Exception("Sprite Unlit shader missing");
            material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Postman.mat");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Folder+"/Postman.mat");}
            var root=new GameObject("PostmanV8");
            try
            {
                var hips=Joint(root.transform,"Hips",new Vector2(0,3.65f));
                Art(hips,"pelvis",.78f,new Vector2(.5f,.9f),2);
                Leg(hips,"Left",new Vector2(-.36f,-.27f),3);
                Leg(hips,"Right",new Vector2(.36f,-.27f),0);
                var body=Joint(hips,"Body",new Vector2(0,.17f));
                var tails=Joint(body,"CoatTails",new Vector2(0,.06f));
                Art(tails,"coat-tails",1.5f,new Vector2(.44f,.94f),4);
                Art(body,"torso",1.92f,new Vector2(.5f,.1f),8);
                var head=Joint(body,"Head",new Vector2(-.01f,1.55f),-4);
                Art(head,"head",1.65f,new Vector2(.32f,.08f),10);
                var scarf=Joint(body,"Scarf",new Vector2(-.05f,1.56f),-13);
                Art(scarf,"scarf",1.05f,new Vector2(.22f,.9f),12);
                var bag=Joint(body,"Satchel",new Vector2(-.62f,.35f),-8);
                Art(bag,"satchel",1.25f,new Vector2(.50f,.93f),13);
                Arm(body,"Left",new Vector2(-.64f,1.28f),-24,125,9);
                Arm(body,"Right",new Vector2(.49f,1.36f),63,22,9);
                var controller=BuildController(root);
                var animator=root.AddComponent<Animator>();
                animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PostmanV8.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("[PostmanV8] Prefab and Idle/Wave/Walk clips saved.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        static Transform Joint(Transform parent,string name,Vector2 position,float angle=0)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);
            t.localPosition=position;t.localRotation=Quaternion.Euler(0,0,angle);return t;
        }
        static Sprite Sprite(string part)
        {
            var family=part.StartsWith("leg")||part.StartsWith("boot")||part=="satchel"||part=="scarf"||part=="coat-tails"?"postman-lower-rig-v8":"postman-upper-rig-v8";
            var name=(family+"_"+part).Replace('-','_');
            return AssetDatabase.LoadAllAssetsAtPath(ArtFolder+family+".png").OfType<Sprite>().FirstOrDefault(x=>x.name==name)
                ??throw new Exception("Missing cutout: "+name);
        }
        static void Art(Transform joint,string part,float height,Vector2 anchor,int order,bool mirror=false)
        {
            var sprite=Sprite(part);
            var image=new GameObject("Art").transform;image.SetParent(joint,false);
            float scale=height/sprite.bounds.size.y,width=sprite.bounds.size.x*scale,sign=mirror?-1:1;
            image.localScale=new Vector3(sign*scale,scale,1);
            image.localPosition=new Vector3(-sign*(anchor.x-.5f)*width,-anchor.y*height,0);
            var sr=image.gameObject.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.sortingOrder=order;
        }
        static void Leg(Transform hips,string side,Vector2 offset,int order)
        {
            bool left=side=="Left";var id=left?"left":"right";
            var thigh=Joint(hips,side+"Thigh",offset,left?-10:12);
            Art(thigh,"leg-"+id+"-upper",1.58f,new Vector2(left?.528f:.537f,.92f),order+2);
            var shin=Joint(thigh,side+"Shin",new Vector2(left?-.0815f:-.0093f,-1.264f),left?12:-15);
            Art(shin,"leg-"+id+"-lower",1.57f,new Vector2(left?.48f:.564f,.92f),order+1);
            var boot=Joint(shin,side+"Boot",new Vector2(left?.0588f:-.0845f,-1.256f));
            Art(boot,"boot-"+id,.88f,new Vector2(left?.278f:.65f,.85f),order,true);
        }
        static void Arm(Transform body,string side,Vector2 offset,float shoulder,float elbow,int order)
        {
            bool left=side=="Left";var id=left?"left":"right";
            var upper=Joint(body,side+"Arm",offset,shoulder);
            Art(upper,"arm-"+id+"-upper",1.02f,new Vector2(left?.583f:.44f,.92f),order);
            var lower=Joint(upper,side+"Forearm",new Vector2(left?-.081f:.03f,-.816f),elbow);
            Art(lower,"arm-"+id+"-lower",.98f,new Vector2(left?.381f:.599f,.92f),order+1);
            var hand=Joint(lower,side+"Hand",new Vector2(left?.0255f:-.0245f,-.784f),left?0:8);
            Art(hand,"hand-"+id,left?.60f:.74f,new Vector2(left?.379f:.467f,.92f),order,!left);
        }
        static AnimatorController BuildController(GameObject root)
        {
            string path=Folder+"/Postman.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine=controller.layers[0].stateMachine;
            foreach(var state in machine.states)machine.RemoveState(state.state);
            foreach(var transition in machine.anyStateTransitions)machine.RemoveAnyStateTransition(transition);
            controller.parameters=new[]{new AnimatorControllerParameter{name="Motion",type=AnimatorControllerParameterType.Int,defaultInt=0}};
            for(int mode=0;mode<3;mode++)
            {
                string name=new[]{"Idle","Wave","Walk"}[mode];
                string clipPath=Folder+"/"+name+".anim";
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
                clip.ClearCurves();clip.frameRate=30;clip.name=name;
                float duration=mode==0?3.2f:mode==1?2.4f:1.2f;
                // Reset every articulated transform in every clip: switching state cannot retain an old pose.
                foreach(var joint in root.GetComponentsInChildren<Transform>().Where(t=>t!=root.transform&&t.name!="Art"))
                {
                    string p=AnimationUtility.CalculateTransformPath(joint,root.transform);
                    Constant(clip,p,"localEulerAnglesRaw.z",Mathf.DeltaAngle(0,joint.localEulerAngles.z),duration);
                    Constant(clip,p,"m_LocalPosition.y",joint.localPosition.y,duration);
                    Constant(clip,p,"m_LocalScale.y",1,duration);
                }
                string body="Hips/Body";
                Curve(clip,"Hips","m_LocalPosition.y",duration,3.65f,mode==2?.055f:.018f,mode==2?2:1);
                Curve(clip,body,"m_LocalScale.y",duration,1,.014f,1);
                Curve(clip,body+"/Head","localEulerAnglesRaw.z",duration,-4,2,1);
                Curve(clip,body+"/Scarf","localEulerAnglesRaw.z",duration,-13,mode==2?7:3,mode==2?2:1);
                Curve(clip,body+"/Satchel","localEulerAnglesRaw.z",duration,-8,mode==2?8:2,mode==2?1:1);
                Curve(clip,body+"/CoatTails","localEulerAnglesRaw.z",duration,0,mode==2?5:1.6f,1);
                if(mode==1)
                {
                    Curve(clip,body+"/RightArm","localEulerAnglesRaw.z",duration,85,3,2);
                    Curve(clip,body+"/RightArm/RightForearm","localEulerAnglesRaw.z",duration,36,11,3);
                    Curve(clip,body+"/RightArm/RightForearm/RightHand","localEulerAnglesRaw.z",duration,8,17,3);
                }
                if(mode==2)
                {
                    Curve(clip,"Hips/LeftThigh","localEulerAnglesRaw.z",duration,-10,16,1);
                    Curve(clip,"Hips/RightThigh","localEulerAnglesRaw.z",duration,12,-16,1);
                    Curve(clip,"Hips/LeftThigh/LeftShin","localEulerAnglesRaw.z",duration,12,10,1);
                    Curve(clip,"Hips/RightThigh/RightShin","localEulerAnglesRaw.z",duration,-15,-10,1);
                    Curve(clip,body+"/LeftArm","localEulerAnglesRaw.z",duration,-24,8,1);
                    Curve(clip,body+"/RightArm","localEulerAnglesRaw.z",duration,25,-12,1);
                    Constant(clip,body+"/RightArm/RightForearm","localEulerAnglesRaw.z",25,duration);
                }
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
                EditorUtility.SetDirty(clip);
                var state=machine.AddState(name,new Vector3(mode*230,70));state.motion=clip;
                if(mode==0)machine.defaultState=state;
                var enter=machine.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=.2f;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.Equals,mode,"Motion");
            }
            EditorUtility.SetDirty(controller);return controller;
        }
        static void Constant(AnimationClip clip,string path,string property,float value,float duration)
            =>AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),AnimationCurve.Constant(0,duration,value));
        static void Curve(AnimationClip clip,string path,string property,float duration,float center,float amplitude,int cycles)
        {
            var keys=new Keyframe[33];
            for(int i=0;i<keys.Length;i++){float t=i/(float)(keys.Length-1);float angle=t*Mathf.PI*2*cycles;float tangent=amplitude*Mathf.Cos(angle)*Mathf.PI*2*cycles/duration;keys[i]=new Keyframe(t*duration,center+amplitude*Mathf.Sin(angle),tangent,tangent);}
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),property),new AnimationCurve(keys));
        }
    }
}

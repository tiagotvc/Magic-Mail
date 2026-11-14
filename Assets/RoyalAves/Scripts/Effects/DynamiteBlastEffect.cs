using UnityEngine;
namespace RoyalAves.Effects
{
    /// <summary>Only the explosive package uses this 6x6 volumetric flipbook.</summary>
    public sealed class DynamiteBlastEffect : MonoBehaviour
    {
        static GameObject prefab;
        ParticleSystem[] emitters;
        float age;
        public bool IsEmphasizing=>age<.28f;
        public static DynamiteBlastEffect Play(Vector3 position,float cellWidth,int radius,bool combined)
        {
            if(prefab==null)prefab=Resources.Load<GameObject>("Effects/PackageExplosionFlipbook");
            if(prefab==null){Debug.LogError("Package explosion flipbook prefab is missing.");return null;}
            var root=Instantiate(prefab,position+Vector3.back*.3f,Quaternion.identity);
            root.name="ExplosivePackageBlast(Clone)";
            root.transform.localScale=prefab.transform.localScale*cellWidth*(combined?Mathf.Max(1.35f,(radius+.5f)/(SweetSugar.Scripts.Items.ItemDestroyAnimation.BlastRadius+.5f)):1);
            PowerUpEffectOrder.Raise(root);
            var playback=root.AddComponent<DynamiteBlastEffect>();
            playback.emitters=root.GetComponentsInChildren<ParticleSystem>();
            foreach(var emitter in playback.emitters)
            {
                emitter.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                emitter.Simulate(.025f,false,true,false);
            }
            return playback;
        }
        void LateUpdate()
        {
            if(emitters==null)return;
            // Local stepping keeps a slow Editor frame from consuming the visible burst.
            float step=Mathf.Min(Time.deltaTime,.05f);age+=step;
            foreach(var emitter in emitters)if(emitter!=null)emitter.Simulate(step,false,false,false);
            if(age>=.8f)Destroy(gameObject);
        }
    }
}

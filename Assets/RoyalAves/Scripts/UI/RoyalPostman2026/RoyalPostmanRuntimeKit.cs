using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    public sealed class RoyalPostmanRuntimeKit : ScriptableObject
    {
        public const string ResourcePath="RoyalPostmanUI2026/RuntimeKit";
        public GameObject home, playModal;
        public GameObject navigation;
        [System.Serializable] public sealed class Screen
        {
            public string id, title;
            public GameObject prefab;
            public bool hideNavigation;
        }
        public System.Collections.Generic.List<Screen> additionalScreens = new System.Collections.Generic.List<Screen>();
        public GameObject ScreenPrefab(string id)=>id=="home"?home:additionalScreens?.Find(s=>s.id==id)?.prefab;
        public bool ShowsNavigation(string id)=>id=="home"||additionalScreens?.Find(s=>s.id==id)?.hideNavigation!=true;
        // Editor and game instantiate this same authored view. No layout is manufactured here.
        public GameObject InstantiateScreen(string id,RectTransform parent)
        {
            var source=ScreenPrefab(id);if(source==null)return null;
            var root=Instantiate(source,parent,false);root.name=source.name;
            var viewport=root.GetComponent<RoyalPostmanViewport>()??root.AddComponent<RoyalPostmanViewport>();viewport.Fit();
            if(id!="home"&&navigation!=null&&ShowsNavigation(id))
            {
                var old=root.transform.Find("Navigation");if(old!=null){old.gameObject.SetActive(false);if(Application.isPlaying)Destroy(old.gameObject);else DestroyImmediate(old.gameObject);}
                var nav=Instantiate(navigation,root.transform,false);nav.name="Navigation";
            }
            foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=parent.gameObject.layer;
            return root;
        }
        public Sprite rocket, bomb, orb;
        static RoyalPostmanRuntimeKit cached;
        public static RoyalPostmanRuntimeKit Load()=>cached!=null?cached:(cached=Resources.Load<RoyalPostmanRuntimeKit>(ResourcePath));
        // Rebind a view already instantiated before an editor asset repair/reimport.
        public static bool RestorePaint(Transform target,GameObject source,string sentinel)
        {
            if(source==null)return false;
            var check=source.transform.Find(sentinel)?.GetComponent<Image>();if(check==null||check.sprite==null)return false;
            foreach(var image in target.GetComponentsInChildren<Image>(true))
            {
                if(image.sprite!=null||image.transform==target)continue;
                string path=image.name;var node=image.transform.parent;
                while(node!=target&&node!=null){path=node.name+"/"+path;node=node.parent;}
                if(node!=target)continue;
                var original=source.transform.Find(path)?.GetComponent<Image>();
                if(original!=null&&original.sprite!=null)image.sprite=original.sprite;
            }
            return true;
        }
    }
}

using System.Collections;
using SweetSugar.Scripts.Blocks;
using UnityEngine;
namespace RoyalAves.Gameplay
{
    // Authored layer instantiates the same approved animation on its owning board cell.
    public sealed class PostalTapePlacement : MonoBehaviour
    {
        public Sprite strip;
        PostalTapeLayer tape;
        IEnumerator Start()
        {
            GetComponent<SpriteRenderer>().enabled=false;
            var layer=GetComponent<Square>();var cell=layer.mainSquare!=null?layer.mainSquare:transform.parent?.GetComponent<Square>();
            if(cell==null)yield break;
            while(cell.Item==null)yield return null;
            tape=cell.GetComponent<PostalTapeLayer>();
            if(tape==null){tape=cell.gameObject.AddComponent<PostalTapeLayer>();tape.Initialize(cell,strip,layer);}
        }
        void OnDestroy(){if(tape!=null && !tape.IsReleased)Destroy(tape);}
    }
}

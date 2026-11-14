using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    [RequireComponent(typeof(Square),typeof(SpriteRenderer))]
    public sealed class SafeCell : MonoBehaviour
    {
        public GameObject animationPrefab;
        public SafeBoardController Controller {get;private set;}
        void Start() => EnsureController();
        public void EnsureController()
        {
            if(Controller!=null)return;
            var layer=GetComponent<Square>();var anchor=layer.mainSquare;var field=layer.field;
            if(anchor==null || field==null)return;
            var cells=new[]{anchor,field.GetSquare(anchor.col+1,anchor.row),field.GetSquare(anchor.col,anchor.row+1),field.GetSquare(anchor.col+1,anchor.row+1)};
            if(cells.Any(c=>c==null || !c.subSquares.Contains(layer)))
            {Debug.LogError("Cofre incompleto: era esperado um objeto compartilhado por quatro células.",this);return;}
            GetComponent<SpriteRenderer>().enabled=false;
            var root=Instantiate(animationPrefab,field.transform);
            root.name="Safe_2x2";
            root.transform.position=(cells[0].transform.position+cells[3].transform.position)*.5f+Vector3.back*.6f;
            float width=Mathf.Min(Mathf.Abs(cells[3].transform.position.x-cells[0].transform.position.x),
                Mathf.Abs(cells[3].transform.position.y-cells[0].transform.position.y))*2*.96f;
            var scale=field.transform.lossyScale;
            root.transform.localScale=new Vector3(width/1.8f/Mathf.Abs(scale.x),width/1.8f/Mathf.Abs(scale.y),1);
            Controller=root.AddComponent<SafeBoardController>();
            Controller.Initialize(root.GetComponent<SafeBreakAnimation>(),layer,cells);
        }
        public void HandleHit(Square hitCell,Square origin)
        {
            EnsureController();
            // Existing DestroyBlock contract: direct/null origin is a special; a forwarded
            // adjacent match carries the original neighboring cell and cannot damage a safe.
            Controller?.AcceptHit(origin==null || origin==hitCell);
        }
        void LateUpdate() {GetComponent<SpriteRenderer>().enabled=false;}
        void OnDestroy() {if(Controller!=null && !Controller.IsComplete)Destroy(Controller.gameObject);}
    }
}

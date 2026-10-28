using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // Only the face moves: gold rim, well, button hit target and layout stay fixed.
    public sealed class RoyalPostmanPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public RectTransform face;
        public float travel = 7;
        Vector2 origin;
        bool initialized,down;
        float amount;
        void Capture(){if(initialized||face==null)return;origin=face.anchoredPosition;initialized=true;}
        public void SetPressed(bool value){Capture();down=value;Apply(value?1:0);}
        public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left&&GetComponent<Button>().IsInteractable()){Capture();down=true;}}
        public void OnPointerUp(PointerEventData e){down=false;}
        public void OnPointerExit(PointerEventData e){down=false;}
        void Update(){if(face==null)return;Capture();amount=Mathf.MoveTowards(amount,down?1:0,Time.unscaledDeltaTime*16);Apply(amount);}
        void Apply(float value){if(face==null)return;amount=value;face.anchoredPosition=origin+Vector2.down*(travel*value);face.localScale=new Vector3(1-.014f*value,1-.025f*value,1);}
        void OnDisable(){down=false;if(initialized)Apply(0);}
    }
}

using UnityEngine;

namespace RoyalAves.EditorTools
{
    [ExecuteAlways]
    public sealed class RoyalAvesMapPreviewInput : MonoBehaviour
    {
        void OnGUI()
        {
            var input=Event.current;
            var control=GUIUtility.GetControlID(FocusType.Passive);
            var type=input.type;
            if(!RoyalAvesMapSimulatorPreview.HandleInput(type,input.button,input.mousePosition,input.delta,Screen.height,Screen.width))return;
            if(type==EventType.MouseDown)GUIUtility.hotControl=control;
            if(type==EventType.MouseUp||type==EventType.Ignore)GUIUtility.hotControl=0;
            input.Use();
        }
    }
}

using System;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Fica fora da pasta Editor para poder ser adicionado a um GameObject (o Unity recusa scripts de Editor).
    // O codigo do editor registra o tratador em Handler; sem ele (ou fora do editor) o componente nao faz nada.
    [ExecuteAlways]
    public sealed class RoyalAvesMapPreviewInput : MonoBehaviour
    {
        // (tipo, botao, posicao do mouse, delta, altura da tela, largura da tela) -> true se consumiu o evento
        public static Func<EventType, int, Vector2, Vector2, float, float, bool> Handler;

        void OnGUI()
        {
            if (Handler == null) return;
            var input = Event.current;
            var control = GUIUtility.GetControlID(FocusType.Passive);
            var type = input.type;
            if (!Handler(type, input.button, input.mousePosition, input.delta, Screen.height, Screen.width)) return;
            if (type == EventType.MouseDown) GUIUtility.hotControl = control;
            if (type == EventType.MouseUp || type == EventType.Ignore) GUIUtility.hotControl = 0;
            input.Use();
        }
    }
}

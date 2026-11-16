// // ©2015 - Present Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System.Collections;
using SweetSugar.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.TargetScripts.TargetSystem
{
    /// <summary>
    /// target icon
    /// </summary>
    public class TargetGUI : MonoBehaviour
    {
        public Image image;
        public TextMeshProUGUI text;
        public GameObject check;
        public GameObject uncheck;
        public TargetCounter subTargetContainer;
        bool savedIconLayout;
        Vector2 iconPosition, iconSize, countPosition;
        bool iconPreserveAspect;
        Image.Type iconType;
        Coroutine arrivalPulse;
        Vector3 arrivalRestScale;
        bool arrivalPoseSaved;

        public void PlayCollectionArrival()
        {
            if(image==null || !isActiveAndEnabled)return;
            if(!arrivalPoseSaved) {arrivalRestScale=image.transform.localScale;arrivalPoseSaved=true;}
            if(arrivalPulse!=null)StopCoroutine(arrivalPulse);
            arrivalPulse=StartCoroutine(PulseArrival());
        }
        IEnumerator PulseArrival()
        {
            for(float age=0;age<.16f;age+=Time.deltaTime)
            {
                image.transform.localScale=arrivalRestScale*(1+.13f*Mathf.Sin(age/.16f*Mathf.PI));
                yield return null;
            }
            image.transform.localScale=arrivalRestScale;arrivalPulse=null;
        }
        void OnDisable()
        {
            if(arrivalPulse!=null)StopCoroutine(arrivalPulse);
            if(arrivalPoseSaved && image!=null)image.transform.localScale=arrivalRestScale;
            arrivalPulse=null;arrivalPoseSaved=false;
        }

        public void SetSprite(Sprite spr)
        {
            if (!savedIconLayout)
            {
                iconPosition = image.rectTransform.anchoredPosition;
                iconSize = image.rectTransform.sizeDelta;
                countPosition = text.rectTransform.anchoredPosition;
                iconPreserveAspect = image.preserveAspect;
                iconType = image.type;
                savedIconLayout = true;
            }
            image.rectTransform.anchoredPosition = iconPosition;
            image.rectTransform.sizeDelta = iconSize;
            text.rectTransform.anchoredPosition = countPosition;
            image.preserveAspect = iconPreserveAspect;
            image.type = iconType;
            image.sprite = spr;
            // A grama é um quadrado com margem transparente: sem preserveAspect ela esticava no retângulo do slot e ficava pequena.
            if (spr != null && spr.texture != null && spr.texture.name.StartsWith("grass"))
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                var side = Mathf.Max(iconSize.x, iconSize.y) * 1.25f;
                image.rectTransform.sizeDelta = new Vector2(side, side);
                image.rectTransform.anchoredPosition = iconPosition + new Vector2(0f, iconSize.y * 0.06f);
            }
            if (spr != null && spr.texture.name == "CM_pergaminho_abrir_sheet_v1")
            {
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                for (var parent = transform.parent; parent != null; parent = parent.parent)
                {
                    if (parent.name != "TopPanel") continue;
                    // Separate the open paper from its number within the cream panel.
                    // Always derive from the authored layout so rebinding cannot drift.
                    image.rectTransform.sizeDelta = iconSize * .78f;
                    image.rectTransform.anchoredPosition = new Vector2(0, iconPosition.y + iconSize.y * .13f);
                    text.rectTransform.anchoredPosition = new Vector2(0, countPosition.y - iconSize.y * .18f);
                    break;
                }
            }
        }

        void OnEnable()
        {
            check.SetActive(false);
            uncheck.SetActive(false);
            StartCoroutine(Check());
        }

        private void Update()
        {
            if(subTargetContainer != null)
                UpdateText();
        }

        private void UpdateText()
        {
            var count = subTargetContainer.GetCount(true);
            text.text = count.ToString();
        }

        IEnumerator Check()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);
                if (text.text == "0")
                {
                    check.SetActive(true);
                    uncheck.SetActive(false);
                    text.GetComponent<TextMeshProUGUI>().enabled = false;
                }
                else if (LevelManager.THIS.gameStatus == GameState.PreFailed || LevelManager.THIS.gameStatus == GameState.GameOver)
                {
                    check.SetActive(false);
                    uncheck.SetActive(true);
                    text.GetComponent<TextMeshProUGUI>().enabled = false;

                }
                else
                {
                    check.SetActive(false);
                    uncheck.SetActive(false);
                    text.GetComponent<TextMeshProUGUI>().enabled = true;

                }
            }
        }

        public static Vector2 GetTargetGUIPosition(string SpriteName)
        {
            var pos = Vector2.zero;
            var list = FindObjectsOfType(typeof(TargetGUI)) as TargetGUI[];
            foreach (var item in list)
            {
                if (item.image.GetComponent<Image>().sprite.name == SpriteName && item.gameObject.activeSelf)
                    return item.transform.position;
            }
            if (list.Length > 0) pos = list[0].transform.position;
            return pos;
        }

        public void BindTargetGUI(TargetCounter subTarget)
        {
            subTargetContainer = subTarget;
            subTargetContainer.BindGUI(this);
            text.text = subTargetContainer.GetCount().ToString();
        }
    }
}

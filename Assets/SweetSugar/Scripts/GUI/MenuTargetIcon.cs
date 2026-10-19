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

using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.GUI
{
    /// <summary>
    /// Target icon in the MenuPlay
    /// </summary>
    public class MenuTargetIcon : MonoBehaviour
    {
        public TextMeshProUGUI description;
        private Image[] images;

        private void Awake()
        {
            images = transform.GetChildren().Select(i => i.GetComponent<Image>()).ToArray();
        }

    void OnEnable()
    {
        DisableImages();
        if(!Application.isPlaying||LevelManager.THIS==null)return;
        var levelData = LoadingManager.LoadForPlay(Mathf.Max(1,PlayerPrefs.GetInt("OpenLevel",LevelManager.THIS.currentLevel)));
        var list = levelData.GetTargetSprites();
        var targets=levelData.GetTargetContainersForUI();
        if(description!=null)description.text=targets.Length>0?targets[0].targetLevel.GetDescription():"";
        for (int i = 0; i < Mathf.Min(list.Length,images.Length); i++)
        {
            if(images[i]==null)continue;
            images[i].sprite = list[i];
            images[i].gameObject.SetActive(true);
        }
    }

        private void DisableImages()
        {
            foreach (var item in images)
            {
                if(item!=null)item.gameObject.SetActive(false);
            }
        }

    }
}

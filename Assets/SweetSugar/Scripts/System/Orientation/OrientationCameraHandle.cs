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

using System;
using UnityEngine;

namespace SweetSugar.Scripts.System.Orientation
{
    /// <summary>
    /// Changes camera size depending from orientation and aspect ratio
    /// </summary>
    [ExecuteInEditMode]
    public class OrientationCameraHandle : MonoBehaviour
    {
        public Camera mainCamera;
        void OnEnable()
        {
            OrientationListener.OnOrientationChanged += OnOrientationChanged;
        }

        void OnDisable()
        {
            OrientationListener.OnOrientationChanged -= OnOrientationChanged;
        }
        void OnOrientationChanged(ScreenOrientation orientation)
        {
            if (mainCamera != null)
            {
                mainCamera.orthographicSize = 5.3f;
                mainCamera.orthographicSize =15f / Screen.width * Screen.height / 2f;
            }
        }

        [Serializable]
        public class OrientationRatio
        {
            public Vector2 ratio;
            public float cameraSize;
            public Vector2 cameraPosition;

        }
    }
}




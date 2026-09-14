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

using SweetSugar.Scripts.System;
using UnityEngine;

namespace SweetSugar.Scripts.Level
{
    /// <summary>
    /// Loading level 
    /// </summary>
    public static class LoadingManager
    {
        public static LevelData LoadForPlay(int currentLevel)
        {
            var levelData = new LevelData(Application.isPlaying, currentLevel);
            levelData = LoadlLevel(currentLevel, "Levels/Level_").DeepCopyForPlay(currentLevel);
            levelData.LoadTargetObject();
            levelData.InitTargetObjects(true);
            return levelData;
        }

        public static LevelData LoadlLevel(int currentLevel, string path)
        {
            var levelData = ScriptableLevelManager.LoadLevel(currentLevel, path);
            levelData.CheckLayers();
            levelData.LoadTargetObject();

            return levelData;
        }


        public static int GetLastLevelNum()
        {
            return Resources.LoadAll<LevelContainer>("Levels").Length;
        }
    }
}


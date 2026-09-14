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

using SweetSugar.Scripts.Level;
using UnityEditor;
using UnityEngine;

namespace SweetSugar.Scripts.System
{
    public static class ScriptableLevelManager
    {
        #if UNITY_EDITOR
        public static void CreateFileLevel(int level, LevelData _levelData)
        {
            var path = "Assets/SweetSugar/Resources/Levels/";

            if (Resources.Load("Levels/Level_" + level))
            {
                SaveLevel(path, level, _levelData);
            }
            else
            {
                string fileName = "Level_" + level;
                var newLevelData = ScriptableObjectUtility.CreateAsset<LevelContainer>(path, fileName);
                newLevelData.SetData(_levelData.DeepCopy(level));
                EditorUtility.SetDirty(newLevelData);
                AssetDatabase.SaveAssets();
            }
        }
        public static void SaveLevel(string path, int level, LevelData _levelData)
        {
            var levelScriptable = Resources.Load<LevelContainer>("Levels/Level_" + level);
            if (levelScriptable != null)
            {
                levelScriptable.SetData(_levelData.DeepCopy(level));
                EditorUtility.SetDirty(levelScriptable);
            }

            AssetDatabase.SaveAssets();
        }
        #endif

        public static LevelData LoadLevel(int level, string path)
        {
            var levelScriptable = Resources.Load<LevelContainer>(path + level);
            return levelScriptable.levelData.DeepCopy(level);
        }
    }
}
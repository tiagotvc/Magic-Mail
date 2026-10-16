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
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;

namespace SweetSugar.Scripts.TargetScripts.TargetEditor.Editor
{
    public static class TargetEditorUtils
    {
        private static TargetEditorScriptable target;

        // Display labels only: internal names and saved target indices remain stable.
        public static string DisplayName(string name)
        {
            if (name == "SugarSquare") return "Grama";
            if (name == "IceBlock") return "Gelo";
            if (name == "MachineLetters") return "Cartas da franqueadora";
            if (name == "SolidBlock") return "Caixa (SolidBlock)";
            return name;
        }

        public static TargetEditorScriptable TargetEditorScriptable
        {
            get
            {
                if (target == null)
                {
                    target = AssetDatabase.LoadAssetAtPath<TargetEditorScriptable>("Assets/SweetSugar/Resources/Levels/TargetEditorScriptable.asset");
                }

                return target;
            }
        }

        public static TargetContainer GetTargetContainer(SerializedProperty property)
        {
            var propertyParent = PropertyUtils.GetParent(property) as TargetObject;
            return TargetEditorScriptable.GetTargetbyName(propertyParent.targetType.GetTarget().name);
        }
    }
}

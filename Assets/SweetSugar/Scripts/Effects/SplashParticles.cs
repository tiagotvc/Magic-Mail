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

using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;
using UnityEngine;

namespace SweetSugar.Scripts.Effects
{
    /// <summary>
    /// Simple item explosion effect
    /// </summary>
    [ExecuteInEditMode]
    public class SplashParticles : MonoBehaviour
    {
        float index;
        ParticleSystem ps;
        public GameObject attached;
        private Sprite[] sprs;

        // Correio Mágico: the match burst throws small copies of the piece that was matched. The 7 pieces sit side by
        // side in Resources/pieces-strip (colour order), because a particle texture sheet needs them in one texture.
        const int PieceCount = 7;
        static Texture2D pieceStrip;
        bool usePieces;

        private void OnEnable()
        {
            ps = GetComponent<ParticleSystem>();
            if(name == "FireworkSplash(Clone)")
            {
                if (pieceStrip == null) pieceStrip = Resources.Load<Texture2D>("pieces-strip");
                usePieces = pieceStrip != null && Application.isPlaying;
                if (usePieces)
                {
                    UsePieceStrip();
                    return;
                }
                var prefab = Resources.Load<Item>("Items/Item");
                sprs = prefab.GetComponent<IColorableComponent>().GetSpritesOrAdd(LevelManager.THIS.currentLevel);
                var sheet = ps.textureSheetAnimation;
                for (var index = 0; index < sprs.Length; index++)
                {
                    // The effect ships with one sprite slot per Sweet Sugar colour (6); Correio Mágico has 7 pieces.
                    if (index < sheet.spriteCount) sheet.SetSprite(index, sprs[index]);
                    else sheet.AddSprite(sprs[index]);
                }
            }
        }

        void UsePieceStrip()
        {
            var material = GetComponent<ParticleSystemRenderer>().material; // an instance for this pooled effect
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", pieceStrip);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", pieceStrip);
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = PieceCount;
            sheet.numTilesY = 1;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.startFrame = 0;
            var main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startColor = Color.white;
        }

        public void SetColor(int index_)
        {
            var textSheet = ps.textureSheetAnimation;
            if (usePieces)
            {
                // frameOverTime is normalised over the sheet: a constant keeps every particle on the matched piece.
                textSheet.frameOverTime = new ParticleSystem.MinMaxCurve((Mathf.Clamp(index_, 0, PieceCount - 1) + 0.5f) / PieceCount);
            }
            else
            {
                index = index_+1;
                if(sprs is { Length: > 0 })
                    textSheet.startFrame = index / sprs.Length;
            }
            ps.Play();
        }

        private void Update()
        {
            if (attached != null)
                transform.position = attached.transform.position;
        }


    }
}

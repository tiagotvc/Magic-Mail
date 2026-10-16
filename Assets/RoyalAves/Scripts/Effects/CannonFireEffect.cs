using System;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.LeanTween.Framework;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Correio Mágico: o canhão (boost ExplodeArea) agora tem visual próprio - aparece parado na base da coluna
    // tocada e atira pra cima, em vez de reaproveitar o foguete partido ao meio (RocketSplitEffect), que não faz
    // sentido pra um canhão que não se move. Sem bala de canhão por enquanto (a pedido do usuário) - o "tiro" é só
    // o flash/fumaça do cano, e as peças da coluna estouram em cascata de baixo pra cima, cronometradas com a
    // animação do disparo. Reaproveita os mesmos efeitos/sons de destruição de peça que o resto do jogo já usa -
    // só a parte visual "o que aparece e atira" é nova.
    public static class CannonFireEffect
    {
        static CannonFireEffectData _data;

        static CannonFireEffectData Data
        {
            get
            {
                if (_data == null) _data = Resources.Load<CannonFireEffectData>("Effects/CannonFireEffectData");
                return _data;
            }
        }

        // Same convention as RocketSplitEffect: board items are scaled 0.42 (Square.cs) times ~0.88 (resting sprite
        // scale) by the normal spawn pipeline - this effect isn't spawned through that pipeline. The cannon prop
        // itself is deliberately bigger than a board item though (a stationary gun sitting below the column reads
        // better large), so it gets its own multiplier instead of reusing RocketSplitEffect's RestScale.
        // 0.4 was the original size; +20% ("muito pequeno") then another +20% on top.
        const float CannonScale = 0.576f;

        const float IdleFrameTime = 0.07f;
        const float IdleDuration = 0.22f; // ~3 idle frames shown before firing
        const float FireFrameTime = 0.08f;
        const float TailLinger = 0.3f; // keeps the last smoke frame visible a beat before the prop disappears

        const float HalfPixelsPerUnit = 128f; // matches cannon-idle/fire-*.png's import setting

        // Correio Mágico: cada frame do spritesheet precisa de uma margem de canvas diferente (os frames de tiro
        // têm fumaça que se espalha bem mais que o frame parado), então o canvas de cada PNG não tem o mesmo
        // tamanho relativo ao cano - o centro do canvas (o pivô do Sprite) não coincide com a ponta do cano. Esse
        // valor, calibrado junto com a exportação da arte (ver histórico), é a distância real em pixels do pivô até
        // a ponta do cano, pra calcular a posição sem depender de sprite.bounds.extents (que mede o canvas inteiro,
        // não o cano). Se a arte for reexportada/recortada de novo, esse número precisa ser recalibrado.
        const float MuzzleOffsetPx = 53f;

        // squares: the column's squares, any order. onCellPassed: called once per square, timed bottom-to-top
        // across the firing animation, so pieces pop in a cascade instead of all at once. Returns how long the
        // whole effect takes, or 0 when the art isn't set up (caller then has to handle the cells itself).
        public static float Play(IList<Square> squares, Action<Square> onCellPassed)
        {
            if (squares == null || squares.Count == 0) return 0f;

            var data = Data;
            if (data == null)
            {
                // No art set up - destroy the column right away instead of silently doing nothing (the caller has
                // no fallback of its own here, unlike StripedShowPass's rocket beam).
                foreach (var sq in squares) onCellPassed(sq);
                return 0f;
            }

            var ordered = squares.OrderBy(s => s.transform.position.y).ToList();

            var idleSprites = ToSprites(data.IdleFrames);
            var fireSprites = ToSprites(data.FireFrames);
            if (idleSprites.Length == 0 && fireSprites.Length == 0)
            {
                foreach (var sq in ordered) onCellPassed(sq);
                return 0f;
            }

            // The art faces right by default and gets rotated +90 below so the muzzle ends up on top.
            // MuzzleOffsetPx (not sprite.bounds, which measures the whole padded canvas, not the cannon itself) is
            // the real pivot-to-muzzle distance, so this places the sprite with the muzzle tip resting right on the
            // column's bottom edge - not floating below it, not poking into the board.
            var halfReach = MuzzleOffsetPx / HalfPixelsPerUnit * CannonScale;
            var columnBottomEdge = ordered[0].transform.position.y - LevelManager.THIS.squareWidth * 0.5f;
            var basePos = new Vector3(ordered[0].transform.position.x, columnBottomEdge - halfReach,
                ordered[0].transform.position.z);

            var root = new GameObject("CannonFireFX");
            root.transform.position = basePos;
            root.transform.rotation = Quaternion.Euler(0, 0, 90); // art faces right by default; column needs up
            root.transform.localScale = Vector3.one * CannonScale;

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            sr.sortingOrder = PowerUpEffectOrder.Value;

            PlayFrames(sr, idleSprites, IdleFrameTime);

            var fireDuration = fireSprites.Length > 0 ? fireSprites.Length * FireFrameTime : 0.3f;
            LeanTween.delayedCall(IdleDuration, () => PlayFrames(sr, fireSprites, FireFrameTime));

            ScheduleCells(ordered, IdleDuration, fireDuration, onCellPassed);

            var total = IdleDuration + fireDuration + TailLinger;
            UnityEngine.Object.Destroy(root, total);
            return IdleDuration + fireDuration;
        }

        static Sprite[] ToSprites(Texture2D[] textures)
        {
            if (textures == null) return Array.Empty<Sprite>();
            return textures.Where(t => t != null)
                .Select(t => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), HalfPixelsPerUnit))
                .ToArray();
        }

        // Cycles a SpriteRenderer through a frame sequence once, one LeanTween.delayedCall per frame (same
        // no-MonoBehaviour-needed approach as the rest of this effect).
        static void PlayFrames(SpriteRenderer sr, Sprite[] frames, float frameTime)
        {
            if (sr == null || frames.Length == 0) return;
            for (var i = 0; i < frames.Length; i++)
            {
                var sprite = frames[i];
                LeanTween.delayedCall(i * frameTime, () =>
                {
                    if (sr != null) sr.sprite = sprite;
                });
            }
        }

        // Cells pop bottom-to-top, spread evenly across the firing animation's duration - no literal projectile to
        // time against (there's no cannonball yet), so a simple linear spread sells the "shot travelling up the
        // column" feeling well enough.
        static void ScheduleCells(List<Square> ordered, float idleDuration, float fireDuration, Action<Square> onCellPassed)
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                var square = ordered[i];
                var t = ordered.Count > 1 ? idleDuration + fireDuration * i / (ordered.Count - 1) : idleDuration;
                if (t <= 0f) onCellPassed(square);
                else LeanTween.delayedCall(t, () => onCellPassed(square));
            }
        }
    }
}

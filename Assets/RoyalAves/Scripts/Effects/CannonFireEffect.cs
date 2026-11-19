using System;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Correio Mágico: o canhão (boost Cannon) aparece na base da coluna tocada, acende o pavio, dispara com coice e solta
    // uma bala de canhão em chamas mágicas que atravessa a coluna de baixo para cima; cada peça estoura quando a bala
    // chega nela. Esta classe é só a porta de entrada que o jogo chama (LevelManager.DestroyColumnWithCannon); a sequência
    // em si roda no CannonFireRunner, com todos os ajustes no asset Resources/Effects/CannonFireEffectData.
    // Nada aqui decide quais peças são destruídas: o jogo passa as casas e o que fazer com cada uma.
    public static class CannonFireEffect
    {
        public const string ResourcePath = "Effects/CannonFireEffectData";

        static CannonFireEffectData _data;
        static readonly List<CannonFireRunner> runners = new List<CannonFireRunner>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _data = null;
            runners.Clear();
        }

        public static CannonFireEffectData Data
        {
            get
            {
                if (_data == null) _data = Resources.Load<CannonFireEffectData>(ResourcePath);
                return _data;
            }
        }

        // squares: as casas da coluna, em qualquer ordem. onCellPassed: chamado uma vez por casa, de baixo para cima, no
        // instante em que a bala chega nela. Devolve o tempo até a última casa ser atingida (o resto do efeito termina
        // sozinho), ou 0 quando a arte não está configurada (aí as casas são processadas na hora).
        public static float Play(IList<Square> squares, Action<Square> onCellPassed)
        {
            if (squares == null || squares.Count == 0) return 0f;
            var ordered = squares.Where(s => s != null).OrderBy(s => s.transform.position.y).ToList();
            if (ordered.Count == 0) return 0f;

            var data = Data;
            if (data == null || !data.HasCannonArt)
            {
                // Sem arte: destrói a coluna na hora em vez de não fazer nada (quem chama não tem outro plano).
                SoundBase.Instance?.PlayOneShot(SoundBase.Instance.strippedExplosion);
                foreach (var sq in ordered) onCellPassed(sq);
                return 0f;
            }

            CannonFireRunner runner = null;
            try
            {
                runner = RentRunner();
                return runner.Begin(data, ordered, onCellPassed);
            }
            catch (Exception e)
            {
                // Qualquer falha ao montar o visual não pode deixar a coluna sem ser destruída nem o jogo travado.
                Debug.LogError("Efeito do canhão falhou ao iniciar; destruindo a coluna sem animação. " + e);
                if (runner != null) runner.Cancel();
                foreach (var sq in ordered)
                    if (sq != null) onCellPassed(sq);
                return 0f;
            }
        }

        /// Só para a janela de preview do editor: o canhão num tabuleiro de mentira (positions de baixo para cima).
        /// Quem chama avança o tempo com Step e destrói o objeto no fim. null = arte não configurada.
        public static CannonFireRunner CreatePreview(IList<Vector3> positions, IList<Color> colors, Action<int> onHit, Camera camera)
        {
            var data = Data;
            if (data == null || !data.HasCannonArt || positions == null || positions.Count == 0) return null;
            var runner = new GameObject("CannonFirePreview").AddComponent<CannonFireRunner>();
            runner.BeginPreview(data, positions, colors, onHit, camera);
            return runner;
        }

        /// Interrompe qualquer canhão em andamento sem destruir as peças que faltavam (troca de fase, volta ao mapa).
        public static void CancelAll()
        {
            foreach (var runner in runners)
                if (runner != null) runner.Cancel();
        }

        // Reaproveita um executor parado (com todos os sprites e malhas já criados); só cria outro se o anterior ainda
        // estiver terminando o rastro quando o jogador usa um segundo canhão.
        static CannonFireRunner RentRunner()
        {
            runners.RemoveAll(r => r == null);
            foreach (var runner in runners)
                if (!runner.IsRunning) return runner;
            var go = new GameObject("CannonFireFX");
            var created = go.AddComponent<CannonFireRunner>();
            runners.Add(created);
            return created;
        }
    }
}

# Caixa cartoon v3 — 22/10/2026

Arte aprovada aplicada aos três sprites `Assets/RoyalAves/Art/Obstacles/ParcelStack/CM_pilha_estado01_00.png` até `CM_pilha_estado03_00.png`, preservando os GUIDs. Folha original: `Docs/Art/caixa-cartoon-v3-duas-tabuas.png`. Recorte sem alteração dos pixels ou do canal alpha, em três células de 724×724.

O prefab SolidBlock possui três resistências: base, uma diagonal e duas diagonais em X. A quarta camada foi removida, juntamente com seu sprite e referências de meta. As fases antigas que ainda pedem quatro camadas são limitadas a três durante a criação da caixa, tanto no formato em camadas quanto no formato legado de bloco ou obstáculo. A meta continua sendo uma unidade por caixa e é coletada somente na quebra final.

Importação e encaixe recalculados para corpo de 1,72 unidades centralizado em todos os estados. Recorte da tábua que cai ajustado à nova espessura e posição dos pregos; o X perde uma diagonal e depois a outra. Laboratório e painel atualizados para três caixas, com resistências 1, 2 e 3 e meta 3.

Backup anterior em `Backups/WoodenCrate-before-cartoon-v3/`. Preparação reproduzível em `Tools/apply_crate_cartoon_v3.py`.

Verificação dos arquivos em `Logs/crate-v3-assets.result`: pixels aprovados preservados, transparência, GUIDs, referências internas do prefab, encaixe e alinhamento, ausência de referências à quarta arte e laboratório com três caixas. Compilação dos projetos de runtime e editor via MSBuild registrada em `Logs/crate-v3-build.log`. Validação visual em Play Mode ainda não realizada nesta aplicação. Validador no Unity: `Royal Aves/Elementos/Pilha de Encomendas/Validar arte e regras das caixas`.

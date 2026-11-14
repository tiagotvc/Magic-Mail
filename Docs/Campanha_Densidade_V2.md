# Campanha: concentração dos elementos — V2

O usuário apontou que o nível 103 misturava elementos demais e os deixava espalhados. Esta revisão substitui a distribuição V1 dos 200 tabuleiros por fases com uma mecânica principal repetida e concentrada.

- **150 fases com um tipo de elemento; 50 com dois.** Parte das fases com dois tipos é fita com grama, porque a fita é somente bloqueio e precisa de uma meta real. Misturas de apoio ficam restritas a desafios selecionados.
- **Nível 103:** antes duas pedras, seis ovos e uma bolsa espalhados; agora **seis bolsas contíguas**, numa mesma faixa, sem outro tipo de obstáculo. Meta compartilhada de **18 pergaminhos**, com **27 jogadas**.
- **Toda fase com bolsa tem pelo menos três bolsas.** A estreia usa três; fases posteriores usam quatro a sete, com a quota compartilhada dimensionada para o grupo. Cada bolsa conserva um lado de acesso para combinações; não se cria uma bolsa encerrada por outras estações permanentes.
- Caixas, grama, gelo, pedras, tinteiros, sinalizadores e ovos crescem em manchas/faixas a partir de uma posição, em vez de ocupar pontos independentes. Cofres, poções e escaninhos repetem grupos próximos respeitando a ocupação 2×2 e a separação exigida pelo escaninho.
- Reduzidos os power-ups já colocados no início. Eles ficam nas demonstrações dos especiais e nas estreias dos obstáculos que exigem especiais. O restante é formado durante a partida.
- Jogadas recalculadas para a maior quantidade de elementos, com teto de 42. Dificuldade continua sendo estimativa de autoria, sujeita a partidas completas.

As 18 apresentações, ordem das estreias, regras, artes, animações, prefabs, catálogo global de metas e progresso salvo permanecem os mesmos. Fita continua sem meta. Não há novo elemento nem mudança nas regras da bolsa.

## Arquivos

- Gerador: `Tools/level-map/build_campaign_200.py`.
- Receitas, planilha e galeria atualizadas: `Tools/level-map/campaign-200/`.
- Evidência própria desta revisão: `Tools/level-map/campaign-200/evidence-density-v2/`.
- Backup integral da distribuição anterior e dos respectivos metadados: `Backups/campaign-density-v2-2026-11-13/`.
- As evidências V1 continuam em `evidence/`; não descrevem a distribuição atual.

O validador estrutural verifica os 200 arquivos. A rodada nativa usa 15 fases representativas, incluindo 103, estreia da bolsa, poções, cofre, escaninho, gelo duplo, sinalizador, fita e nível 200; confere metas reais, apresentações aplicáveis e uma troca inicial por fase. Capturas são da câmera do tabuleiro, sem HUD completo. Isso não é prova de 200 vitórias, taxa de sucesso, entrada física, dispositivo ou áudio.

Resultado final: **PASS** nos 200 níveis e nos 15 casos nativos. São 214 grupos de metas no total. Os 400 assets de campanha ficaram idênticos aos usados na cópia isolada, e todos os arquivos de runtime comparados também coincidiram. Foram preservados os 402 metadados da campanha anterior e os 389 arquivos de produção protegidos. Compilação nativa do principal e da cópia sem erros C#. Auditoria reproduzível: `python Tools/level-map/audit_campaign_density_v2.py`.

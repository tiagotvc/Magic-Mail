# Fases 4 a 10 — primeiro lote aplicado

REGRA VIGENTE (28/10/2026): desafios a cada cinco fases. Parametros atuais de 1-10 em [Campanha_Ciclos_de_Cinco.md](Campanha_Ciclos_de_Cinco.md); essa revisao substitui os movimentos, alvos, camadas e papeis historicos abaixo.

23/10/2026. Sete fases gravadas nos assets reais da campanha e suas metas. GUIDs e arquivos `.meta` preservados. Fases 1–3 preservadas. A montagem segue `Progressao_Campanha.md` e a planilha de autoria.

| Fase | Tabuleiro | Jogadas | Grama (células) | Camadas da grama | Caixas |
| --- | --- | --- | --- | --- | --- |
| 4 | 6×6 | 26 | 6 | 1 | 0 |
| 5 | 6×6 | 25 | 9 | 1 | 0 |
| 6 | 6×6 | 27 | 8 | 1 | 4 |
| 7 | 7×7 | 28 | 6 | 1 | 4 |
| 8 | 6×6 | 28 | 6 | 2 | 0 |
| 9 | 7×7 | 26 | 10 | 1 | 8 |
| 10 | 7×7 | 30 | 8 | 1 | 6 |

Todas as caixas têm uma camada. Todas as fases usam quatro peças (IDs 0, 1, 2, 3), preenchimento inicial aleatório, reposição no topo e apenas as metas dos obstáculos presentes. Metas antigas de estrelas, peças e ingredientes foram retiradas dessas sete fases. Nenhum especial é fixado no começo. O roteiro não adiciona novos avisos de tutorial nesta entrega.

A fase 4 apresenta grama em uma faixa central; 5 amplia a área; 6 combina caixas laterais com piso central; 7 oferece respiro com centro amplo; 8 repete o desenho de 4 com grama resistente; 9 tem duas barreiras parciais com passagens; 10 oferece espaço e 30 jogadas para consolidar.

## Conferência

Executado `python Tools/level-map/build_campaign_4_10.py`: conferidos quantidade e posição das células, dimensões, camadas, tipos, entradas no topo, paleta, jogadas, metas e referências de arte existentes. Relatório: `Logs/campaign-4-10-validation.json`. `git diff --check` sem erros.

Esta é validação estática dos dados. Importação nativa, visual, partidas completas e dificuldade ainda precisam de conferência no Unity. A existência de processos Unity não disponibilizou uma sessão de controle conectada para executar partidas nesta entrega.

No Level Editor, abrir os níveis 4 a 10 normalmente. O menu `Royal Aves > Ir para o nível...` também permite acesso pelo lobby, mas altera o progresso salvo; usar somente no perfil de teste.

## Reprodução e restauração

Ferramenta de montagem: `Tools/level-map/build_campaign_4_10.py`. Executar novamente reaplica este lote; `--check` valida sem reconstruir. A montagem preserva o primeiro backup e usa sua versão original como template.

Originais dos 14 assets em `Backups/Campaign-4-10-before-v1/`, com a mesma estrutura relativa à pasta `Assets/SweetSugar/Resources/Levels`. Para restaurar, copiar os `.asset` correspondentes de volta, mantendo os `.meta` atuais.


## Revis?o de formas ? 28/10/2026

Substitu?dos os sete tabuleiros preenchidos por silhuetas distintas. Estes valores substituem as dimens?es e descri??es da primeira vers?o acima:

| Fase | Forma | Linhas ? colunas | C?lulas ativas |
| --- | --- | --- | --- |
| 4 | Cantos recortados | 6?6 | 32 |
| 5 | Leque | 6?7 | 36 |
| 6 | Ampulheta | 7?7 | 41 |
| 7 | Faixa horizontal recortada | 5?9 | 41 |
| 8 | Cruz | 7?7 | 33 |
| 9 | H | 7?7 | 45 |
| 10 | Oct?gono largo | 7?9 | 51 |

Pr?via esquem?tica: [sete formas](Campanha_Formas_4_a_10.svg). As ?reas cortadas s?o c?lulas NONE reais, sem piso ou pe?as. Cada in?cio de segmento vertical recebe uma entrada de reposi??o, incluindo bordas abaixo de recortes. Mantidos movimentos, quantidades de alvos e camadas; a apresenta??o da grama continua no fluxo da fase 4. A mudan?a de geometria exige novo playtest de dificuldade.

O gerador agora suporta matrizes retangulares, c?lulas ausentes e entradas por segmento. A valida??o est?tica passou para as sete fases: metas, camadas, posi??es, aus?ncia de entrada em buracos, uma regi?o conectada e abastecimento por segmento vertical. Isso n?o comprova o comportamento da gravidade e da moldura em Play. Backup imediatamente anterior ? revis?o em `Backups/Campaign-4-10-before-shapes-v2/`; backup original tamb?m preservado.

### Pesquisa e aplica??o

Consultados materiais de Royal Match, Royal Kingdom e Candy Crush. As formas desta revis?o s?o propostas pr?prias; n?o reproduzem n?veis espec?ficos nem uma f?rmula interna dos jogos.

- [Royal Match ? capturas de gameplay](https://gameapp.last-dragon.work/zroyalmatch.html).
- [Royal Kingdom ? capturas e an?lise de design](https://www.funconomy.com/post/unpacking-royal-kingdom-s-design).
- [Candy Crush ? exemplos por desenho do tabuleiro](https://candycrush.fandom.com/wiki/Category:Levels_by_board_design).
- [King / GDC ? Level Design Saga](https://media.gdcvault.com/gdceurope2016/presentations/KANG_Jeremy_LevelDesignSaga.pdf): refer?ncia localizada; PDF n?o foi inspecionado nesta sess?o.

Diretriz adotada: variar silhueta, largura ?til, acesso aos alvos e caminhos de queda ao planejar o desafio. Ret?ngulos continuam v?lidos; a campanha deve altern?-los com outras geometrias, evitando repetir o mesmo contorno por todo um bloco. Em fases de ensino, preservar espa?o para compreender o elemento antes de apertar os caminhos.

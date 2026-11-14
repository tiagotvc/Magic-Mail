# Seis elementos com acabamento 3D

Aplicação autorizada em 13 de novembro de 2026 após a prévia animada.

O novo modelo e as animações foram instalados na **caixa de madeira, pedra rúnica, tinteiro, poções, bolsa do carteiro e sinalizador**. Os seis continuam disponíveis no Level Editor. Cofre, escaninho, fita e power-ups permanecem com seus modelos atuais.

- Caixa: três estados novos, retirada das tábuas e quebra em fragmentos sólidos de madeira.
- Pedra: nova arte e fragmentação sólida; combinações comuns continuam sem causar dano.
- Tinteiro: cinco estados, cacos violetas/dourados e respingos arredondados com luz suave. Os pontos de saída da tinta usam a nova geometria; a quinta batida continua removendo a peça e pintando sua célula.
- Poções: suporte e quatro frascos independentes; cada frasco produz vidro e líquido de sua cor. O suporte quebra quando o último frasco termina. A ocupação 2×2 e as regras por cor/especial permanecem.
- Bolsa: nova sequência sem alça, base registrada, abertura única, pergaminho desenrolando e voo com inclinação suave, fechamento. Continua fixa; a meta só é descontada na chegada ao HUD e respeita a quantidade reservada entre bolsas.
- Sinalizador: corpo cobre, lente e símbolos das cores, três resistências, fragmentos sólidos. Mantém rejeição da cor errada e avanço da cor por jogada.

Os efeitos novos são selecionados pelo componente `Soft3DElementStyle`, apenas nos seis elementos. Os materiais e sprites ficam em `Assets/RoyalAves/Resources/Soft3D/`. Os PNGs são cópias byte a byte das propostas; o shader aplica a política de transparência em tempo de renderização. Os fragmentos são malhas com chanfro e espessura; gotas usam luz suave sem contorno escuro.

Fases antigas mantêm seus arquivos e quantidades. `Soft3DTargetVisuals` resolve os novos ícones em memória e mantém os sprites antigos como aliases de compatibilidade. Não é necessário regravar fases. Os menus de preparação dos laboratórios preservam a remodelagem instalada; o registro dos três últimos elementos não devolve os pergaminhos antigos à bolsa.

Validação nativa em projeto isolado: brushes reais do editor, salvar/reimportar/carregar, dois suportes de poções adjacentes, metas automáticas e cota manual da bolsa, match por API, quebras e liberação das células, três camadas da caixa contadas uma vez, pedra rejeitando dano comum, cinco impactos do tinteiro, rejeição de diagonal, entrega da bolsa apenas na chegada e encerramento na cota, limpeza dos efeitos e overlays. Os grupos de metas foram testados em sequência para respeitar o limite atual do HUD. A cópia de teste usa o nível 1903, que **não foi instalado no projeto principal**.

Compilação de runtime/editor sem erros. Capturas nativas do tabuleiro e resultado do teste em `Tools/royal-postman/production/review/six-elements-soft3d-v1/`. O manifesto documenta a preservação dos IDs existentes dos prefabs, da ordem/comportamento das metas, dos outros elementos e dos arquivos de campanha.

Limites: testes por APIs e renderização nativa; sem validação de toque físico, dispositivo, áudio ou fluxo completo de campanha. Não houve alteração de recompensas, economia ou progresso. A apreciação estética no uso real continua com o usuário.

Backup: `Backups/six-elements-soft3d-2026-11-13/`.

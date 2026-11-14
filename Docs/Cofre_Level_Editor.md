# Cofre 2×2 no Level Editor

09/11/2026. Integração da animação turquesa aprovada pelo usuário.

**Aceite final do elemento — 09/11/2026:** o usuário confirmou "elemento finalizado com sucesso, testei de todas as formas, ficou legal" após a correção da hélice. Cofre concluído e aprovado, incluindo funcionamento 2×2, animação de quebra e ordem visual. Os limites das verificações automatizadas registrados abaixo são históricos e separados desse teste/aceite do usuário. Inserção em fases da campanha permanece em etapa própria.

## Colocar e apagar

Abra **Sweet Sugar → Level editor → Blocks** e selecione **Cofre 2×2**, pelo botão com nome ou pelo ícone na paleta. Clique na célula que será o canto superior esquerdo: o editor reserva quatro células. O desenho aparece uma única vez, cobrindo o grupo inteiro.

Clique em qualquer quadrante com o pincel de cofre ou com a borracha para remover o grupo. Colocar outro bloco sobre um quadrante primeiro remove o cofre inteiro. O piso de grama abaixo é preservado. A operação não permite colocar um grupo cortado na borda, sobre buracos, teleportes ou outros obstáculos. Fill+ preenche grupos inteiros e, no próximo uso, remove os cofres do campo. Peças fixas não podem ser pintadas dentro do cofre.

Salvar valida origem, dimensões, referências dos quatro quadrantes e sobreposições. Se houver um grupo incompleto, o salvamento é recusado e o rascunho continua marcado como alterado.

## Funcionamento

- Um objeto fixo de 2×2: não cai, não troca e bloqueia entrada de peças nas quatro células.
- Cinco acertos diretos de especiais, compartilhados entre os quadrantes. Matches encaminhados dos vizinhos só fazem a reação sem dano.
- Proteção de 0,2 s contra impactos imediatamente repetidos. Reação sem dano não consome a proteção de um especial válido.
- Mantém os cinco estados e a quebra por fragmentos aprovada, com moedas independentes.
- O quinto acerto despedaça a estrutura e libera as quatro células. A meta conta **um cofre**, uma única vez, na quebra final.
- As moedas são efeito visual e não alteram o saldo. A vitória aguarda a conclusão do efeito registrado em `animateItems`.
- Ordem visual corrigida após relato do usuário: cofre na camada `Item mask`, ordem 90; fragmentos/moedas na 93; hélice em voo na 100. A inicialização corrige também renderers já serializados em prefabs antigos, evitando empate com o power-up.

## Dados e arquivos

`SquareTypes.Safe` foi acrescentado no índice **17**, preservando os anteriores. Meta `Safe` acrescentada ao fim do catálogo com quantidade automática pelo tabuleiro. A contagem considera as origens dos grupos, inclusive antes de o tabuleiro existir.

Prefab: `Assets/SweetSugar/Resources/Blocks/Safe.prefab`. Usa o suporte nativo a blocos maiores: origem com `anotherSquare=false`, três referências com `anotherSquare=true`, `originalPos` comum e `size=(2,2)`. Runtime compartilha uma única camada `Square` entre as quatro células.

`SafeFootprint` concentra pintura, apagamento, contagem e validação. `SafeCell` encaminha impactos da camada compartilhada ao `SafeBoardController`, que controla a animação, conclusão e meta. Arte e animação existentes são reutilizadas; não houve geração de nova arte nem colocação de cofres nas fases da campanha.

Backups dos arquivos existentes: `Backups/safe-editor-2026-11-09/`. Registro e hashes: `Tools/royal-postman/production/SAFE-EDITOR-V1.json`.

## Verificações

Runtime/editor atuais compilados sem erros. Verificação nativa em Unity 6000.6.0f1, cópia isolada `Logs/InkwellValidationProject`, com saída 0. As preferências e fixtures dessa cópia são separadas; nenhum resultado/recompensa foi concedido no projeto principal.

Authoring: prefab/BindLayer/ícone, enum estável, pincel real invocado por método, quatro células para um cofre, dois grupos encostados, JSON save/load, apagamento por quadrante, rejeição atômica de borda e buraco, preservação de grama e detecção de grupo incompleto.

Tabuleiro real: dois cofres encostados com quatro células bloqueadas e sem peças em cada grupo; visual com largura de 1,92 células; meta inicial de dois; reação encaminhada de match sem dano; **um foguete real** aplicando um acerto por cofre; demais acertos diagnósticos distribuídos pelos quadrantes; rejeição de repetição imediata; contagem apenas na quebra; liberação conjunta das quatro células; vizinho preservado; limpeza do bloqueio de animação. Capturas nativas inicial, após foguete e durante a quebra foram inspecionadas.

Evidências: `Tools/royal-postman/production/review/safe-editor-v1/`. Não testados: ponteiro físico no editor/jogo, partida completa, todas as combinações de especiais, áudio, aparelho e fluxo completo vitória → Continue. As chamadas diagnósticas não são apresentadas como esses testes.

# Pilha de Encomendas — revisão para pacote único

Direção atual definida pelo usuário: um pacote único com quatro estados de embrulho, mantendo o tamanho enquanto recebe dano. Essa decisão substitui a proposta de diminuir a quantidade de caixas do guia original.

Revisão atual: o papel permanece fechado em todos os estados. A progressão passa a remover selo e fitas, terminando no pacote em papel pardo. Detalhes e prompts em `Pilha_Estados_Selo_Fitas.md`.

## Regras iniciais

- Ocupa uma célula fixa e bloqueia entrada/saída de peças enquanto houver camadas.
- Resistência configurável de 1 a 4; cada acerto aceito remove uma camada.
- Usa o caminho existente de matches adjacentes ortogonais e especiais do `SolidBlock`.
- O último acerto inicia uma animação de conclusão com duração aproximada de 0,62 segundo. A célula permanece bloqueada durante o efeito; ao terminar, o pacote fechado desaparece, a célula é liberada e a meta diminui em uma unidade.
- Cada pacote conta como um único objetivo, independentemente da resistência. Quatro pacotes com vidas 1, 2, 3 e 4 totalizam **4 objetivos**, exigindo até 10 impactos. Dano intermediário não reduz a meta.
- A proteção existente contra dano repetido usa uma janela de 0,2 segundo. Ela foi preservada: deduplicação por ativação e combos simultâneos precisam de playtest antes de homologar a regra final.

## O que foi implementado

O prefab `SolidBlock` mantém seu nome, enum e referências existentes e ganhou a quarta camada. Agora usa quatro PNGs próprios de `Assets/RoyalAves/Art/Obstacles/ParcelStack`: selo e duas fitas (4), duas fitas (3), uma fita horizontal (2), pacote fechado em papel pardo (1). O contador numérico foi removido. `ParcelStackLayer` desenha apenas o estado exposto, evitando sobrepor as silhuetas completas das camadas inferiores. A arte e os prompts atuais estão em `Pilha_Estados_Selo_Fitas.md`.

Os quatro PNGs são RGBA de 1254×1254 com transparência real. O importador geral tinha sobrescrito a escala dos novos arquivos com 128 pixels por unidade, causando a extrapolação da célula. A pasta ParcelStack agora possui regra própria persistente: 660 pixels por unidade, pivô central e canvas de 1,9 unidades locais. A apresentação normaliza o corpo visível de cada estado para 1,72×1,72 unidades locais e centraliza seu contorno, sem editar os PNGs. Na escala 0,42 da célula, o corpo mede 0,7224×0,7224 em uma célula de 0,82. Os prompts atuais estão em `Pilha_Prompts_Embrulho.md`.

`ParcelAccessoryEffect` anima a saída do selo e das fitas sem abrir o papel. `ParcelTearEffect`, cujo nome histórico foi preservado, agora anima um pulso e desaparecimento do pacote inteiro. Os efeitos usam malhas texturizadas e não exigem novo spritesheet. A conclusão entra na fila nativa de animações, impedindo vitória/derrota antes do fim. Acertos repetidos durante o efeito não iniciam nova coleta. Reinício ou saída da fase cancela o efeito sem coletar o pacote antigo.

Somente a camada base, de resistência 1, recebe `TargetComponent`. Ela representa o pacote inteiro e continua presente enquanto as proteções superiores são removidas. A coleta deixa de usar o voo genérico de cada camada: a meta só diminui no callback do último rasgo. Objetos sem `ParcelStackLayer` mantêm o fluxo existente.

O menu `Royal Aves > Elementos > Pilha de Encomendas` prepara uma fase isolada em `Assets/RoyalAves/Laboratory`, fora de Resources/Levels, com quatro pacotes e uma meta de 4 destruições. A configuração usa APIs do editor, sem editar YAML das fases da campanha.

## Como testar no Unity

1. Aguarde a recompilação. A aplicação das imagens e sincronização dos objetivos foi solicitada para a próxima recarga do editor; se necessário, use **Preparar protótipo e laboratório**. Esse comando atualiza as referências de sprite dos objetivos existentes de SolidBlock, sem alterar layouts ou quantidades das fases.
2. Escolha **Ativar laboratório para próximo Play**.
3. Entre em Play e abra uma fase pelo lobby: o carregamento usará o laboratório.
4. Reinicie a fase após recompilar. Confira a meta inicial **4**. Acerte o pacote de vida 4: o selo sai, depois a fita vertical e a horizontal. Nas transições 4→3→2→1 a meta permanece igual. No último impacto, o pacote de papel pardo desaparece inteiro; somente ao terminar a meta passa a 3 e a célula libera a queda. Verifique também pacotes com uma, duas e três vidas e acertos repetidos durante o efeito.
5. Ao vencer ou esgotar jogadas, o laboratório encerra o Play antes do fluxo normal de vitória/derrota. O acesso pelo lobby e ferramentas ainda usam a economia normal: evitar boosters pagos durante esse teste.
6. Use **Desativar laboratório** para voltar às fases normais. A seleção do laboratório dura a sessão do editor e não existe no build.

## Validação realizada e pendente

Compilação de runtime e editor com o compilador e referências do Unity passou, com avisos existentes no projeto. Os quatro estados foram inspecionados; resolução, alpha, tamanho idêntico de apresentação e encaixe na célula foram conferidos. O prefab mantém os quatro GUIDs já usados pelos objetivos, agora com novas imagens e ajustes de apresentação. O playtest visual no Unity ainda precisa ser confirmado; a compilação e as medidas não substituem essa validação.

Próximo aceite: confirmar em Play o rasgo final, meta 4→0 somente por pacotes concluídos, ausência de coleta intermediária, cada fonte de dano, combos, cascatas, reinício durante o efeito e desbloqueio da queda. A contagem por pacote foi decidida pelo usuário e implementada; o teste visual ainda está pendente.

# Mapa de níveis — editor, seasons e dez áreas

> A direção visual ativa passou a ser a [base ilustrada v6](BASE-ILUSTRADA-V6.md), com cenário verde único e coleção reduzida. Este documento registra a versão anterior; a estrutura de seasons permanece.

O editor e o jogo leem `RoyalAvesWorldMapIslands.asset` e `RoyalAvesWorldMapLayout.asset`. A disposição manual foi preservada. São dez ilhas de vinte posições: as cinco seguintes ficam disponíveis para montar cenários; o catálogo continua com as 100 fases realmente criadas. Não foram criadas fases novas.

## Editor

Menu **Royal Aves → Mapa de níveis → Pré-visualizar mundo curvo**. Todas as configurações e ferramentas de objetos ficam à esquerda, com rolagem. Arraste a divisória para ajustar a largura. O preview à direita usa a altura livre, preserva a proporção do celular e permite aproximação até 4×, roda do mouse e movimento pelo botão do meio. **Enquadrar** restaura a visão. O arraste dos objetos usa as coordenadas do preview ampliado. A ilha selecionada acompanha a região em foco ao avançar no mundo.

O cabeçalho identifica o editor v5. **Atualizar dados e reconstruir preview** relê os dados. O Unity recompila os scripts ao atualizar o projeto.

## Seasons

**Editar seasons e camadas do mapa** abre o editor de temporadas. Escolha a season, abra **Mapa de níveis** e configure **Camadas da season no mapa**:

- **Neve sobre os objetos** aplica sprites de cobertura sobre árvores, prédios, postes, caixas de correio e detalhes de qualquer bioma, incluindo Lava. Os sprites e as posições dos objetos base continuam os mesmos.
- **Cobertura de neve no chão** aplica o tratamento do chão inteiro com trechos expostos, preservando a diferença de altura da trilha. **Atlas de chão da season** permite fornecer um material completo próprio (quatro quadrantes do padrão atual).
- **Decorações por categoria** aceita sprites independentes, posição e altura relativas ao objeto. Até três camadas por objeto. Também funciona para seasons sem neve.
- Duplicar uma season copia suas configurações e decorações do mapa sem compartilhar as listas editáveis.

O seletor **Preview de season** do mapa oferece a season ativa, a base sem decoração ou a simulação de Natal/neve. Essa simulação não altera a season ativa do jogo. Para o jogo, ative a season desejada no editor de temporadas e salve. O nome interno da season não força o efeito: a opção de neve controla o visual.

## Áreas e efeitos

1. Jardim das Cartas
2. Bosque Encantado
3. Recife das Pérolas — fundo azul submerso, raios filtrados, bolhas e flora de corais; sem nuvens de céu aberto.
4. Serra da Aurora
5. Vale Termal — fissuras e pequenas poças de lava na trilha, fora da fita central e entre os botões.
6. Dunas do Sol
7. Pomar das Entregas
8. Lagoa dos Vagalumes
9. Jardim das Nuvens
10. Gruta dos Cristais

**+ Nova ilha** acrescenta outra ilha ao fim. O catálogo de temas oferece os dez ambientes. As cinco áreas novas têm seis sprites próprios cada: flora alta, agência, poste, caixa, arbusto e detalhe. O recife também ganhou seis sprites submersos.

O gêiser usa uma base fixa; a água cresce e recolhe a partir de uma origem fixa, e o vapor sobe em duas camadas translúcidas independentes. Não há troca de quadros do conjunto inteiro. O meteoro é um sprite com trajetória e transparência animadas; acompanha as camadas de transição do horizonte. A animação de água é uma transformação contínua do sprite isolado, não uma sequência de desenhos diferentes da rocha.

## Artes e validação

As oito imagens novas estão em `Assets/RoyalAves/Art/MapKitV3/{DesertLand,OrchardLand,MarshLand,CloudLand,CrystalLand,CoastLand,Shared,Christmas}/*v5.png`. Foram produzidas pelo **ImageGen integrado**: os objetos e efeitos têm alpha verdadeiro; o material de chão é opaco. Os pixels originais foram preservados. Prompts completos, nomes dos sprites, cópias de origem e arquivos finais: [season-v5-assets.json](season-v5-assets.json). São 40 sprites novos em sete atlas de objetos/efeitos e um atlas pintado de chão nevado.

Capturas nativas do Unity e relatório: [galeria v5](previews/v5/review.html), [validação](previews/v5/validation.txt). A validação roda numa cópia isolada do projeto; não fecha o Unity principal nem abre os saves do jogador. Verifica temas, referências, catálogo de fases, cliques nas fronteiras, base fixa do gêiser, água independente, meteoros como sprites, luz submersa, fissuras, neve sobre Lava e persistência das camadas de season. Desempenho em aparelho físico e gestos do mouse não foram medidos automaticamente.

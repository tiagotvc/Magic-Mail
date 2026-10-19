# Pinheiro, arbustos e controles por objeto

Novas artes na biblioteca:

- **Pinheiro clássico - pintura do arbusto**: mantém a silhueta do pinheiro central do atlas `simple-nature-v6.png`, com o acabamento do arbusto coral.
- **Arbusto verde glossy** e **Arbusto laranja glossy**: variantes de cor do arbusto coral, preservando formas, degradês e reflexos.

Foram criadas pelo **ImageGen integrado**. Os PNGs foram copiados sem alterar os pixels; prompts, referências e fontes estão em [nature-glossy-v29.json](nature-glossy-v29.json). Medidas, alpha e hashes em [art-validation.json](previews/nature-v29/art-validation.json). Os itens já têm registros nas categorias Árvores e Props. Se a janela estava aberta durante a importação, use **Atualizar biblioteca**.

Na janela **Royal Aves → Mapa de níveis → Pré-visualizar mundo curvo**, ative **Editar objetos** e selecione uma peça colocada no cenário. Abaixo da biblioteca, junto dos campos de posição e tamanho, há:

- **Virar horizontalmente**: inverte a orientação atual da peça. É manual; mudar a lateral não redefine sua escolha. As orientações automáticas já existentes de personagens, postes e caixas continuam como base.
- **Cravar no chão**: abaixa a imagem em relação ao ponto de apoio, sem alterar fase, lateral ou tamanho. **Profundidade (%)** controla a fração da altura inserida no terreno. A sombra permanece na superfície, centrada junto da base. Novos objetos sólidos entram com esse controle ligado, com profundidade inicial de 6%. Objetos de chão e efeitos conservam seu tratamento próprio do terreno.
- **Adicionar tema**: permite seguir **Da temporada (Natal: gelo)** ou escolher **Gelo** manualmente. Desligar retira a decoração sazonal daquela peça. O tema de gelo usa coberturas separadas, sem substituir a pintura original. A partir da v33, o pinheiro clássico e o arbusto coral possuem camadas pintadas específicas; os demais assets continuam com as coberturas anteriores. O chão da ilha continua sendo controlado pelas configurações da ilha/temporada.

Use **Salvar disposição** para persistir essas opções. Há suporte a Undo/Redo pelo mesmo fluxo dos demais ajustes.

A validação nativa usa o código do projeto em uma cópia isolada do Unity e não salva os layouts abertos do usuário. Confere serialização, conversão entre os dados do editor e as peças renderizadas, orientação nos slots reutilizados, UVs espelhados dos detalhes de chão, profundidade e sombra de contato, tema manual, exclusão individual e gelo de temporada. Capturas e resultado em [previews/nature-v29](previews/nature-v29/validation.txt).

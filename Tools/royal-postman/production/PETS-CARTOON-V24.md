# Mascotes cartoon V24 — instalado

Autorização de 21 de outubro de 2026: fabricar e montar todas as peças da proposta aprovada. O usuário pediu capricho na pintura, brilho separado igual nas quatro raridades e header mais alto para afastar os botões da faixa dourada.

## Regras atuais dos botões

- Preservar pintura e acabamento do teal anterior; a variante glassy geometric foi rejeitada.
- Contorno simétrico nos dois eixos, mesma largura superior/inferior, sem afunilamento.
- Chanfro com a mesma medida nos quatro lados, também na borda dourada.
- Encaixe no bege é um filete discreto de cerca de 2 px; não usar grandes cavidades.
- A face central é elevada, contínua e móvel. Borda dourada e encaixe ficam fixos.
- Texto e pequenos ícones separados. Ícone no máximo da altura das letras; x0 menor na parte inferior direita, junto aos pés do ícone e ligeiramente elevado.

## Base geométrica nativa

As gerações de bitmap continuaram distorcendo a geometria. O componente `RoyalPostmanSymmetricButtonFace` usa dois retângulos arredondados concêntricos e aplica a textura PNG anterior sem alterar seus bytes. A geometria determina a forma e a espessura, independentemente das imprecisões do bitmap. Shader separado mantém a pintura e suporta recorte UI e desaturação.

Render nativo: `../previews/pets-action-teal-v24-native.png`. Conferência em `review/pets-cartoon-v24/button-geometry-validation.txt`: 1400x540, raio140, face1272x412, raio76; chanfro64 em todos os lados, erro geométrico máximo0,000061px. O render é uma captura do componente Unity, não uma nova geração de arte.

Montagem completa instalada no prefab real `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab`: 19 PNGs originais independentes, quatro incubadoras distintas, quatro ovos e brilho compartilhado, header mais alto, ações iguais lado a lado, estoque de aceleradores no alto e prateleira reta até a base da tela. O perfil Atual, Home, Navigation, recursos e regras de incubação foram preservados, incluindo Lendário de 48 horas.

`PETS-CARTOON-V24.json` registra assets, fontes, hashes e variantes rejeitadas. As faces e bordas usam geometria nativa simétrica com a pintura aprovada; os PNGs originais continuam sem alterações de pixels. O editor recarrega o prefab quando o arquivo muda e não há edição local pendente; a proteção contra salvar por cima de uma atualização continua ativa.

Validação isolada: composição, simetria dos vértices e espessura do chanfro, toque, ações/estoque, proporção de ícones/quantidades, clique com aro fixo, área segura e persistência. A comparação editor/runtime exige transformações idênticas e permite diferença de apenas um nível de canal de 8 bits em no máximo 0,01% dos pixels, por precisão de rasterização. Relatório nativo e capturas em `review/pets-cartoon-v24/`.

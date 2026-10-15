# Carteiro no mapa — v12

O mapa usa a ilustração adulta aprovada (`postman-stylized-v7.png`), copiada sem alterar os pixels para `postman-map-v12.png`. O import original com recortes manuais foi preservado.

A versão segmentada criava emendas no corpo. Ela foi retirada do mapa: uma única malha contínua mantém a silhueta original e aplica apenas respiração e movimento leve de tecido, com os pés fixos. Não há aceno articulado nesta versão. O protótipo separado em `Characters/PostmanV8` foi preservado.

Mipmaps, filtragem trilinear e preservação da cobertura alpha suavizam o desenho na escala do mapa. O carteiro olha para a trilha. Posição e lateral dos layouts manuais permanecem iguais; entradas antigas do carteiro pequeno recebem a escala adulta ao renderizar, inclusive após arrastar no editor.

Para adicionar peças: **Adicionar item → Outro asset** (seletor de Sprite do projeto ou arraste um Sprite) **→ Colocar no mapa**. A coleção reduzida também continua disponível. Ajuste tipo/animação, tamanho e posição e use **Salvar disposição**.

Validação: compilação e renderização nativas do Unity em projeto isolado, telas 720×1600 e 540×960, substituição do personagem antigo, orientação, área de seleção, movimento com pés fixos e reutilização de instâncias ao navegar no mapa. O seletor compila; interação manual com o mouse e desempenho em celular não foram medidos.

Prévia: [review.html](previews/v12/review.html). Resultado: [validation.txt](previews/v12/validation.txt). A sincronização copia somente a referência do novo Sprite e sua importação; os layouts são conferidos por SHA256 e permanecem byte a byte iguais.

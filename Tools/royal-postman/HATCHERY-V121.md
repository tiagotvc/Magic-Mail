# Incubadora V121

Implementada a composição aprovada em `previews/incubator-v120/mock.png`, com o recebimento de `previews/pet-reveal-v117/mock.png`.

- Visor digital âmbar acima da janela exatamente quadrada, com três luzes de navegação abaixo.
- Uma ação principal: Incubar verde; durante o processo, Cancelar vermelho sem aura ou partículas. Cancelar devolve apenas o ovo original, uma vez.
- Relógio azul/dourado sobre o ovo, com estoque real `xN` dentro do mostrador. Aparece somente enquanto incuba. Continua usando os aceleradores e a curva de ouro existentes.
- Gaveta fechada mostra somente Ovos e a seta. Ao abrir, escurece o restante e exibe quatro ovos grandes em duas colunas, nomes e quantidades legíveis. Selecionar fecha a gaveta; tocar o ovo preparado remove a seleção sem consumir estoque.
- Lâmpadas e partículas de aquecimento acesas somente durante a incubação.
- Recebimento com pet menor em cores, texto amarelo com contorno azul e relevo laranja, cápsula de raridade e partículas douradas/brancas animadas em pulsos. Casca rachando, explosão e recibos persistentes permanecem no mesmo fluxo.
- Abas superiores ocultas na incubadora; perfil existente preservado. Header PT/EN, PNGs redondos do usuário e materiais AllIn1 azuis mantidos.

## Teste na Unity

Menu **Royal Postman > Testar incubadora**, depois **Iniciar simulação**. Esse ambiente usa 3 ovos de cada tipo, 3 aceleradores e tempos de 6–12 segundos. Não salva no inventário real nem no prefab. Pode concluir o processo pelo botão da barra de teste.

Ainda não existem mascotes definitivos cadastrados: o catálogo de produção permanece vazio e o botão real informa **Em breve / Coming soon**, sem consumir ovos. A coruja da simulação é exclusivamente provisória.

## Validação

`production/HatcheryV121Check.cs`: compilação/renderização nativa e GraphicRaycaster reais; gaveta/seleção/remoção; janela 1:1; pixels iluminados dos segmentos digitais; cancelamento e restituição uma vez; aquecimento/visibilidade contextual; materiais azuis persistentes; aceleração por item/ouro e pagamento rejeitado; quatro raridades e seus tempos; rachadura/explosão/recebimento; concessão e confirmação únicas; fechar/reabrir e falha de gravação; catálogo vazio; níveis; PT/EN; limites em 360×640, 390×844 e 720×1280; simulação somente leitura.

Montagem e testes executados em projeto isolado oculto, sem operar a janela principal da Unity. `-nographics` na montagem; `-force-d3d11 -job-worker-count 1 -diag-debug-shader-compiler` nos testes para limitar o uso de memória nesta máquina. Resultado, capturas e auditoria em `production/review/hatchery-v121/`. O primeiro teste encontrou um CanvasRenderer ausente no relógio antigo; agora há requisito explícito e verificação de pixels, além do ajuste do recorte frontal da almofada.

Scripts de montagem são ferramentas de produção, não devem ser reaplicados sobre edições posteriores. O instalador exige o hash original e verifica a preservação integral de CurrentProfile.

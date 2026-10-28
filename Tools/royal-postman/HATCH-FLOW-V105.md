# Testar a incubadora

Na Unity, abra **Royal Postman > Testar incubadora** e clique em **Iniciar simulação**.

O teste usa três ovos de cada tipo, três aceleradores, nível 100 e ciclos de 6, 8, 10 e 12 segundos. O botão **Concluir atual** avança apenas o relógio dessa simulação. **Reiniciar teste** repõe seus dados temporários. A janela não grava PlayerPrefs, inventário, moedas ou prefab.

1. Clique em um ovo no rodapé para colocá-lo no espaço selecionado. Isso ainda não consome estoque.
2. Clique no ovo dentro da incubadora para retirar a seleção. Cada um dos três espaços conserva sua seleção temporária.
3. Clique em Incubar para consumir um ovo, iniciar o timer e ligar o brilho quente e as partículas no interior da estufa.
4. Durante a incubação, Acelerar mantém a lógica de itens/ouro existente. Cancelar devolve o ovo original uma vez e desliga o aquecimento. O ovo em processamento não pode ser retirado com um simples toque.
5. Na conclusão, a tela escurece e o ovo cresce no centro. Raios giram atrás do glow; as oito partes da casca se separam, voam e giram, revelando o mascote com nome e raridade. Toque para continuar ou use o X depois da animação.

Ainda não há mascotes finais. A simulação usa uma silhueta e o nome **Prévia de mascote** para os quatro tipos. Os antigos exemplos Lumi/Foxy/Biscoito não foram cadastrados como recompensas reais.

## Recompensas reais

O catálogo vazio fica em `Assets/RoyalAves/Resources/Pets/PetHatchCatalog.asset`. Cada entrada configura tipo de ovo, ID do mascote, nome PT/EN, raridade e retrato. Não foram inventadas probabilidades ou conversões de duplicatas. A versão atual aceita uma entrada por tipo de ovo. Até o cadastro estar preenchido, uma nova incubação real fica indisponível e não consome ovos; os timers anteriores são preservados.

`PetsIncubationState` mantém cópias recebidas e recibos de revelações pendentes. O recebimento e a liberação do espaço são salvos antes da animação. Fechar/reabrir retoma uma apresentação pendente sem conceder novamente. Confirmar a apresentação só remove o recibo. Falha ao salvar a conclusão restaura o estado anterior, mantendo o ovo pronto.

O efeito usa as próprias texturas dos ovos: polígonos recortam a casca sem modificar pixels. Luzes, raios e movimento são gráficos nativos de UI, com tempo independente de `Time.timeScale`. A arte, os materiais AllIn1 azuis, o layout e o perfil atual não foram substituídos. Nenhum componente do prefab foi alterado para esta implementação; a ligação ocorre no controlador existente.

## Verificação

Teste nativo isolado: quatro tipos, seleção sem débito, retirada, espaços independentes, consumo/salvamento único, cancelamento, partículas, duração, conclusão, recibos/cópias, reabertura, falha de salvamento, catálogo vazio, migração do quarto timer antigo, encaixe das partes da casca, fases da animação, cliques reais com raycast e render em 390/720 pixels. A simulação também rejeita salvar alterações no prefab.

Resultados e auditoria: `production/review/hatch-flow-v105/`. O projeto de verificação usa apenas stubs para idioma/nível e para o helper de autoria do plugin AllIn1; o código de fluxo, estado e efeitos é o código real do projeto. Nenhuma janela da Unity principal foi operada automaticamente.

# Neve, gelo e iluminação noturna

As áreas de inverno têm duas superfícies: neve branca opaca com volume pintado e gelo azul com profundidade de cor, faixas largas de reflexo do céu, linhas finas internas e bordas claras. Algumas áreas antes totalmente brancas agora deixam esse gelo exposto, mantendo a borda e os montinhos de neve. A grama e a trilha continuam visíveis.

As poças de água recebem a mesma pintura de gelo quando o objeto usa o tema Gelo ou acompanha uma temporada de neve. Desligar o tema restaura a água original. As passagens entre ilhas congelam no inverno, com borda de neve e sem os sprites de ondas móveis. A ponte permanece na posição original. Os reflexos são estilizados no shader; não há uma passagem de reflexão da cena em tempo real.

## Dia e noite no editor

No painel esquerdo de **Royal Aves → Mapa de níveis → Pré-visualizar mundo curvo**, abra **Dia e noite**. Há **Ciclo automático**, duração do ciclo completo e os botões **Dia**, **Entardecer** e **Noite**. Ao usar um botão, o ciclo automático desliga para conferir aquele período. A mudança acontece gradualmente no preview. **Salvar dia e noite** persiste a configuração no kit usado pelo jogo. Os ajustes participam de Undo.

O ciclo automático padrão dura quatro minutos e começa de dia. Passa por entardecer, noite e amanhecer. A captura com TimeOverride mantém o período manual para reproduzir imagens e testes. O céu ganha cores de entardecer, estrelas animadas e lua com halo suave. O cenário fica mais escuro e frio, conservando a leitura das peças.

À noite, as luzes de Natal existentes piscam com emissão mais forte e halos coloridos. Postes acendem uma luz quente no chão. As janelas registradas ganham vidro iluminado e um halo com feixe suave saindo delas; esses efeitos se apagam de dia e acompanham o espelhamento, a posição e a profundidade da construção. A iluminação do HUD permanece separada do ambiente.

## Fontes e manutenção

- `MapFrozenIce.hlsl`: acabamento compartilhado do gelo do terreno, das poças e das passagens.
- `WorldMapKit.NightAt`: curva do ciclo automático; `WorldMapView.ConfigureDayNight`: controle manual do período.
- `WorldMapWindowProfiles.json`: regiões de vidro por sprite. `bake-window-lights-v36.cjs` lê cores e transparência das imagens, gera coordenadas e inclui encaixes revisados para as casas principais; não altera pixels. Há 30 perfis, com 68 regiões candidatas. As demais construções devem receber revisão visual ao serem usadas; novos perfis podem ser ajustados no JSON.
- `WorldMapWindowLight` e `MapWindowGlow.shader`: luz quente e feixe das janelas. `install-night-v36.cjs` instala metadados faltantes e vincula somente o shader no kit. O setup do Unity também mantém essa referência para o build.

## Verificação

Compilação completa do runtime e capturas nativas em projeto isolado. `FrozenV36Validation.Run` confere água congelada no inverno, retorno à água fora dele, temas individuais das poças e limpeza de efeitos em slots reutilizados. `NightV36Validation.Run` confere a curva de dia/noite/dia, os períodos manuais, o céu, a emissão maior das lâmpadas, os halos e as janelas da disposição salva. A cena principal, os layouts do usuário e a temporada ativa não são salvos pelos testes. Desempenho em aparelho físico não foi medido.

[Gelo e neve](previews/frozen-v36/map-1080x1920.png) · [Passagem congelada](previews/frozen-v36/crossing-frozen.png) · [Poças com temas diferentes](previews/frozen-v36/puddles-ice-and-water.png)

[Dia](previews/night-v36/day.png) · [Entardecer](previews/night-v36/dusk.png) · [Noite](previews/night-v36/night.png)

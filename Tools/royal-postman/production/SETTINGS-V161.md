# Configurações — primeira janela instalada

O usuário autorizou a migração para o jogo real em 28/10/2026, uma janela por vez. Esta entrega instala **Configurações**. A migração das demais telas continua pendente.

## Onde está

- Janela usada no jogo: `Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab`, em `Content/TelaConfiguracoes`.
- A cena `Assets/SweetSugar/Scenes/game.unity` já instancia esse prefab. A engrenagem chama `LobbyController.ToggleSettings`, que encontra o componente `SettingsScreen` existente. Nenhuma mudança na cena ou no controlador do lobby foi necessária.
- Peça independente para inspeção: `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/SettingsWindowV161.prefab`.
- Arte: `Assets/RoyalAves/Art/UI/Menu/RoyalPostman/SettingsV161/`.
- Capturas nativas e auditoria: `Tools/royal-postman/production/review/settings-v161/`.

## Arte e composição

Cinco PNGs originais, gerados separadamente pelo ImageGen integrado e preservados sem alteração dos bytes. Transparência alfa real; importação sem compressão destrutiva, mipmaps e filtro trilinear. `Sprites/` contém os Sprites serializados pelo próprio Unity. O botão usa 9-slice nativo para manter pontas, reflexos e espessura do dourado ao variar a largura.

O cabeçalho mantém arco de cobre atrás da costura horizontal dourada, cristal violeta com envelope no centro superior, título no arco e fechar vermelho. Os botões azuis têm borda contínua, sem as pontas dobradas da referência. Textos e estados são TMP reais; nenhuma ação foi gravada dentro dos PNGs.

Reutilizar estas mesmas peças nas próximas janelas para manter a coesão. Não gerar novamente o mesmo botão ou cabeçalho para cada tela. O mapa fica para o final. A nova direção terá Summon; não introduzir incubadora, ovos ou temporizadores nas novas telas.

## Comportamento preservado

Som, música, vibração, dicas e notificações continuam usando as cinco chaves originais de PlayerPrefs, com `1`/`-80`, e o caminho original dos mixers de áudio. Os nomes de ações dos protótipos (Conquistas, Suporte etc.) não foram tratados como funcionalidades já existentes. A janela mantém as preferências que de fato funcionam no jogo.

`SettingsScreen` recebeu somente campos opcionais de apresentação, rótulos de estado e atenuação visual dos ícones desativados. Sem esses campos, os outros usos do controlador continuam com a aparência anterior.

`RoyalPostmanSettingsFit` ajusta proporcionalmente o painel à área segura e faz o bloqueio de toque cobrir todo o lobby. Também neutraliza o deslocamento antigo de -107px que a cena mantém no RectTransform de Configurações, sem editar as outras alterações da cena.

## Verificação

Unity 6000.6.0f1, projeto isolado, com os scripts reais de Configurações e os sprites de produção. Após a instalação, a subárvore foi extraída novamente do prefab real e reaberta para verificar sua serialização e os cliques. Foram verificados:

- Primeiro abrir, fechar vermelho, reabrir e nova instância.
- Todas as cinco preferências alternam e persistem uma única vez por toque.
- GraphicRaycaster e eventos de clique reais, incluindo bloqueio do lobby ao fundo.
- Telas 360×640, 390×844, 720×1280 e 640×360, área segura e alvos de toque de pelo menos 44px em retrato.
- Nenhum texto transbordando; arte transparente e sem referências locais quebradas.
- Correção do deslocamento legado da cena.

Os singletons de áudio são nulos no teste isolado; não houve teste de som audível nem teste em aparelho físico. O código real de volume/mixer foi preservado.

A auditoria registra 542 blocos de objetos/componentes externos à janela preservados. O prefab de Mascotes/Incubadora, a home, o mapa e os arquivos de trabalho do usuário não foram migrados nesta etapa.

## Histórico e manutenção

Prompts completos, referências, caminhos das imagens originais e SHA-256: `SETTINGS-V161.json`. Backup anterior e candidato instalado estão na pasta de revisão. Não rodar os builders antigos sobre esta janela: eles recriam a composição anterior e apagam ajustes manuais.

Os scripts `stage-settings-v161.cjs`, `SettingsV161Build.cs` e `SettingsV161Check.cs` são ferramentas de preparação/validação isolada, não executam na aplicação. `install-settings-v161.cjs` já foi aplicado; não reaplicar sobre novas edições do usuário.

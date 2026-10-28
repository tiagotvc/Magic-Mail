# Menu — segunda janela instalada

O botão superior do lobby agora abre o Menu. **Configurações** continua disponível dentro dele, com a janela V161 preservada.

## Reaproveitamento

- Base clonada do prefab `SettingsWindowV161`: mesmo bloqueio de toque, ajuste à área segura, tipografia, fechar vermelho, botão azul, borda e resposta ao pressionar.
- Cabeçalho e corpo são duas regiões de Sprite nativas apontando para o **mesmo `SettingsV161/frame.png`**. O arco, cristal e envelope continuam com a arte original. O corpo usa o trecho inferior vazio da moldura, sem carregar o encaixe dos controles de som para o Menu.
- Oito ícones individuais já existentes no projeto. Foram mantidos seus PNGs e Sprites; apenas mipmaps e filtragem trilinear foram habilitados para evitar serrilhado na redução.
- **Nenhum PNG de arte novo, nenhuma duplicação de textura e nenhuma nova geração de imagem.**

## Destinos existentes

| Menu | Ação já usada no jogo |
|---|---|
| Ranking | `OpenTab(1)` → resultados locais de `LobbyMenus.Route.Records` |
| Equipes | `OpenTab(3)` |
| Coleção | `OpenTab(4)` |
| Eventos | `OpenTab(0)` |
| Meu perfil | `OpenProfile()` |
| Inventário | `OpenInventory()` |
| Loja | `OpenCoinShop()` |
| Configurações | `ToggleSettings()` → janela V161 |

O Menu fecha antes de abrir o destino. A seleção aproveita o som do handler existente, sem duplicá-lo. Abrir/fechar e ocultar o lobby não deixam listeners acumulados nem uma janela pendurada por cima da próxima tela.

Esta etapa não cria um novo backend de ranking, loja ou equipes. Também não cria um atalho falso de Summon apontando para a incubadora. A migração de Summon continua para uma etapa própria, conforme a direção aprovada.

## Arquivos

- Prefab: `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/MenuWindowV162.prefab`.
- Registro: entrada `menu` em `Assets/RoyalAves/Resources/RoyalPostmanUI2026/RuntimeKit.asset`, sem adicionar a navegação antiga à janela.
- Controlador: `RoyalPostmanMenuWindow.cs`.
- Integração: alterações pontuais em `LobbyController.cs`, preservando todas as edições anteriores do usuário.
- Ajuste de janela: `RoyalPostmanSettingsFit` existente. O `RoyalPostmanViewport` agora delega a ele quando presente, para o editor e o jogo mostrarem a mesma composição.
- Peças reutilizáveis: `Art/UI/Menu/RoyalPostman/MenuV162/Sprites/postal-header.asset` e `postal-body.asset`, ambos referenciando o PNG V161.
- Manifesto, origens e hashes: `MENU-V162.json`.
- Capturas, backups e auditoria: `review/menu-v162/`.

Os prefabs Home, Navigation, Pets, Configurações e RoyalAvesLobby mantiveram seus bytes. A cena, o mapa, as fontes, a economia e os dados salvos não foram editados nesta etapa. Os PNGs reutilizados também mantiveram seus bytes; as oito alterações de importação estão registradas no manifesto.

## Verificação

- Compilação C# com a resposta real do compilador Unity, incluindo os fontes atuais de RoyalAves: **zero erros**, tanto no candidato quanto na versão anterior do controlador. Os DLLs de verificação foram escritos somente na pasta de revisão.
- Unity 6000.6.0f1: cliques reais por `GraphicRaycaster`, oito destinos corretos, seleção única, reabertura, rebind, fechar, descarte silencioso e proteção contra duplo disparo.
- Transição Menu → Configurações → fechar usando o prefab real V161.
- Layout e textos em 360×640, 390×844, 720×1280 e 640×360; área segura; alvos de pelo menos 44px em retrato; nenhum controle sobreposto.
- Reuso das referências de textura, Sprite, material de texto e 9-slice verificado no Unity.
- Instanciação via `RuntimeKit` e propagação da área segura verificadas.
- Registro instalado do `RuntimeKit` reaberto no Unity: prefab correto, começa fechado, sem scripts ausentes.

Os handlers antigos após o dispatcher são limites observados no teste nativo; não houve partida completa, compra, teste de som audível ou instalação em aparelho físico. Seus códigos e regras existentes foram reaproveitados.

Não reaplicar os scripts de build/instalação por cima de ajustes manuais posteriores. As próximas janelas devem reutilizar estas mesmas peças e os sprites V161.

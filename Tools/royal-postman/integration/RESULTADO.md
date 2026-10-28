# Substituição autorizada — 19/10/2026

A home e a modal de play estão ligadas ao jogo por `Assets/RoyalAves/Resources/RoyalPostmanUI2026/RuntimeKit.asset`. O catálogo referencia os prefabs originais do kit, sem cópias achatadas da interface. A cena carrega a substituição ao iniciar o Play; uma sessão de Play anterior à mudança deve ser reiniciada.

`LobbyController` abre a home ilustrada no lugar do mapa/sala, mantém as rotas de menu, perfil, moedas, vidas e configurações, e lê o nível real de `MetaProgress`. A apresentação antiga não é mais a home enquanto esse catálogo estiver instalado. O acesso Mascote abre apenas uma indicação de recurso futuro; não foi criada uma mecânica de pet ou construção.

`RoyalPostmanLivePlayModal` mantém o `MenuPlay`, os `BoostIcon` e os callbacks originais. Oculta sua apresentação antiga e liga os novos controles ao início/fechamento da fase, seleção/compra dos reforços e aos objetivos reais. O `FreeAdBoosterButton` original recebe o novo botão/badge sem mudar o pool ou intervalo configurado; conserva a captura do item no clique e o caminho de anúncio/recompensa existente.

Validação:

- Compilação completa de runtime e editor: saída 0. Artefatos de compilação isolados em `Library/MapValidation/RoyalPostmanRuntime`; os assemblies da Unity não foram sobrescritos.
- `BINDING-CHECKS.txt`: callbacks da home, navegação/retorno, seleção/lock dos reforços, Play/Close, objetivos dinâmicos e badge/recompensa capturada. Foram usados os adaptadores reais e o `FreeAdBoosterButton` real, com fachadas isoladas para os serviços de jogo/anúncio.
- Catálogo: cinco referências de asset com GUID e object ID conferidos; oito scripts de UI com seus metadados.
- Nenhuma escrita de teste no progresso, carteira ou inventário. Não foi testada uma conclusão de anúncio em uma rede/dispositivo real.

A fonte continua sendo a candidata nativa da prévia; não foi declarada uma correspondência exata com a tipografia gerada do mock. As outras telas de menu conservam seus conteúdos existentes.

Corre��o de importa��o em 19/10/2026:
- Os PNGs s�o texturas; cada sprite de UI agora � um asset nativo persistente em Art/RoyalPostmanUI2026/Sprites. Prefabs n�o dependem dos IDs de subassets gerados pelo importador.
- Recortes respeitam a resolu��o efetivamente importada. Corrigido o erro de recorte fora da textura que interrompia a reconstru��o.
- As 133 refer�ncias m_Sprite dos prefabs entregues resolvem para objetos existentes; cat�logo dos refor�os tamb�m usa os novos sprites.
- Simulator em modo de edi��o usa a home/modal instaladas em vez do renderer antigo do level map.
- Runtime e Editor completos compilados com sa�da 0. Captura ap�s reabertura da Unity verificada no projeto isolado; n�o foi controlada a sess�o Play j� aberta do usu�rio.

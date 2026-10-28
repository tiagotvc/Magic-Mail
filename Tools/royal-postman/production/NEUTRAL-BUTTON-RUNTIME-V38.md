# Incubar — teste do botão neutro

Aplicado ao prefab real `Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab` após pedido explícito do usuário. Somente os quatro botões Incubar foram alterados; o restante do prefab foi preservado por comparação semântica.

Centro neutro V37 e borda neutra V38 continuam em PNGs originais separados. Sprites nativos usam a mesma região `(104,49,1736,712)` e pivô central, sem modificar pixels. O centro recebe tint teal e a borda tint dourado. A borda permanece fixa na frente enquanto a face, o texto e o ícone encolhem ao pressionar.

`RoyalPostmanActionVisual.enabledTint` permite persistir a cor da face; padrão branco mantém compatibilidade com os demais controles. Sem recursos, o botão mantém seu matiz em tom mais escuro e continua desabilitado conforme a regra existente.

Validação nativa isolada: estados habilitado/desabilitado nas quatro raridades; pressionar/soltar com borda fixa; clique consome exatamente um ovo pela ligação existente; capturas720x1280 e390x844. Apenas estoque injetado em memória, sem gravar PlayerPrefs. Home e Navigation mantiveram os hashes e todos os componentes fora de Incubar foram preservados. Relatórios e imagens em `review/neutral-button-v38/`.

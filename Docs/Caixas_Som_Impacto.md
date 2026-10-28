# Som das caixas de madeira

Áudio fornecido pelo usuário: `madeira-quebrando.MP3`. Importado sem edição em `Assets/RoyalAves/Resources/Audio/Parcels/madeira-quebrando.mp3`; conteúdo binário conferido por SHA-256.

Cada impacto que remove uma camada da caixa toca o arquivo, inclusive o último golpe. O som parte de `ParcelStackLayer`, tanto na remoção da proteção quanto no início da quebra final. A guarda `IsFinishing` evita repetir o som para tentativas de atingir uma caixa que já está concluindo a quebra.

`ParcelSoundEffects.PlayWoodHit` usa o AudioSource/mixer de efeitos do jogo, respeitando volume e mute. Não aplica o agrupamento de 0,06 s dos efeitos antigos: caixas atingidas juntas também disparam individualmente. Os antigos sons de proteção/conclusão da caixa foram retirados desses caminhos, evitando sobreposição com o áudio novo; o armário continua com seu som próprio.

Importação configurada como efeito 2D, pré-carregado. Runtime/editor compilados sem erros. Conferência auditiva em Play pendente: caixas com uma, duas e três camadas, impactos adjacentes e especiais, grupo de caixas e efeitos silenciados.

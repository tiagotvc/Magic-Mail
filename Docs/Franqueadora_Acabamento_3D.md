# Franqueadora — acabamento 3D

Corpo e alavanca remodelados para acompanhar os seis elementos já instalados: superfícies arredondadas, dourado acetinado, base laranja sem textura de madeira, iluminação suave superior esquerda e redução dos contornos escuros. O corpo e a alavanca continuam sendo peças independentes.

Assets novos em `Assets/RoyalAves/Resources/Soft3D/franking-body.png` e `franking-lever.png`, com sprites nativos e materiais individuais. As imagens anteriores permanecem intactas. Geração pelo ImageGen integrado; prompts, referências, arquivos originais e hashes em `Tools/royal-postman/production/review/franking-soft3d-v1/prompts.json` e no manifesto.

O prefab visual existente `Assets/RoyalAves/Prefabs/Elements/FrankingMachine.prefab` mantém os IDs e recebe os sprites/materiais dos renderizadores Press e LeverPivot. O Unity também serializou explicitamente os padrões que já estavam em uso: playbackSpeed=1 e useCabinetDelivery=false. Permanecem os mesmos encaixes, escala, pivô inferior da alavanca, ângulos de −28°/−100°, tempos de acionamento/retorno, compressão do corpo e carta emitida. O caminho de entrega exclusivo do escaninho continua desativado nesta peça.

A regra de emissão, cota de cartas, meta MachineLetters e registro da franqueadora no Level Editor não mudaram. A miniatura do editor passa a ler a arte do prefab instalado. O comando antigo de preparar a franqueadora preserva a remodelagem, sem reconstruir o modelo anterior.

Validação em cópia isolada: carregamento normal com laboratórios desligados, oito acionamentos e oito entregas, contagem somente na chegada, rejeição de impacto enquanto ocupada e após a cota, retorno da alavanca e da posição/escala do corpo. Capturas nativas da peça em repouso e acionada. Nível de teste 1904 apenas na cópia isolada, sem instalação na campanha real.

Auditoria verifica que apenas os dois pares sprite/material mudaram no prefab e que os outros componentes, cartas, transforms, arquivos de fase/metas e prefabs de blocos permanecem iguais. Sem alegação de validação física, dispositivo ou áudio. Backup em `Backups/franking-soft3d-2026-11-13/`.

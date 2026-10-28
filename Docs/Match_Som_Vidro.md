# Som de combinação: vidro quebrando

Áudio fornecido pelo usuário: `vidro-quebrando.MP3`. Fonte preservada em `Tools/audio/sources/vidro-quebrando.mp3`.

Substituídos os conteúdos dos dois sons já usados por `SoundBase.destroy`, mantendo nomes, metadados e GUIDs:

| Asset | Duração solicitada | Duração PCM efetiva |
| --- | --- | --- |
| `Assets/SweetSugar/Audio/wav/Jelly_destroy.wav` | 0,183 s | 0,182993 s (8.070 amostras) |
| `Assets/SweetSugar/Audio/wav/Jelly_destroy_02.wav` | 0,157 s | 0,157007 s (6.924 amostras) |

As diferenças de microssegundos decorrem do número inteiro de amostras a 44.100 Hz. Ambas as versões são mono, PCM de 16 bits, como os originais. Removido silêncio externo; conteúdo audível comprimido no tempo com `atempo`, preservando o tom. Aplicados ataque de 1 ms, saída de 10 ms e nível RMS próximo ao de cada versão antiga para manter o equilíbrio de efeitos.

Reconstrução: `python Tools/audio/build_match_glass.py` (numpy e ffmpeg de imageio_ffmpeg). Backup anterior em `Backups/Match-before-glass`; relatório em `Logs/match-glass-audio-validation.json`. Verificados comprimento em amostras, ausência de clipping, término em zero e preservação dos metadados/referências. A escolha aleatória entre as duas versões continua no código existente; nenhum código de gameplay foi alterado. Conferência auditiva no Unity pendente.

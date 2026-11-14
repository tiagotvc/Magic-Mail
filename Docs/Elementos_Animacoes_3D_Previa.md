# Prévia de animações dos seis elementos

O novo acabamento foi aplicado a um estudo animado de **caixa de madeira, pedra rúnica, tinteiro, poções, bolsa do carteiro e sinalizador**. Contornos suaves, superfícies arredondadas, iluminação superior esquerda e fragmentos sólidos com chanfros. Os efeitos dos power-ups e outros elementos ficam fora deste escopo.

Abrir no Unity: **Royal Aves → Elementos → Remodelagem 3D → Previa das seis animacoes**. Há reprodução automática, pausa, reinício e barra de tempo. A janela usa uma PreviewScene separada; funciona sem Play e preserva a cena aberta.

Vídeo e GIF em `Tools/royal-postman/previews/elementos-soft3d-animacao-v1/`; abrir `index.html`. A gravação contém 168 quadros nativos, 1200×800, 24 fps, com reinício restaurando exatamente o quadro intacto. MP4/GIF são codificações desses quadros; a montagem de comparação não repinta a arte.

Validação: compiladores de runtime/editor sem erros, exportação nativa em Unity isolado, inspeção dos estados intactos, quebras, líquidos e bolsa aberta; primeiro e último quadro idênticos. Não foram feitos testes de toque físico, áudio, dispositivo ou regras reais nesta cena de arte.

**Prévia visual, sem migração de produção.** Os prefabs originais, suas metas e regras continuam instalados. Os três últimos elementos já registrados continuam acessíveis no Level Editor; este estudo não desfaz essa integração. Cofre, fita e escaninho não são modificados. Não considerar a estética aprovada antes da avaliação do usuário.

Prompts e fontes: `prompts.json`, `cleanup-prompts.json`; manifesto `ANIMATION-PREVIEW-V1.json`; detalhes `LEIA-ME.md` na pasta da prévia. O primeiro atlas de dano do tinteiro e sua limpeza tinham aura e foram rejeitados; usa-se a faixa de cinco estados gerada novamente. A transparência de baixa opacidade é normalizada apenas no shader exclusivo da prévia, sem editar os PNGs originais.

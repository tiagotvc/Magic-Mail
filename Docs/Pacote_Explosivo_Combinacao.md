# Pavio V3 - fogo curto e faiscante (09/11/2026)

Usuario rejeitou o formato de vela da revisao anterior. Substituido por nucleo incandescente compacto, pequenas linguas irregulares em varias direcoes e particulas curtas que saltam da corda. Preservados percurso, consumo do pavio, duracao e alcance da explosao.

Validacao: runtime/editor zero erros. Shader suportado, seis estagios renderizados no Unity isolado, material restaurado; capturas 1 e 3 inspecionadas. Evidencias: Tools/royal-postman/production/review/dynamite-fuse-v3/. Ainda sem teste da combinacao em Play nesta revisao e sem aprovacao visual do usuario.

---

# Revisao do pavio animado (09/11/2026)

Substituidas as faiscas por chama continua animada, com linguas de fogo e nucleo amarelo. A chama percorre a curva do pavio; shader remove progressivamente a corda queimada e mostra uma borda incandescente. Material aplicado aos dois renderers do PACKAGE para o pavio intacto nao reaparecer por tras. PNG original preservado. Materiais originais restaurados na desativacao/destruicao.

Antecipacao: 0,90s combinada / 0,32s simples. O alcance nao foi alterado nesta revisao. Runtime/editor: compilacao zero erros. Renderizacao nativa isolada em Logs/fuse-native-v2; nao equivale a teste de gameplay completo.

---

# Atualizacao 09/11/2026: pavio e combinacao global

Dois pacotes combinados atingem todas as celulas jogaveis do campo atual, sem limite por distancia ou bloqueio de caixas. Cada obstaculo recebe um impacto e conserva sua resistencia. Dinamite simples continua com raio 2 e barreiras existentes.

Brasa pulsante e faiscas seguem a ponta do pavio durante o crescimento; limpeza no impacto e ao desativar. Explosao combinada dimensionada pela borda mais distante. Laboratorio existente atualizado para 24 pecas simples e 79 na combinacao 9x9.

Verificacao desta atualizacao: runtime e editor compilados com zero erros (Tools/inkwell-compile.py, Logs/inkwell-review/). Play, captura visual, ponteiro fisico, audio, dispositivo e vitoria ainda NAO verificados. Nenhum outro Unity iniciado; campanha/recompensas preservadas. Video indisponivel pela ferramenta; pedido escrito utilizado.

Registro abaixo HISTORICO: resultados/raios antigos nao validam esta atualizacao.

---

# Pacote explosivo: absorção e explosão combinada

Referência observada: vídeo local `Bombs-merge-in-Royal-Match`, principalmente a sequência inicial. O pacote mantém a arte existente.

- Dois pacotes usam uma única sequência: absorção do segundo, crescimento do principal, compressão breve e explosão.
- Antecipação combinada de 0,68 s; crescimento até aproximadamente 2,12 vezes a escala original, com compressão final.
- Explosão simples preserva o raio existente de três células. Combinação usa quatro células e um único impacto, sujeito às barreiras existentes.
- O efeito antigo `_ExplosionAround` foi retirado desta ativação. Novo efeito procedural: núcleo quente irregular, pressão alaranjada, fogo curto e fumaça que dispersa; sem estrelas de brilho. Material em Resources mantém o shader referenciado em builds.
- Gravidade permanece bloqueada até 0,28 s após o impacto; dano começa no estouro.
- Removido bloqueador provisório de `BonusesAnimation`: ele podia bloquear a própria chamada de combinação na troca real. Cada especial mantém o bloqueio de sua sequência.

Validação nativa: três casos passaram (pacote simples, troca real entre dois pacotes, pacote simples repetido). Foram observados 48 impactos no raio 3 e 70 no raio 4, este último limitado pela borda esquerda do tabuleiro. Crescimento combinado observado em cerca de 2,37 vezes na horizontal, incluindo a compressão final. Eventos verificaram impacto único, ausência de dano anterior ao estouro e bloqueio da gravidade na ênfase. Capturas de Play inspecionadas; shader/material carregados e suportados. Compilação completa de runtime e editor sem erros. A captura controlada limita maximumDeltaTime somente no teste e restaura o valor ao terminar. Logs/bomb-native.result registra a execução mais recente.

Não foram testados toque físico, áudio audível, dispositivo ou fluxo completo de vitória/Continue. Os testes mantêm pedras fora do alcance para evitar vitória e não executam concessão de recompensa. Não é uma integração/merge. Não foram alteradas a imagem do pacote, a arte da pedra, a configuração de níveis ou recompensas.

Última conferência visual: PASS em Logs/bomb-native.result (troca real, raio ampliado, um impacto, shader suportado, limpeza automática). Resultado completo dos três casos de comportamento preservado em Logs/bomb-behavior-native.result. Capturas: Logs/bomb-merge.png e Logs/bomb-fire-1.png. O efeito inicial já é configurado no mesmo quadro da ativação; nenhuma nuvem aparece com material ou escala provisória.

Validacao visual da revisao: PASS em Logs/fuse-native-v2/Evidence/result.txt; seis estagios renderizados no Unity, capturas 3 e 5 inspecionadas (chama e encurtamento visiveis). Material restaurado via callback de desativacao. Render isolado, sem teste da troca em Play nesta revisao.

Pavio V4: faiscas ampliadas (alcance, comprimento e espessura), mantendo tamanho da brasa e fogo curto. Runtime/editor zero erros; seis renders Unity isolados e restauracao de material PASS. Captura 1 inspecionada. Evidencia review/dynamite-fuse-v4/. Sem teste de gameplay nesta revisao.

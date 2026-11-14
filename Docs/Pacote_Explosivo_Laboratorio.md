# Laboratório do pacote explosivo

No Unity: **Royal Aves > Efeitos > Pacote explosivo > Abrir laboratório**.

Dois cenários num tabuleiro 9×9:
- **Pacote explosivo simples**: toque no pacote central ou use **Ativar pacote explosivo** no painel.
- **Combinação de dois pacotes**: arraste o pacote da direita sobre o da esquerda, ou use **Combinar os dois pacotes**. O botão executa a troca real das peças.

O seletor de cenário reinicia a bancada. **Abrir / reiniciar cenário** repõe o tabuleiro; o controle **Velocidade** permite observar a antecipação e a explosão devagar. **Desativar e voltar às fases normais** encerra o Play e remove a seleção do laboratório.

Implementação exclusiva do Editor. Reutiliza os assets existentes do laboratório da Light Ball como modelo, mas constrói a cópia 9×9 em memória. Não escreve níveis, prefabs, arte ou metas da campanha. Compartilha o ajuste de câmera e os sprites reais das metas. Boosters pagos ocultos; vitória/derrota da bancada não concede recompensas nem avança a campanha.

Verificação: compilação completa de runtime e editor sem erros. Execução nativa dos dois cenários registrada em `Logs/explosive-package-lab-smoke.result`; capturas `Logs/explosive-package-lab-mode-0.png` e `Logs/explosive-package-lab-mode-1.png`. Teste automatizado da combinação usa troca por código; não equivale a teste de arraste físico, dispositivo, áudio ou fluxo de vitória da campanha.

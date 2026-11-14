# Prioridade de alvo da hélice

A hélice procura primeiro uma **pedra rúnica ou um cofre exposto**, pois esses obstáculos só recebem dano de especiais. A prioridade vale mesmo quando o obstáculo não faz parte das metas do HUD.

São mantidas as reservas entre hélices: um alvo reservado por outra não é escolhido. Pedras em quebra, cofres no impacto final e objetos inativos ficam fora da seleção. Se não houver candidato elegível, permanece a escolha anterior de metas, obstáculos e peças. Dentro do grupo prioritário, continua o desempate pela maior distância, usado para deixar o voo visível.

A escolha fica no caminho compartilhado de voo/reseleção de `MarmaladeFly`, usado pela hélice simples e pelas combinações. Quando existem posições de destino pré-configuradas, a preferência pelos obstáculos exclusivos de especiais também é consultada antes delas.

Não há alteração de trajetória, velocidade, dano, animações ou ordem de renderização. O cofre continua com cinco impactos e a pedra com um impacto especial.

Validação: compilação de runtime/editor; teste nativo isolado do seletor de produção para prioridade, distância, reservas, objetos inativos/em quebra, cobertura por outra camada e retorno sem candidato para permitir o fallback. Sem nova validação física do voo, dispositivo ou áudio.

Backup: `Backups/helix-special-priority-2026-11-13/`. Evidência: `Tools/royal-postman/production/review/helix-special-priority-v1/`.

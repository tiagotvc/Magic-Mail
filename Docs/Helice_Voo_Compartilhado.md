# Navegacao compartilhada da helice - 12/11/2026

Regra pedida: ao combinar com foguete ou dinamite, a helice escolhe o mesmo destino e segue o mesmo voo que seguiria sozinha. O parceiro acompanha e ativa na chegada.

FlyToTarget agora usa o arco normal para todas as cargas: altura max(1, distancia*0.35), desvio lateral 0.6 e velocidade/limites existentes. Removida a prioridade por concentracao de alvos exclusiva da dinamite. FindTarget aplica a mesma cadeia normal de objetivos, reservas e revalidacao; o tipo de carga nao participa da escolha. O laboratorio de dinamite nao impoe mais o alvo central. Destinos explicitamente definidos por autores continuam respeitados tanto no voo simples como no combinado.

Animacao orbital, escala durante giro, prioridade acima do cofre e ativacao local H/V/dinamite preservadas. A saida fluida das tres helices mantem sua aceleracao de partida; usa a mesma geometria de arco. Nao houve alteracao em campanha, recompensas, economia, arte ou prefabs de obstaculos.

Teste nativo isolado PASS em dinamite e foguete horizontal: apex/duracao verificados pela formula normal; selecao repetida sem carga escolhe o mesmo alvo. Dinamite entrega um estouro local, sem explosao na origem; foguete entrega um disparo e nove celulas no eixo. Gravidade liberada. Runtime/editor zero erros. Sem ponteiro fisico, audio audivel, dispositivo ou fluxo completo de vitoria.

Evidencias: Tools/royal-postman/production/review/normal-helix-delivery-v1/. Manifesto NORMAL-HELIX-DELIVERY-V1.json. Previas MP4/GIF em Tools/royal-postman/previews/normal-helix-delivery-v1/index.html. Historicos de voo baixo/alvo guiado anteriores ficam supersedidos por esta regra. Avaliacao visual do usuario pendente.

# Som do power-up hélice

Áudio fornecido pelo usuário: `helicoptero-som.MP3`. Conteúdo copiado sem edição para `Assets/RoyalAves/Audio/SFX/som-helice.mp3`, substituindo o áudio anterior e preservando o GUID e as referências existentes.

`SoundBase.helixLaunch` aponta para esse asset. O som agora é controlado pelo próprio `MarmaladeFly`, começando quando a hélice inicia o giro/decolagem, inclusive nas combinações. O disparo solto de `ItemMarmalade.Destroy` foi removido para evitar duplicidade e áudio continuando após o impacto.

## Duração sincronizada ao voo

Mantido o áudio original maior `helicoptero-som.MP3`, conforme a preferência do usuário caso fosse possível sincronizar. O novo arquivo `madeira-quebrando(1).MP3` foi disponibilizado como alternativa mais curta; não foi necessário substituir o original.

Cada hélice usa uma fonte própria com o mixer, volume e mute da fonte de efeitos, sem mudar o tom (`pitch = 1`). O som acompanha o tempo real do movimento, com saída suave nos últimos 0,08 s da trajetória e parada ao atingir o alvo. Isso ajusta o trecho ouvido ao voo; não comprime o arquivo inteiro nem altera a velocidade da animação. Se o voo se estender por troca de alvo, cancela a saída e mantém o áudio, repetindo o clip se necessário.

Desativação, retorno ao pool e saída da cena interrompem a fonte. Hélices duplicadas controlam seus sons individualmente. Runtime/editor compilados; teste auditivo e de sincronismo em Play pendente para voos curtos/longos, mudança de alvo, combinações e reinício da fase.

Verificado: conteúdo binário idêntico ao arquivo fornecido, `.meta` preservado e referência `helixLaunch` no prefab SoundBase. Nenhum código alterado nesta substituição. Conferência auditiva em Play pendente. Backup do som anterior e metadados em `Backups/Helix-before-helicoptero-som`.

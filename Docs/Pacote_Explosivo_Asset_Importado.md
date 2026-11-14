> EFEITO REJEITADO E INATIVO: substitu?do por Explosion.tif/PNG. Ver Docs/Pacote_Explosivo_Explosion_TIF.md. N?o reintroduzir este prefab.

# Explosão do pacote: asset Mesh Explosion

Fonte local fornecida pelo usuário: `Mesh Explosion Mesh explode System Mesh Slicing Framework v1.0.unitypackage`, autor identificado na documentação: GolemKin.

Foram importados somente `Prefab/Explosion.prefab`, seus quatro materiais, duas texturas e o pequeno componente de desativação referenciado pelo original. O manifesto de arquivos e hashes está em `Logs/mesh-explosion-inspect/import-manifest.json`. Fontes preservadas em `Assets/ThirdParty/GolemKinMeshExplosion/`; cenas de demonstração, fontes, modelos, sistema de corte de meshes e Jobs não foram importados.

O adaptador `PackageExplosionAssetTools` gera `Assets/RoyalAves/Resources/Effects/ExplosivePackageBlast.prefab` e quatro materiais próprios. As curvas e texturas do efeito original são reutilizadas. O shader de partículas preserva textura, cor por partícula e mistura alpha/aditiva, compatível com a renderização 2D atual, sem exigir iluminação/soft particles da demo. O prefab adaptado não contém scripts do fornecedor nem luzes de cenário; não toca física, colliders ou peças do tabuleiro.

Adaptação: rotação da apresentação para a vista frontal; escala reduzida; trajetórias de faíscas e raios de fogo contidas; emissão em uma única execução. O efeito é removido após 1,6 segundos. A combinação usa escala 35% maior. Antecipação, absorção, área de dano e pausa da gravidade continuam sob a lógica existente do pacote.

`DynamiteBlastEffect.Play` usa primeiro o novo prefab, mantendo o efeito procedural anterior como fallback se o recurso estiver ausente. O mesmo caminho atende partida e laboratório. Não houve alteração em níveis, recompensas, imagem do pacote ou quebra da pedra rúnica.

Menu para regenerar apenas a adaptação: **Royal Aves > Efeitos > Pacote explosivo > Preparar efeito importado**. Requer sair do Play; não reimporta nem sobrescreve a fonte do fornecedor.

Validação nativa: consultar `Logs/package-explosion-asset.result` e `Logs/explosive-package-lab-smoke.result`. O teste do laboratório observa o novo prefab, materiais suportados, remoção do efeito e os danos dos dois cenários. Capturas do efeito importado: `Logs/explosive-package-imported-effect-0.png` e `Logs/explosive-package-imported-effect-1.png`. Não é validação em dispositivo físico, áudio audível ou fluxo completo de vitória.

As part?culas s?o pr?-simuladas por 25 ms e retomadas individualmente no instante do impacto, para que o clar?o j? exista no primeiro quadro. O teste exige part?culas efetivamente emitidas, al?m da presen?a do prefab.

A nuvem central usa a textura original Smoke.psd, ampliada e com gradiente quente amarelo/laranja e dura??o fixa de 0,32 s; o clar?o dura 0,25 s. Isso evita que a varia??o aleat?ria de vida do prefab deixe apenas as fa?scas vis?veis muito cedo.

Resultado final: PASS nos dois cen?rios em Play, com part?culas realmente emitidas, materiais suportados e efeito removido automaticamente; 48 pe?as atingidas no cen?rio simples e 79 na combina??o. Captura do n?cleo: `Logs/explosive-package-imported-start.png`. Manifesto e resultado preservados em `Tools/asset-imports/explosive-package/`. Laborat?rio reaberto com dois pacotes para testar a combina??o.

## Corre??o de visibilidade no uso normal

O teste anterior limitava `Time.maximumDeltaTime` a 50 ms, mascarando quadros lentos do Editor. Na reprodu??o sem esse limite, o intervalo observado foi de 321 ms, consumindo o n?cleo de 320 ms quase inteiro entre dois quadros. A camada de ordena??o j? era Item mask, acima do tabuleiro; n?o foi comprovado que ordena??o fosse a causa.

O efeito importado agora fica pausado para a simula??o autom?tica e avan?a localmente em LateUpdate por no m?ximo 50 ms por quadro. O rel?gio global do jogo permanece intacto. O shader usa ZTest Always e ZWrite Off, conservando a camada e a ordem dos power-ups, para n?o sofrer oclus?o pela profundidade do grid. A gravidade espera tamb?m a ?nfase visual inicial de 250 ms do efeito. O controlador remove o efeito ap?s 800 ms de simula??o visual; n?o h? mais um temporizador global de 1,6 s que poderia remov?-lo antes de apresentar os quadros.

O teste do laborat?rio n?o altera mais maximumDeltaTime e exige o n?cleo presente em pelo menos tr?s observa??es de quadros. Os resultados e capturas atuais substituem a alega??o de valida??o visual baseada na captura anterior com limite global.

Verifica??o desta corre??o: PASS nos dois cen?rios sem reduzir maximumDeltaTime (0,333 s). N?cleo observado em oito quadros no simples e dez na combina??o; houve quadro real de 333 ms sem consumir todo o efeito. Dano permaneceu em 48/79 pe?as, impacto ?nico, pausa antes da gravidade e descarte autom?tico. Captura real inspecionada: `Logs/explosive-package-imported-start.png`.

## Escala e express?o da explos?o

Ap?s a corre??o de visibilidade, o usu?rio esclareceu que o efeito aparecia, mas era pequeno e pouco expressivo frente ao v?deo de Royal Match. A adapta??o passa de escala-base 0,16 para 0,30 (1,875 vezes), com n?cleo de 420 ms, crescimento r?pido e expans?o final; cor inicial mais clara antes de amarelo/laranja. O clar?o foi contido separadamente para n?o lavar a tela inteira. Rajadas de fogo ficam mais largas. Uma frente de press?o complementar usa o alcance real (7?7 simples / 9?9 combinado), com contorno irregular e raios quentes curtos. A combina??o preserva o fator visual de 1,35 e a absor??o/crescimento pr?vios. Dano e regras dos n?veis n?o mudaram.

Confer?ncia desta amplia??o: dois cen?rios nativos PASS, mesmo dano de 48/79 pe?as e n?cleo observado por m?ltiplos quadros sem limitar o rel?gio global. Captura real da combina??o inspecionada em `Logs/explosive-package-imported-start.png`. Bancada reaberta limpa. Sem teste de toque f?sico/dispositivo/?udio/vit?ria completa.

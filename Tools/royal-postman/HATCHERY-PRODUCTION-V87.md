# Chocadeira V87 instalada

A montagem segue as proporções da V84, com cabeçalho vermelho/dourado único, fundo azul, uma janela por vez, dois visores e footer roxo embutido. O arquivo real é Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab.

Os botões redondos usam os PNGs do botão atual do usuário: aro fixo, centro vermelho e ícone separado. Os dois retângulos em HatcheryTabs são placeholders editáveis; seus Buttons e callbacks já estão ligados.

A janela, apoio, ovo, frente do apoio e partículas são camadas separadas. A frente do apoio é um recorte de Sprite nativo do mesmo PNG intacto. As quantidades, nomes e visor de sete segmentos são editáveis e mudam em runtime. Os novos títulos têm face creme e contorno/relevo marrom, retos, em português/inglês; o título muda para Mascotes/Pets quando a outra aba abre.

Há três espaços desbloqueados nos níveis 30, 50 e 100. Cada um aceita qualquer um dos quatro ovos, mantendo durações 1h/3h/12h/48h. Timers e recursos dos saves anteriores são preservados; uma eventual quarta incubação antiga continua acessível por um controle condicional, sem permitir uma quarta incubação nova. Acelerar mantém o consumo e o custo do sistema existente.

As posições são autoradas no prefab. O preenchimento e o footer acompanham a altura disponível e a área segura. A tela CurrentProfile existente é preservada integralmente.

Produção: HatcheryV87Build.cs, HatcheryV87Review.cs, stage-hatchery-v87.cjs e merge-hatchery-v87.cjs. Não execute o builder sobre uma tela já editada. A instalação foi seletiva e auditada: 8 componentes existentes ajustados, 360 adicionados, 1528 preservados, incluindo 486 de CurrentProfile.

Fontes PNG originais em Assets/RoyalAves/Art/UI/Menu/RoyalPostman/HatcheryV87/. Prompts/fontes em HATCHERY-V87-ASSETS.json. Os arquivos de imagem não foram redesenhados ou regravados por código. Recortes/bordas/paletas ficam em assets nativos.

Validação nativa: navegação e profile, back/fechar, raycast e pressão independente, PT/EN, bloqueios, seleção dos quatro ovos, débito/start/save único, tempo 48h, aceleração por item/ouro e falha de pagamento, migração de saves, máscaras/partículas e área segura em 360/390/720/1080. Resultados em production/review/hatchery-v87/.

O usuário pediu para conferir por conta própria. Nenhuma nova janela/prévia foi aberta durante esta etapa.

# Royal Postman — máscaras VFX da Flecha Postal (mock específico)

Kit com **22 máscaras PNG individuais** para replicar por composição o mock em que a Flecha Postal voa para a direita deixando cauda branca/dourada, fitas mágicas rosas/roxas, fragmentos RGBY e sparkles. Nenhum sprite da flecha está incluído: use o existente no projeto.

**Todos PNGs:** cor RGB branca, alpha com intensidade e fundo inteiramente transparente. Não há quadriculado/preto incorporado. Reutilize com tint e shaders Unity.

| Arquivo | Função |
|---|---|
| `01_aura_item_suave.png` (512×320) | Glow radial discreto atrás do corpo da flecha |
| `02_glow_ponta_dourado.png` (384×384) | Glow concentrado na ponta da flecha |
| `03_cauda_halo_largo.png` (1024×256) | Banda larga translúcida; pintar rosa, roxo suave ou dourado |
| `04_cauda_coracao_branco.png` (1024×256) | Núcleo branco super estreito e muito intenso |
| `05_cauda_faixa_dourada.png` (1024×256) | Camada intermediária dourada sob o núcleo branco |
| `06_fita_roxa_superior.png` (1024×256) | Fita ondulada superior magenta, desenhada para +X |
| `07_fita_roxa_inferior.png` (1024×256) | Fita inferior lilás; cruza e se afasta do núcleo |
| `08_fita_rosa_intermediaria.png` (1024×256) | Fita interna rosa-prateada entre os feixes |
| `09_fita_halo_difuso.png` (1024×256) | Aura macia para duplicar sob a fita superior |
| `10_residuo_ondulado.png` (1024×256) | Mecha secundária tênue que se apaga no caminho |
| `11_sparkle_quatro_pontas.png` (256×256) | Estrelinha fina de 4 pontas, brilho pontual |
| `12_sparkle_seis_pontas.png` (256×256) | Estrelinha fina de 6 pontas, variar escala e alpha |
| `13_sparkle_mini.png` (128×128) | Variação pequena dos sparkles |
| `14_ponto_luz.png` (128×128) | Partícula radial circular pequena |
| `15_risco_velocidade.png` (512×128) | Risco curto e afilado que voa ao lado da cauda |
| `16_fragmento_irregular.png` (128×128) | Pedra plana irregular, colorizar por cor de peça |
| `17_fragmento_pentagono.png` (128×128) | Fragmento pequeno pentagonal simples |
| `18_fragmento_triangulo.png` (128×128) | Triângulo simples; sem chanfro ou contorno metálico |
| `19_fragmento_quadrado.png` (128×128) | Fragmento quadrilátero simples |
| `20_flash_impacto_celula.png` (256×256) | Microflash branco ao limpar célula no caminho |
| `21_anel_impacto_celula.png` (256×256) | Anel fino de impacto, expandir e dissipar |
| `22_whizz_frontal.png` (512×256) | Alargamento frontal translúcido para sensação de velocidade |

## Camadas (de trás para frente)

1. Halo largo `03`, com tint dourado pálido de baixa intensidade.
2. Fita difusa `09`, tint magenta/roxo de baixa intensidade.
3. Fitas `06`, `07` e `08`, cada uma com variação de brilho e deslocamento; ficam atrás do corpo da flecha.
4. Faixa dourada `05`, sobreposta pelo núcleo branco `04`.
5. Sparkles `11`–`15` e pedrinhas `16`–`19`, com cores por peças afetadas.
6. Aura da ponta `02`, e o sprite ORIGINAL da flecha acima dos demais.
7. Microflash/anel `20`/`21` nas células atingidas.

**Aviso essencial:** as fitas prontas são perfis de referência visual para um segmento da trilha, não devem ser usadas como uma faixa gigante rigidamente anexada à flecha. Faça os nós/partículas nascerem na posição da cauda e permanecerem em **world space** ou coordenadas equivalentes do Canvas. TrailRenderer/ParticleSystem/mesh dinâmica podem gerar curvas com controle fino. A textura base pode também ser repetida ao longo de um mesh; evite stretching grotesco no fim da linha.

**Orientação:** texturas de cauda são horizontais, direção +X, com extremo final à direita. Para movimento contrário espelhe a textura e altere sentido; para vertical rotacione 90°/270°, respeitando o eixo do seu objeto. A Flecha Postal na arte de apresentação original é inclinada: ajuste o ângulo do sprite para apontar para o deslocamento sem redesenhar o asset.

**Importação Unity:** Texture Type Sprite (2D and UI); Sprite Mode Single; Mesh Type Full Rect; Alpha Is Transparency ativado; Compression preferencialmente None para partículas delicadas (ou teste formato mobile comprimido sem halos); mipmaps normalmente OFF para UI 2D; bilinear. Nas bordas dos sprites use Clamp para os streaks quando esticados, para evitar repetição de borda.

**Preset sugerido:** comprimento da cauda visível 2–3 células, núcleo 0,08–0,14 célula, glow geral 0,3–0,65 célula, fitas com oscilação baixa (0,12–0,25 célula), cerca de 8–14 sparkles, 20–45 fragmentos por linha longa (não simultaneamente em toda a linha), impactos por peça 80–150 ms. Use pooling e configure no Inspector.

**Previews** são apenas ilustrações; não são sprites de produção. O mock original da conversa é a referência final e deve ser fornecido ao Claude junto deste ZIP.

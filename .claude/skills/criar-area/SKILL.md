---
name: criar-area
description: Use quando o usuário quiser criar, editar ou ajustar uma área (building) do lobby do Magic Mail/Correio Mágico a partir de imagens que ele gerou no ChatGPT — fundo vazio + objetos separados. Cobre conferir as imagens, montar as camadas no tamanho do fundo, posicionar, gerar prévia e registrar a área em RoyalAvesProgression. Frases: "cria uma área nova", "monta as camadas", "ajusta a posição do móvel", "novo building".
---

# Criar área (pipeline manual)

O usuário gera as imagens no ChatGPT e entrega: **1 fundo sem nenhum móvel** e **os objetos soltos** (uma folha por melhoria, PNG com transparência). Você faz o resto. Não gere imagens por API.

## Modelo de dados (não mude sem pedir)
`Assets/RoyalAves/Resources/RoyalAvesProgression.asset` → `AreaProgressionConfig.areas[]`:
`AreaDefinition { id, title, background, tasks[], reward }`, `AreaTask { id, title, starCost, layer, icon }`.
- `layer` = PNG **do mesmo tamanho do fundo**, com o móvel já na posição final.
- Ordem das tasks = ordem de desenho (trás → frente) = ordem de compra.
- Código: `Assets/RoyalAves/Scripts/Meta/AreaProgressionConfig.cs`. Editor: menu **Royal Aves > Editor de áreas** (`Editor/RoyalAvesAreaEditor.cs`).

## Passos
1. **Conferir as entradas.** Veja cada imagem (Read). O fundo tem de estar realmente vazio; cada folha tem de ter objetos separados e com alpha. Se algo faltar ou o estilo destoar das áreas existentes (`Assets/RoyalAves/Art/Backgrounds/`), diga o que pedir de novo ao ChatGPT antes de seguir.
2. **Montar as camadas.** Ferramenta existente: `Tools/montar-camadas-area/montar.py` (recorta objetos com `split.py`, posiciona por `posicoes.json` em frações do fundo: `cx`, `cy` = centro, `w` = largura relativa) e grava `previa-composta.png`. Requer Python com Pillow e numpy (nesta máquina o `python` do PATH é só o atalho da Store; use `py` ou instale antes de rodar). Copie `posicoes.json` como base de uma área nova, não sobrescreva a da agência.
3. **Conferir a prévia.** Abra `previa-composta.png`, compare com a imagem de referência do usuário e ajuste `cx/cy/w` até casar. Itere até ficar certo; só então siga.
4. **Nomear na pasta da área** `Assets/RoyalAves/Art/Backgrounds/<id-da-area>/`:
   - `bg-<algo>.png` (fundo), `NN-nome.png` (camadas, NN = ordem), `icone-NN-nome.png` (ícones).
5. **Registrar.** Peça ao usuário para usar *Editor de áreas > Importar pasta de imagens…* (ou, com a Unity aberta, edite via ferramenta de editor). Defina `starCost` por etapa e a recompensa do baú com o usuário — **não invente custos**; sugira, mas confirme.
6. **Validar.** O Editor de áreas mostra avisos (tamanho de camada ≠ fundo, ícone faltando, id repetido). Zere os avisos.

## Regras
- Escreva Assets/.meta juntos; imagens (png/psd) vão por Git LFS (ver `.gitattributes`). Nunca commite/push sem o usuário pedir.
- Não renomeie arquivos em `Resources/` nem prefabs carregados por string (ver CLAUDE.md).
- `RoyalAvesAreaArtBuilder.cs` é a versão fixa da área "agencia"; não use como molde para áreas novas, use o Editor de áreas.
- Com a Unity aberta não dá para compilar em batchmode: peça ao usuário para focar o Editor e informar erros do Console.

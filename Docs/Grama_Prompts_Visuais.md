# Grama — revisão de densidade e cor

Revisão anterior, substituída pelo tapete de folhas arredondadas solicitado pelo usuário. Prompts atuais: [Grama_Prompts_Tapete_Quadrado.md](Grama_Prompts_Tapete_Quadrado.md).

Ferramenta: `imagegen` integrada, edição com transparência real, sem alteração posterior de pixels por script.

Destinos finais: `Assets/RoyalAves/Art/Obstacles/grass.png` (inicial, duas camadas, mais clara e fechada) e `Assets/RoyalAves/Art/Obstacles/grass-sparse.png` (após dano, uma camada, mais escura). Os GUIDs são mantidos para atualizar o piso e a meta. Os originais estão em `Backups/GrassFloor-original`; a revisão em `Backups/GrassFloor-v2`.

## Estado inicial — claro

Alvo da edição: `Assets/RoyalAves/Art/Obstacles/grass.png` original, copiado do projeto inicial.

Use case: precise-object-edit. Asset type: transparent 2D floor sprite for a mobile match-3 game, INITIAL FULL GRASS, 2 layers remaining. Edit target: the provided original grass sprite. Keep its same illustrated cartoon leaf style, overhead/slightly tilted view, approximately square clump footprint, centered position and blade forms. Make this grass clearly MORE DENSE AND CLOSED: fill internal empty gaps with overlapping broad curved pointed grass blades, especially in the center and lower area, producing an almost continuous lush canopy. Preserve individual readable blade silhouettes and illustrated shading, do not turn it into a solid green block. Make the dominant initial color LIGHTER, a fresh bright light leaf green with soft lime-green highlights and medium green shadows; avoid excessive yellow, near-white glare or extremely dark forest green areas. This is the brighter first state, later replaced by a darker damaged state. Broad simplified details that read well behind playing pieces at mobile tile size. Keep light direction from upper-left. Same square canvas with true alpha transparency outside the clump; keep complete grass within about 5 percent transparent margin on every side, fill roughly 90 percent of canvas width and height. No soil base, no square floor tile, no frame, no flowers, no stems protruding beyond the footprint, no insects, no text, no logo, no background/checkerboard drawing. Single sprite only, no comparison sheet. Preserve the existing game's art style rather than photo realism.

## Estado seguinte — escuro

Alvo da edição: a versão clara produzida acima, para manter a composição entre danos.

undefined

const fs=require('fs'),path=require('path');
const source=fs.readFileSync(path.join(__dirname,'stage-fast-forward-v93.cjs'),'utf8');
const script=source.replaceAll('fast-forward-v93','pet-tabs-v99').replaceAll('FastForwardV93','PetTabsV99').replace("deps('Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/pets-page-arrow-v6.png');deps('Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanNeutralPalette.shader');","deps('Assets/Plugins/AllIn1SpriteShader/Shaders/AllIn1SpriteShaderUiMask.shader');deps('Assets/Plugins/AllIn1SpriteShader/Shaders/AllIn1OneShaderFunctions.cginc');");
eval(script);

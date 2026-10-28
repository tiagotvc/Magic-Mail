# Atual: correção dos sprites — V16

A versão anterior usava formas genéricas nas barras, abas, badges e filtros. Esta revisão troca essas peças por 28 sprites raster separados, gerados com o ImageGen integrado usando o mock como referência.

- Barras: trilho azul-marinho discreto, preenchimento azul/verde com brilho e pontas arredondadas. Sprites com bordas de nove fatias preservam as pontas ao mudar o valor. A região dos números é reservada dentro do trilho.
- Badges: três variantes da mesma peça, com escudo dourado, pata e campo de texto separado. A arte acompanha a raridade do pet, inclusive após ordenar a coleção.
- Coleção: nova moldura, fundos próprios azul estrelado/outono/magia roxa, rodapés azul/cinza/roxo e painel taupe pintado. Cards e setas ficam ancorados perto do topo, eliminando o espaço excessivo acima da lista.
- Abas: fundo vermelho selecionado e roxo inativo, ícones próprios e cadeados separados para Aparência, Selos e Contratos.
- Demais peças: pata de nível, raio, confirmação de equipado, alimentação, informações e filtro de raridade com coroa.

O cabeçalho, o menu lateral, as setas aprovadas e o ornamento continuam compartilhados com a Incubadora. A instalação altera somente `Pets/CurrentProfile`; os componentes e IDs externos permanecem idênticos. Os pets e suas habilidades continuam exemplos de apresentação, sem conceder propriedade ou efeitos de combate.

Os PNGs originais ficam em `Assets/RoyalAves/Art/UI/{Button,Tab,Icon,Menu}/RoyalPostman/Pets/`, com os respectivos assets Sprite em `Sprites/`. Nenhum pixel dos originais foi repintado ou ampliado por código. Os retângulos de Sprite apenas delimitam o componente e suas margens transparentes. O importador mantém as dimensões nativas e a transparência.

Prompts, referências de cada geração/edição, arquivos originais, destinos, dimensões e hashes: [PETS-CURRENT-V16.json](PETS-CURRENT-V16.json).

Verificação do prefab instalado, sem reconstrução: seleção de pets, habilidades, magia, raridades após ordenação, tooltip, bloqueios, valores zero/intermediário/cheio das barras, salvamento, Incubadora e regressões anteriores passaram. Layouts 360×800, 720×1280 e 1080×1920; captura de detalhe em 2160×3840. Editor/runtime: zero pixels diferentes. Compilação integral de runtime/editor passou.

[Captura final](review/pets-current-v16/pets-current-lumi-720.png) · [Validação do instalado](review/pets-current-v16/installed-prefab-validation.txt)

Use **Recarregar** no editor após esta atualização externa. Não salve uma cópia antiga sobre o prefab revisado.

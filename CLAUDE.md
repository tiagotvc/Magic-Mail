# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Visão geral

**Magic Mail**: jogo mobile match-3 (Android/iOS) feito em **Unity 6000.6.0f1**, URP 2D. Todo o jogo é o template pago **Sweet Sugar** (Candy Smith, Asset Store), em `Assets/SweetSugar/`. Ainda não há pasta de código próprio. O `applicationIdentifier` Android continua `com.candysmithgames.sweetsugar`.

## Economia de tokens: onde NÃO procurar

- Busque com `git grep` / `git ls-files`: eles olham só arquivos versionados e pulam `Library/` (GBs, com o código-fonte de todos os pacotes em `Library/PackageCache`).
- Gerados/ignorados: `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.sln`.
- Terceiros, não editar: `Assets/SweetSugar/LeanTween/`, `Assets/SweetSugar/Unity-Reorderable-List-master/`, `Assets/MobileDependencyResolver/`.
- Sobras do template 2D do Unity, fora do build: `Assets/Welcome/`, `Assets/Scenes/SampleScene.unity`, `Assets/Settings/Scenes/`.
- Não ler em massa: `Assets/SweetSugar/Resources/Levels/` (201 `Level_N.asset` + `Targets/TargetLevelN.asset`), `.unity` e `.prefab` (YAML enorme; use grep por nome/GUID).
- Arquivos gigantes: `LevelManager.cs` e `Blocks/Square.cs` têm ~1700 linhas cada. Leia por trechos (grep + offset).

## Comandos

Não há scripts de build, lint ou testes. O `com.unity.test-framework` está instalado, mas não existe nenhum teste nem `.asmdef` no jogo: tudo compila em `Assembly-CSharp` / `Assembly-CSharp-Editor`.

Checar compilação sem abrir o editor (falha se o projeto já estiver aberto no Unity):
```
"/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath . -logFile -
```

## Arquitetura (Sweet Sugar)

- **Cenas do build**: `main` (tela inicial) → `game` (mapa dinâmico + gameplay na mesma cena) ou `gameStatic` (mapa estático). Qual das duas é usada vem de `Resources/Scriptable/MapSwitcher.asset` (`GetSceneName()`, menu *Sweet Sugar/Scenes/Map switcher*).
- **Singletons** expostos como `public static X THIS` (às vezes `Instance`). O núcleo é `Core/LevelManager.cs`: máquina de estados `GameState` (Map, PrepareGame, Playing, Win, GameOver…, linha 43) via `gameStatus`, matching, bloqueios e sincronização de animações.
- **Fases**: `LoadingManager.LoadForPlay(n)` carrega `Resources/Levels/Level_N.asset` (`LevelContainer` → `LevelData`). Cada fase tem `fields` (subfases), e cada field vira um `FieldBoard` com uma grade de `Square`. Os objetivos ficam em `Levels/TargetEditorScriptable.asset` + `Targets/`. Edite fases e objetivos pelo menu **Sweet Sugar/Level editor** (`Editor/LevelMakerEditor.cs`), não no YAML.
- **Progresso do jogador**: `PlayerPrefs`, gerenciado em `Core/InitScript.cs` (chaves `Gems`, `Lifes`, `OpenLevel`, `Boost_*`, `Level.{000}.StarsCount`…).
- **Carregamento por string (`Resources.Load`)**: renomear ou mover estes arquivos quebra o jogo sem erro de compilação:
  - Configurações em `Resources/Scriptable/`: `AdditionalSettings`, `DebugSettings`, `PoolSettings`, `AdManagerScriptable`, `UnityAdsID`, `LevelPlayID`, `MapSwitcher`, `WinReward` (acessíveis no menu *Sweet Sugar/Settings*).
  - Prefabs de blocos: `Resources.Load("Blocks/" + sqType)`, então o nome do prefab tem que bater com o valor do enum.
  - Também `Items/`, `Boosts/` e `Border`.
- **Localização**: `Resources/Localization/English.txt`, `Russian.txt`.

## Armadilha: scripting defines automáticos

`Scripts/Editor/PostImporting.cs` roda a **cada importação de asset** e reescreve os defines de Standalone/Android/iOS/WSA:
- Por pacote no `manifest.json`: `com.unity.purchasing` → `UNITY_INAPPS`, `com.unity.ads` → `UNITY_ADS`, `com.unity.services.levelplay` → `LEVELPLAY`.
- Por pasta existente: `Assets/GoogleMobileAds` → `GOOGLE_MOBILE_ADS`, `Assets/FacebookSDK` → `FACEBOOK` (e aí `PLAYFAB`/`GAMESPARKS`), além de Chartboost, Appodeal, EPSILON e GetSocial.

Não defina símbolos à mão em Player Settings: eles serão sobrescritos. Os SDKs de Facebook/PlayFab/AdMob/Appodeal não estão no projeto, então o código dentro de `#if PLAYFAB`, `#if FACEBOOK` etc. **não é compilado**, e erros ali não aparecem.

## Git

- Binários (png, psd, áudio, fontes, pdf, dll…) vão por **Git LFS** conforme `.gitattributes`. Sempre versione o `.meta` junto com o asset.
- Remote: `tiagotvc/Magic-Mail` (**público**; o SweetSugar tem licença que proíbe redistribuição). O `gh` está logado como `ilustrejr`, e o credential manager do Git não tem login do GitHub. Para dar push:
  `git -c credential.helper= -c "credential.helper=!gh auth git-credential" push`
- Scripts sem o cabeçalho "Candy Smith" são adições fora do template original: `AdsEvents/LevelPlayIntegration.cs`, `Editor/ExportGitStaged.cs`, `Editor/LayerTransferTool.cs`, `GUI/CharAnimationController.cs`, `GUI/Utils/ReferenceRestorer.cs`.

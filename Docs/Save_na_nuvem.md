# Save na nuvem (Correio Mágico)

## Como funciona
- `Assets/RoyalAves/Scripts/Save/SaveGame.cs`: sabe quais chaves do PlayerPrefs são progresso (moedas, vidas, fase, estrelas, power-ups, MetaProgress, Pets, Eventos...) e tira/aplica uma "foto" (`SaveSnapshot`, JSON).
- `RoyalCloudSave` (nasce sozinho ao abrir o jogo): entra na conta automaticamente, baixa o save da nuvem, compara com o do aparelho e o **mais adiantado vence** (fase mais alta; empate, o mais recente). Depois envia sozinho a cada mudança (máx. 1x/20 s) e ao minimizar/fechar.
- Nuvem na frente: aplica no aparelho e recarrega a cena (só no mapa, nunca no meio de uma fase).
- Fica só no aparelho (de propósito): limites de vídeo (3h/6h), consentimento de anúncios (`npa`), `Rated`.
- Sistema novo (coleção, guild...): chame `SaveGame.RegisterKeys("chave-json")` no início do jogo.

## Passo a passo para ligar
1. **Unity Cloud:** Edit > Project Settings > Services, ligar o projeto a um Unity Project ID (login na conta Unity).
2. Menu **Royal Aves > Save na nuvem > Instalar Cloud Save (Unity)**. Aguarde compilar (o símbolo `ROYAL_CLOUDSAVE` liga sozinho).
3. **Android (login automático):**
   - Menu *Instalar Google Play Games*.
   - Play Console > Play Games Services: criar o serviço para `com.oakhouse.royalmagic`, credencial OAuth "Web" (Web client ID + secret).
   - Unity Dashboard > Authentication > Identity providers > Google Play Games: colar Client ID e Secret.
   - Unity: Window > Google Play Games > Setup > Android (colar o resource XML e o Web client ID).
4. **iOS (login automático):**
   - Menu *Instalar Apple GameKit*.
   - Apple Developer: App ID `com.oakhouse.royalmagic` com a capability **Game Center**.
   - Unity Dashboard > Authentication > Identity providers > Apple Game Center: colar o Bundle ID.
5. Botão "Salvar Progresso" da tela de configurações: adicionar o componente **Cloud Save Button** ao botão (e um TMP no campo Status Text, se quiser).
6. Testar: menu *Mostrar resumo do save local*; em aparelho real, jogar uma fase, desinstalar, instalar de novo e conferir.

Sem os passos 3/4 o jogo usa conta anônima na nuvem (funciona, mas só recupera no mesmo aparelho).

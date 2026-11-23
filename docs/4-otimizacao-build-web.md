# 4 - Otimização do build Web (Poki)

Guia para deixar o build WebGL do Olo o menor possível na hora de publicar (ex.: Poki).
Tudo aqui já foi testado uma vez em 19/11/2026: o build caiu de **11,2 MB → 9,5 MB** (Brotli) e depois foi revertido para seguir desenvolvendo com o projeto "completo".

## Progresso

- [ ] 1. Managed Stripping Level → High (no Build Profile)
- [ ] 2. Tirar o Post Process Data do Renderer2D
- [ ] 3. Ligar o stripping de variantes da URP
- [ ] 4. Remover pacotes e módulos não usados
- [ ] 5. (Opcional) Exception Support → None
- [ ] 6. Fazer o build e testar o jogo inteiro
- [ ] 7. Compressão para envio à Poki

---

## Antes de começar: de onde vem o peso

No primeiro build (quase vazio) os arquivos eram:

| Arquivo | Tamanho (Brotli) | O que é |
|---|---|---|
| `.wasm.br` | 6,7 MB | Código da engine + pacotes (inclui ~1 MB de código Burst) |
| `.data.br` | 2,7 MB | Assets — quase tudo **shaders da URP** que o jogo não usa |
| `.framework.js.br` + loader | ~0,1 MB | JavaScript de inicialização |

As texturas do jogo são pequenas, então o ganho está em **código** e **shaders**.

**Importante:** as configurações de build ficam no Build Profile
`Assets/Settings/Build Profiles/Web - Mobile - Release.asset`. Ele **sobrescreve** o Player Settings global, então mude as opções pela janela **File > Build Profiles > Web - Mobile - Release > Player Settings Overrides**.

O que **já vem certo** nesse profile (não precisa mexer): Development Build OFF, Strip Engine Code ON, IL2CPP Code Generation "Optimize for code size", API Compatibility .NET Standard 2.1, Debug Symbols OFF, Compression Brotli, Data Caching ON, Code Optimization "Disk Size with LTO".

---

## 1. Managed Stripping Level → High

No Build Profile: **Player Settings > Other Settings > Optimization > Managed Stripping Level = High**.

**Por quê?** O padrão é *Minimal*, que mantém quase todo o código C# dos pacotes. Com *High* a Unity remove tudo o que o jogo não chama, diminuindo o `.wasm`.

**Cuidado:** código usado só por reflexão (ex.: algum pacote que procura classes pelo nome) pode ser removido por engano. Se algo funcionar no Editor mas quebrar só no build, crie um `Assets/link.xml` preservando o assembly afetado:

```xml
<linker>
  <assembly fullname="NomeDoAssembly" preserve="all"/>
</linker>
```

✅ **Teste:** o build roda e tudo funciona igual ao Editor.

## 2. Tirar o Post Process Data do Renderer2D

Selecione `Assets/Settings/Renderer2D.asset` e, em **Post-processing > Data**, deixe **None**.

**Por quê?** Enquanto esse campo estiver preenchido, a URP coloca no build todos os shaders de pós-processamento (Bloom, Depth of Field, TAA, Motion Blur...). A câmera do Olo não usa pós-processamento (`Post Processing` desmarcado na câmera), então são só bytes a mais.

**Se um dia o jogo usar pós-processamento** (ex.: Bloom), pule este passo.

✅ **Teste:** o visual do jogo continua idêntico.

## 3. Ligar o stripping de variantes da URP

Em **Project Settings > Graphics > URP Global Settings** (ou no asset `UniversalRenderPipelineGlobalSettings.asset`), marque:

- **Strip Unused Post Processing Variants**
- **Strip Unused 2D Variants** (se aparecer)

**Por quê?** Cada shader da URP tem várias "variantes" (combinações de features). Essas opções removem as que o projeto não usa.

✅ **Teste:** sprites e luzes 2D aparecem normalmente no build.

## 4. Remover pacotes e módulos não usados

Pelo **Window > Package Manager** (ou editando `Packages/manifest.json`), remova o que veio do template 2D e o jogo não usa. Na última verificação nenhum asset/script usava:

**Pacotes:**
- `com.unity.2d.animation` e `com.unity.2d.spriteshape` — trazem jobs **Burst** (~1 MB de código no build)
- `com.unity.2d.psdimporter`, `com.unity.2d.aseprite` — importadores de PSD/Aseprite
- `com.unity.2d.tilemap.extras`
- `com.unity.timeline`
- `com.unity.visualscripting`

**Módulos (Package Manager > Built-in):**
Terrain, Terrain Physics, Cloth, Vehicles, Wind, Video, Umbra, AI, Unity Analytics

**Por quê?** Menos pacotes = menos código e menos shaders para a Unity incluir. O Strip Engine Code ajuda, mas não remove tudo.

**Antes de remover, confira** se o jogo passou a usar algum deles (ex.: se você adicionou Tilemaps, animação de esqueleto 2D, arquivos `.psd`/`.aseprite`, Timeline). Física **2D** é outro módulo e deve ficar.

✅ **Teste:** o Console fica sem erros depois da Unity resolver os pacotes.

## 5. (Opcional) Exception Support → None

No Build Profile: **Player Settings > Publishing Settings > Enable Exceptions = None**.

**Por quê?** Diminui bastante o `.wasm`, porque a Unity deixa de gerar o código de tratamento de exceções.

**Custo:** qualquer exceção (ex.: um `NullReferenceException`) **trava o jogo de vez** no navegador. Só faça isso no build final, depois de testar bem.

✅ **Teste:** jogue todas as fases sem travar.

## 6. Fazer o build e testar

**File > Build Profiles > Web - Mobile - Release > Build**. Compare o tamanho da pasta `Build/` com a referência (11,2 MB sem otimizar / 9,5 MB com os passos 1–4).

Para testar localmente, use **Build And Run** (a Unity sobe um servidor que entende Brotli) — abrir o `index.html` direto do disco não funciona.

**Dica:** se aparecer uma pasta `Data/` na raiz do projeto depois do build, ela é um resto do Burst. Não comite; pode apagar ou adicionar `/Data/` no `.gitignore`.

✅ **Teste:** jogo completo funcionando no navegador, inclusive no celular.

## 7. Compressão para envio à Poki

No Build Profile: **Publishing Settings > Compression Format = Disabled** (só para o build enviado).

**Por quê?** A Poki comprime os arquivos no servidor dela; ela pede o build sem compressão. Para hospedar em outro lugar (site próprio, itch.io), mantenha **Brotli**.

O tamanho em disco vai parecer maior sem compressão — isso é normal, o que o jogador baixa continua comprimido.

✅ **Teste:** seguir o checklist de envio da documentação da Poki para Unity.

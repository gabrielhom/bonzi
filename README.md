# Bonzi

O BonziBuddy de volta à área de trabalho, com os sprites, as animações e os sons originais, e **sem nada de malicioso**: sem rede, sem propaganda, sem coletar nada. É um único `.exe` de Windows, sem instalador e sem dependências.

## Baixar

Pegue o `Bonzi.exe` na [última release](https://github.com/gabrielhom/bonzi/releases/latest) e abra. Precisa só do Windows 10 ou 11, que já vem com o .NET Framework 4.

O executável não é assinado, então o SmartScreen pode avisar na primeira vez: **Mais informações → Executar assim mesmo**. Se preferir, compile você mesmo (abaixo); são 400 linhas de C# para ler.

## O que ele faz

Ele aparece no canto inferior direito, por cima das outras janelas, cumprimenta conforme a hora do dia e fica ali, piscando e fazendo as animações de descanso do original.

- **Clique** nele para uma cutucada, uma piada ou uma curiosidade.
- **Arraste** para levar para qualquer lugar da tela.
- **Botão direito** abre o menu:
  - Conta uma piada
  - Fala uma curiosidade
  - Que horas são?
  - Canta uma música (*Daisy Bell*, a mesma que o Bonzi original cantava)
  - Lê o que eu copiei (lê em voz alta o texto da área de transferência)
  - Puxar assunto sozinho (a cada poucos minutos ele lembra de beber água, alongar, fechar abas…)
  - Fica quieto
  - Tchau, Bonzi

Tudo o que ele fala aparece num balão amarelo, escrito palavra por palavra no ritmo da voz.

## Voz

Usa as vozes de texto para fala que já vêm no Windows. Se houver uma voz em português do Brasil instalada (a Maria, por exemplo), ele fala em português, com o tom grave do Bonzi. Para cantar, ele procura uma voz em inglês e monta a melodia nota a nota com SSML.

Para instalar a voz em português: **Configurações → Hora e idioma → Fala → Adicionar vozes**.

## Privacidade

Ele não abre nenhuma conexão de rede. A única coisa que escreve no disco são os sons embutidos, copiados uma vez para `%TEMP%\Bonzi-*.mp3`, porque o player do WPF só toca arquivos. A área de transferência só é lida quando você pede, pelo item **Lê o que eu copiei**.

## Compilar

```cmd
build.cmd
```

Usa o `csc.exe` que já vem no Windows (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319`), então não precisa instalar Visual Studio nem SDK. Por isso o código é C# 5: nada de `$""` nem `?.`. Os assets vão embutidos no executável como recursos.

## Como funciona

- `assets/agent.json` e `assets/map.png` são o personagem do Microsoft Agent extraído do `Bonzi.acs` original pelo [clippy.js](https://www.npmjs.com/package/clippyjs) (pacote `clippyjs@0.1.0`, `dist/agents/bonzi`): a folha de sprites, a lista de quadros de cada animação e os sons.
- `Bonzi.cs` é uma janela WPF transparente que reproduz o mesmo algoritmo do Microsoft Agent: quadros com duração própria, ramificações aleatórias com peso e caminhos de saída para as animações em loop.
- O sprite é desenhado em escala 2x com *nearest neighbor*: o Agent desenhava 1:1 em telas de 800x600, então em 1080p o dobro fica com o tamanho de antigamente e com os pixels nítidos.

## Release

Uma tag `v*` dispara o workflow `.github/workflows/release.yml`, que compila no Windows e publica o `Bonzi.exe` numa release:

```bash
git tag v1.2.3 && git push origin v1.2.3
```

## Créditos

O personagem, os sprites e os sons do BonziBuddy pertencem aos seus donos originais (BONZI Software, 1999). Eles vêm do pacote clippy.js, e este projeto não tem relação com a empresa. O código em `Bonzi.cs` é deste repositório.

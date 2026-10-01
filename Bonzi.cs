// Bonzi: o amigo roxo de área de trabalho, com os sprites, animações e sons originais,
// e sem nada de malicioso: sem rede, sem propaganda, sem coletar nada.
// Compilar: build.cmd (usa o csc.exe que já vem no Windows; é C# 5, então nada de $"" nem ?.).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Speech.Synthesis;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// Dados do personagem do Microsoft Agent, extraídos do Bonzi.acs original pelo clippyjs (assets/agent.json).
class Agente { public int[] framesize; public Dictionary<string, Animacao> animations; }
class Animacao { public Quadro[] frames; public bool useExitBranching; }
class Quadro { public int duration; public int[][] images; public string sound; public int? exitBranch; public Ramos branching; }
class Ramos { public Ramo[] branches; }
class Ramo { public int frameIndex; public int weight; }

class Bonzi : Window
{
    [STAThread]
    static void Main() { new Application().Run(new Bonzi()); }

    static readonly string[] Piadas = {
        "O que o pato disse para a pata? Vem quá!",
        "Qual é o café mais perigoso do mundo? O ex-presso.",
        "O que é um pontinho amarelo no céu? Um yellowcóptero.",
        "O que o tomate foi fazer no banco? Tirar extrato.",
        "Por que a plantinha não foi atendida no hospital? Porque só tinha médico de plantão.",
        "Qual é o contrário de volátil? Vem cá, sobrinho.",
        "O que o zero disse para o oito? Que cinto bonito!",
        "Como o elétron atende o telefone? Próton!",
        "Qual é o doce preferido do átomo? Pé de molécula.",
        "Sabe por que eu sou roxo? Fiquei prendendo a respiração esperando o Windows 98 ligar.",
    };
    static readonly string[] Curiosidades = {
        "Gorilas compartilham cerca de 98% do DNA com a gente. Somos quase primos!",
        "Polvos têm três corações e sangue azul.",
        "Um dia em Vênus dura mais do que um ano inteiro em Vênus.",
        "Bananas são levemente radioativas, por causa do potássio. Mas pode comer tranquilo!",
        "Tubarões existem há mais tempo do que as árvores.",
        "No cavalo-marinho, quem fica grávido é o macho.",
        "Os flamingos nascem cinzentos e ficam cor-de-rosa por causa do que comem.",
        "Os coalas dormem até 20 horas por dia. Meu sonho.",
        "Um grupo de gorilas é liderado por um macho mais velho, chamado costas-prateadas.",
        "A Torre Eiffel fica até 15 centímetros mais alta no verão, porque o ferro dilata com o calor.",
        "O BonziBuddy original saiu em 1999 e ficou com fama de adware. Eu sou a versão que não esconde nada.",
    };
    static readonly string[] Sozinho = {
        "Lembrete de amigo: bebe um copo de água.",
        "Tá trabalhando muito? Levanta e estica as costas um pouquinho.",
        "Só passando pra lembrar que eu não estou coletando nada seu. Juro de dedinho.",
        "Tá quieto aqui, hein?",
        "Se quiser ouvir uma piada, é só clicar em mim.",
        "Sabia que eu sei cantar? Clica com o botão direito em mim.",
        "Hoje é um ótimo dia pra fechar umas abas do navegador.",
    };
    static readonly string[] Cutucadas = {
        "Ei, isso faz cócegas!",
        "Oi! Tô aqui.",
        "Pode me arrastar pra onde quiser, eu não ligo.",
        "Cuidado com o pelo, acabei de pentear.",
    };

    // Daisy Bell (1892, domínio público), a música que o Bonzi original cantava.
    // Cada nota: sílaba, grau (0 = Sol3 ... 7 = Sol4) e duração em tempos.
    const string Daisy =
        "Day 7 3|zee 5 3|Day 3 3|zee 0 3|give 1 1|me 2 1|your 3 1|an 1 2|sir 3 1|do 0 5|" +
        "I'm 4 3|half 7 3|cray 5 3|zee 3 3|all 1 1|for 2 1|the 3 1|love 4 2|of 5 1|you 4 5|" +
        "But 0 1|you'll 3 1|look 5 1|sweet 4 3|up 0 1|on 3 1|the 5 1|seat 4 3|" +
        "of 5 1|a 6 1|bye 7 1|see 5 1|kul 3 1|built 4 2|for 0 1|two 3 6";
    // ponytail: pitch medido na voz Maria (134 a 230 Hz); acima de +50% o motor satura, então a oitava sai espremida
    static readonly string[] Notas = { "-50%", "-40%", "-15%", "+0%", "+30%", "+50%", "+65%", "+80%" };
    const string Ssml = "<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{0}'>";
    // O Agent desenhava 1:1 em telas de 800x600; em 1080p o dobro fica com o tamanho de antigamente.
    const double Escala = 2;

    const string Layout = @"
<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
  <StackPanel Name='fala' Width='240' Visibility='Hidden' Panel.ZIndex='1'>
    <Border Background='#FFFFE1' BorderBrush='#000' BorderThickness='1' CornerRadius='10' Padding='10,7'>
      <TextBlock Name='texto' TextWrapping='Wrap' FontFamily='Tahoma' FontSize='13' Foreground='#000'/>
    </Border>
    <Path Margin='110,-1,0,0' Data='M 0,0 L 4,16 L 18,0' Fill='#FFFFE1' Stroke='#000'/>
  </StackPanel>
  <Grid Name='sprite' Canvas.Left='30' Canvas.Top='200' Cursor='Hand'>
    <Image Name='camada0' RenderOptions.BitmapScalingMode='NearestNeighbor'/>
    <Image Name='camada1' RenderOptions.BitmapScalingMode='NearestNeighbor'/>
  </Grid>
</Canvas>";

    readonly SpeechSynthesizer voz = new SpeechSynthesizer();
    readonly Random rnd = new Random();
    readonly DispatcherTimer passo = new DispatcherTimer(), ocio = new DispatcherTimer(),
        esconde = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
    readonly Agente agente;
    readonly BitmapSource mapa;
    readonly string[] descanso;
    readonly Dictionary<int, BitmapSource> recortes = new Dictionary<int, BitmapSource>();
    readonly Dictionary<string, MediaPlayer> sons = new Dictionary<string, MediaPlayer>();
    readonly Image[] camadas;
    readonly FrameworkElement fala;
    readonly TextBlock texto;
    readonly MenuItem tagarela = new MenuItem { Header = "Puxar assunto sozinho", IsCheckable = true, IsChecked = true };
    Animacao anim;
    Quadro quadroAtual;
    int quadro, inicio;
    bool saindoAnim, animAcabou, ocupado, saindo, digitando;
    Action fimAnim;
    Prompt atual;
    string falando = "";

    Bonzi()
    {
        Title = "Bonzi";
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        using (var r = new StreamReader(Recurso("agent.json")))
            agente = new JavaScriptSerializer().Deserialize<Agente>(r.ReadToEnd());
        mapa = BitmapFrame.Create(Recurso("map.png"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        descanso = agente.animations.Keys.Where(k => k.StartsWith("Idle")).Concat(new[] { "Blink", "Blink" }).ToArray();

        // Quadro de 200x160 escalado, com 200 de espaço em cima pro balão. A cabeça fica ~64 px abaixo do topo do quadro.
        double w = agente.framesize[0], h = agente.framesize[1];
        Width = w * Escala + 60;
        Height = 200 + h * Escala;
        Left = SystemParameters.WorkArea.Right - Width - 20;
        Top = SystemParameters.WorkArea.Bottom - Height;
        var raiz = (Canvas)XamlReader.Parse(Layout);
        Content = raiz;
        var sprite = (Grid)raiz.FindName("sprite");
        sprite.Width = w;
        sprite.Height = h;
        sprite.LayoutTransform = new ScaleTransform(Escala, Escala);
        fala = (FrameworkElement)raiz.FindName("fala");
        Canvas.SetLeft(fala, 30 + w / 2 * Escala - 124);  // 124 = ponta do rabinho do balão
        Canvas.SetBottom(fala, (h - 50) * Escala);
        texto = (TextBlock)raiz.FindName("texto");
        camadas = new[] { (Image)raiz.FindName("camada0"), (Image)raiz.FindName("camada1") };

        var pt = voz.GetInstalledVoices().FirstOrDefault(v => v.VoiceInfo.Culture.Name == "pt-BR");
        if (pt != null) voz.SelectVoice(pt.VoiceInfo.Name);
        // Os eventos da voz chegam de outra thread; tudo volta pra thread da janela.
        voz.SpeakProgress += (s, e) => UI(() => {
            if (e.Prompt == atual && digitando) Mostra(e.CharacterPosition - inicio + e.CharacterCount);
        });
        voz.SpeakCompleted += (s, e) => UI(() => {
            if (e.Prompt != atual) return;
            Mostra(falando.Length);
            esconde.Start();
            Sai(() => { if (saindo) Anima("Hide", Close); else { ocupado = false; Ocioso(); } });
        });
        esconde.Tick += (s, e) => { esconde.Stop(); fala.Visibility = Visibility.Hidden; };
        passo.Tick += (s, e) => Passo();
        ocio.Tick += (s, e) => {
            ocio.Stop();
            if (ocupado) return;
            Anima(Sorteia(descanso), Ocioso);
            var a = anim;  // descansos que ficam em loop até alguém mandar sair
            if (a.useExitBranching) Depois(8, () => { if (anim == a && !ocupado) saindoAnim = true; });
        };

        var menu = new ContextMenu();
        Item(menu, "Conta uma piada", Piada);
        Item(menu, "Fala uma curiosidade", Curiosidade);
        Item(menu, "Que horas são?", Horas);
        Item(menu, "Canta uma música", Canta);
        Item(menu, "Lê o que eu copiei", LeCopiado);
        menu.Items.Add(new Separator());
        menu.Items.Add(tagarela);
        Item(menu, "Fica quieto", () => { voz.SpeakAsyncCancelAll(); fala.Visibility = Visibility.Hidden; });
        Item(menu, "Tchau, Bonzi", () => { saindo = true; Diz("Tchau! Foi bom passar esse tempo com você.", "Wave"); });
        sprite.ContextMenu = menu;
        sprite.MouseLeftButtonDown += (s, e) => {
            var antes = new Point(Left, Top);
            DragMove();
            if (antes != new Point(Left, Top)) return;
            var r = rnd.Next(3);
            if (r == 0) Diz(Sorteia(Cutucadas), "Surprised"); else if (r == 1) Piada(); else Curiosidade();
        };

        Loaded += (s, e) => {
            ocupado = true;
            Anima("Show", () => Diz(Saudacao() + " Eu sou o Bonzi, seu amigo roxo. Diferente do meu primo dos anos 2000, " +
                "eu não mostro propaganda, não espio nada e não mando seus dados pra ninguém. " +
                "Clica com o botão direito em mim pra ver o que eu sei fazer.", "Greet"));
            var conversa = new DispatcherTimer { Interval = TimeSpan.FromMinutes(3) };
            conversa.Tick += (s2, e2) => {
                if (tagarela.IsChecked && !ocupado && rnd.Next(2) == 0) Diz(Sorteia(Sozinho), "GetAttention");
            };
            conversa.Start();
        };
        Closed += (s, e) => voz.Dispose();
    }

    // Animação: mesmo algoritmo do Microsoft Agent/clippy.js (quadros, ramificação aleatória e caminho de saída).
    void Anima(string nome, Action fim)
    {
        Animacao a;
        if (!agente.animations.TryGetValue(nome, out a)) { if (fim != null) fim(); return; }
        anim = a;
        quadroAtual = null;
        quadro = 0;
        saindoAnim = animAcabou = false;
        fimAnim = fim;
        Passo();
    }

    void Sai(Action fim)
    {
        if (animAcabou) { fim(); return; }
        fimAnim = fim;
        saindoAnim = true;
    }

    void Passo()
    {
        int prox = Math.Min(Proximo(), anim.frames.Length - 1);
        bool mudou = quadroAtual == null || prox != quadro;
        quadro = prox;
        bool ultimo = quadro == anim.frames.Length - 1;
        // No último quadro de uma animação com saída, o Agent segura o quadro anterior até mandarem sair.
        if (!(ultimo && anim.useExitBranching) || quadroAtual == null) quadroAtual = anim.frames[quadro];
        if (mudou)
        {
            Desenha(quadroAtual);
            if (quadroAtual.sound != null) Som(quadroAtual.sound);
        }
        if (ultimo && (anim.useExitBranching ? saindoAnim : mudou))
        {
            passo.Stop();
            animAcabou = true;
            // Algumas animações (as palmas do Congratulate) acabam num quadro vazio; o Agent volta pra pose de descanso.
            if ((quadroAtual.images == null || quadroAtual.images.Length == 0) && anim != agente.animations["Hide"])
                Desenha(agente.animations["RestPose"].frames[0]);
            var f = fimAnim;
            fimAnim = null;
            if (f != null) f();
            return;
        }
        passo.Interval = TimeSpan.FromMilliseconds(Math.Max(10, quadroAtual.duration));
        passo.Start();
    }

    void Desenha(Quadro q)
    {
        var imgs = q.images ?? new int[0][];
        for (int i = 0; i < camadas.Length; i++) camadas[i].Source = i < imgs.Length ? Recorte(imgs[i]) : null;
    }

    int Proximo()
    {
        if (quadroAtual == null) return 0;
        if (saindoAnim && quadroAtual.exitBranch.HasValue) return quadroAtual.exitBranch.Value;
        if (quadroAtual.branching != null)
        {
            var r = rnd.NextDouble() * 100;
            foreach (var b in quadroAtual.branching.branches)
            {
                if (r <= b.weight) return b.frameIndex;
                r -= b.weight;
            }
        }
        return quadro + 1;
    }

    void Ocioso()
    {
        ocio.Interval = TimeSpan.FromSeconds(3 + rnd.NextDouble() * 7);
        ocio.Start();
    }

    BitmapSource Recorte(int[] xy)
    {
        BitmapSource b;
        int chave = xy[1] * 10000 + xy[0];
        if (!recortes.TryGetValue(chave, out b))
            recortes[chave] = b = new CroppedBitmap(mapa, new Int32Rect(xy[0], xy[1], agente.framesize[0], agente.framesize[1]));
        return b;
    }

    void Som(string n)
    {
        MediaPlayer p;
        if (!sons.TryGetValue(n, out p))
        {
            // MediaPlayer só toca de arquivo, então o som embutido vai uma vez pra pasta temporária.
            var arquivo = Path.Combine(Path.GetTempPath(), "Bonzi-" + n + ".mp3");
            if (!File.Exists(arquivo)) using (var f = File.Create(arquivo)) Recurso(n + ".mp3").CopyTo(f);
            p = new MediaPlayer();
            p.Open(new Uri(arquivo));
            sons[n] = p;
        }
        p.Position = TimeSpan.Zero;
        p.Play();
    }

    void Piada() { Diz(Sorteia(Piadas), "Pleased"); }
    void Curiosidade() { Diz(Sorteia(Curiosidades), "Explain"); }

    void Diz(string t, string gesto)
    {
        var abre = string.Format(Ssml, voz.Voice.Culture.Name) + "<prosody pitch='-50%' rate='-10%'>";
        inicio = abre.Length;
        // ponytail: texto com & < > ' " desloca o balão alguns caracteres até a fala terminar
        Fala(t, abre + SecurityElement.Escape(t) + "</prosody></speak>", true, gesto);
    }

    void Canta()
    {
        var en = voz.GetInstalledVoices().FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("en"));
        var sb = new StringBuilder(string.Format(Ssml, en != null ? en.VoiceInfo.Culture.Name : voz.Voice.Culture.Name));
        if (en != null) sb.AppendFormat("<voice name='{0}'>", en.VoiceInfo.Name);
        foreach (var nota in Daisy.Split('|'))
        {
            var p = nota.Split(' ');
            sb.AppendFormat("<prosody pitch='{0}' rate='-20%'>{1}</prosody><break time='{2}ms'/>",
                Notas[int.Parse(p[1])], SecurityElement.Escape(p[0]), int.Parse(p[2]) * 160);
        }
        if (en != null) sb.Append("</voice>");
        Fala("♪ Daisy, Daisy, give me your answer, do!\nI'm half crazy, all for the love of you.\n" +
             "But you'll look sweet upon the seat\nof a bicycle built for two! ♪", sb.Append("</speak>").ToString(), false, "Congratulate");
    }

    void Fala(string mostrado, string ssml, bool digita, string gesto)
    {
        ocupado = true;
        ocio.Stop();
        Anima(gesto, null);
        voz.SpeakAsyncCancelAll();
        esconde.Stop();
        falando = mostrado;
        digitando = digita;
        Mostra(digita ? 0 : mostrado.Length);
        fala.Visibility = Visibility.Visible;
        atual = voz.SpeakSsmlAsync(ssml);
    }

    void Horas()
    {
        var d = DateTime.Now;
        var h = d.Hour == 1 ? "Agora é uma hora" : "Agora são " + d.Hour + " horas";
        var m = d.Minute == 0 ? "" : " e " + d.Minute + (d.Minute == 1 ? " minuto" : " minutos");
        Diz(h + m + ". Hoje é " + d.ToString("dddd, d 'de' MMMM", new CultureInfo("pt-BR")) + ".", "Explain");
    }

    void LeCopiado()
    {
        string t;
        try { t = Clipboard.ContainsText() ? Clipboard.GetText().Trim() : ""; }
        catch { t = ""; }  // área de transferência ocupada por outro programa
        Diz(t.Length > 0 ? t : "Copia um texto com Control C que eu leio pra você.", "Explain");
    }

    void Mostra(int n)
    {
        var s = falando.Substring(0, Math.Max(0, Math.Min(falando.Length, n)));
        texto.Text = s.Length > 300 ? "…" + s.Substring(s.Length - 300) : s;
    }

    static string Saudacao()
    {
        var h = DateTime.Now.Hour;
        return h < 5 || h >= 18 ? "Boa noite!" : h < 12 ? "Bom dia!" : "Boa tarde!";
    }

    string Sorteia(string[] lista) { return lista[rnd.Next(lista.Length)]; }

    void UI(Action a) { Dispatcher.BeginInvoke(a); }

    static Stream Recurso(string nome) { return Assembly.GetExecutingAssembly().GetManifestResourceStream(nome); }

    static void Depois(double segundos, Action a)
    {
        var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(segundos) };
        t.Tick += (s, e) => { t.Stop(); a(); };
        t.Start();
    }

    static void Item(ContextMenu menu, string nome, Action a)
    {
        var i = new MenuItem { Header = nome };
        i.Click += (s, e) => a();
        menu.Items.Add(i);
    }
}

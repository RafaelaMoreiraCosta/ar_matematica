using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

/// <summary>
/// Script responsável por gerenciar vários marcadores de imagem em Realidade Aumentada.
/// 
/// Cada marcador cadastrado na Reference Image Library pode ter um modelo 3D próprio.
/// Quando a câmera detecta um marcador, o modelo correspondente aparece.
/// Quando outro marcador aparece, o modelo antigo some e o novo modelo aparece.
/// 
/// Além disso, exibe uma identificação contextual (título do tópico + ano escolar)
/// no canto da tela enquanto o marcador está sendo rastreado.
/// 
/// Também registra automaticamente o objeto ativo no AnimationManager,
/// para que o botão único de Pausar/Retomar sempre controle o objeto
/// que está sendo exibido no momento.
/// </summary>
public class ImageTargetMultiSpawner : MonoBehaviour
{
    /// <summary>
    /// Classe auxiliar usada apenas para aparecer no Inspector da Unity.
    /// 
    /// Ela representa a relação:
    /// nome da imagem cadastrada na Reference Image Library -> prefab 3D que deve aparecer,
    /// além dos metadados usados na identificação contextual exibida na tela.
    /// </summary>
    [System.Serializable]
    public class MarcadorObjeto
    {
        [Tooltip("Nome exato da imagem cadastrada na Reference Image Library da Unity")]
        public string nomeImagem;

        [Tooltip("Prefab 3D que deve aparecer quando esse marcador for detectado")]
        public GameObject prefab;

        [Header("Identificação Contextual")]
        [Tooltip("Nome do tópico exibido na tela (ex: Planificação de sólidos)")]
        public string tituloTopico;

        [Tooltip("Ano escolar sugerido, exibido entre parênteses (ex: 6º–7º ano)")]
        public string anoEscolar;
    }

    [Header("Configuracoes de Realidade Aumentada")]
    // Referência ao componente que gerencia as imagens que a câmera detecta no ambiente.
    // Normalmente esse componente fica no XR Origin / AR Session Origin.
    [SerializeField] private ARTrackedImageManager gerenciadorImagemAR;

    [Header("Marcadores e Modelos 3D")]
    // Lista configurável pelo Inspector.
    // Aqui cadastramos todos os pares:
    // nome do marcador -> modelo 3D correspondente (+ metadados de identificação).
    [SerializeField] private List<MarcadorObjeto> marcadores = new();

    [Header("Comportamento")]
    // Define se apenas um modelo 3D pode ficar visível por vez.
    //
    // true:
    // Quando um novo marcador for detectado, o objeto anterior será escondido.
    //
    // false:
    // Vários marcadores podem mostrar vários objetos ao mesmo tempo.
    [SerializeField] private bool permitirApenasUmObjetoAtivo = true;

    [Header("UI de Identificação Contextual")]
    [Tooltip("Texto (TextMeshPro - UI) que mostra o tópico e o ano escolar no canto da tela")]
    [SerializeField] private TextMeshProUGUI textoIdentificacao;

    // Dicionário interno usado para encontrar rapidamente qual prefab pertence a cada imagem.
    //
    // Exemplo:
    // "marcadorCubo" -> prefabCubo
    // "marcadorFracao" -> prefabFracao
    private readonly Dictionary<string, GameObject> prefabsPorImagem = new();

    // Dicionário interno usado para encontrar rapidamente os metadados (título/ano)
    // de cada marcador, sem precisar percorrer a lista toda vez.
    private readonly Dictionary<string, MarcadorObjeto> dadosPorImagem = new();

    // Dicionário interno usado para guardar os objetos já instanciados na cena.
    //
    // Isso evita que o script crie vários clones do mesmo modelo toda vez que o marcador for detectado.
    //
    // Exemplo:
    // "marcadorCubo" -> cubo já criado na cena
    // "marcadorFracao" -> fracao já criada na cena
    private readonly Dictionary<string, GameObject> objetosInstanciados = new();

    // Guarda o nome do marcador que está ativo no momento.
    // Isso ajuda a saber qual objeto deve ser escondido quando o rastreamento for perdido.
    private string imagemAtivaAtual;

    // Awake é chamado automaticamente quando o objeto nasce na cena,
    // antes do OnEnable e antes do primeiro frame.
    private void Awake()
    {
        // Monta os dicionários que relacionam nomes dos marcadores aos prefabs e metadados.
        // Fazemos isso uma vez no começo para não precisar procurar na lista toda hora.
        MontarDicionario();

        // Garante que a UI de identificação comece escondida,
        // já que nenhum marcador foi detectado ainda.
        if (textoIdentificacao != null)
        {
            textoIdentificacao.gameObject.SetActive(false);
        }
    }

    // OnEnable é chamado automaticamente quando este script, ou o objeto em que ele está, é ativado.
    private void OnEnable()
    {
        if (gerenciadorImagemAR != null)
        {
            // Inscreve o método AoAlterarImagens para ser chamado sempre que a Unity detectar,
            // atualizar ou remover alguma imagem rastreada pela câmera.
            gerenciadorImagemAR.trackablesChanged.AddListener(AoAlterarImagens);
        }
    }

    // OnDisable é chamado automaticamente quando este script, ou o objeto em que ele está, é desativado.
    private void OnDisable()
    {
        if (gerenciadorImagemAR != null)
        {
            // Remove a inscrição do evento para evitar chamadas desnecessárias,
            // erros de referência e possíveis problemas de memória.
            gerenciadorImagemAR.trackablesChanged.RemoveListener(AoAlterarImagens);
        }
    }

    /// <summary>
    /// Monta os dicionários internos ligando cada nome de imagem ao seu prefab e aos seus metadados.
    /// 
    /// Isso transforma a lista configurada no Inspector em estruturas mais rápidas
    /// para consulta durante o rastreamento da câmera.
    /// </summary>
    private void MontarDicionario()
    {
        // Limpa os dicionários antes de montar.
        // Isso evita dados duplicados caso esse método venha a ser chamado novamente no futuro.
        prefabsPorImagem.Clear();
        dadosPorImagem.Clear();

        // Percorre todos os marcadores configurados no Inspector.
        foreach (var item in marcadores)
        {
            // Se por algum motivo o item estiver nulo, pula para o próximo.
            if (item == null)
                continue;

            // Se o nome da imagem estiver vazio, avisamos no Console e ignoramos esse item.
            if (string.IsNullOrWhiteSpace(item.nomeImagem))
            {
                Debug.LogWarning("Existe um marcador sem nome configurado.");
                continue;
            }

            // Se o prefab não foi configurado, avisamos no Console e ignoramos esse item.
            if (item.prefab == null)
            {
                Debug.LogWarning($"O marcador '{item.nomeImagem}' está sem prefab configurado.");
                continue;
            }

            // Se já existir um marcador com esse mesmo nome, avisamos no Console e ignoramos o duplicado.
            //
            // Cada imagem da Reference Image Library deve apontar para apenas um prefab neste script.
            if (prefabsPorImagem.ContainsKey(item.nomeImagem))
            {
                Debug.LogWarning($"Marcador duplicado na lista: '{item.nomeImagem}'. Apenas o primeiro será usado.");
                continue;
            }

            // Adiciona o par nome da imagem -> prefab no dicionário.
            prefabsPorImagem.Add(item.nomeImagem, item.prefab);

            // Adiciona o par nome da imagem -> dados (título/ano) no dicionário.
            dadosPorImagem.Add(item.nomeImagem, item);
        }
    }

    /// <summary>
    /// Método chamado automaticamente sempre que o ARTrackedImageManager percebe alguma mudança
    /// nas imagens rastreadas pela câmera.
    /// 
    /// As mudanças podem ser:
    /// - imagem adicionada: a câmera acabou de encontrar um marcador;
    /// - imagem atualizada: o marcador continua visível, mudou de posição ou mudou de estado;
    /// - imagem removida: o marcador deixou de ser rastreado.
    /// </summary>
    private void AoAlterarImagens(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // Loop para imagens novas que a câmera acabou de encontrar.
        foreach (var imagem in args.added)
        {
            AtualizarImagem(imagem);
        }

        // Loop para imagens que já tinham sido encontradas e agora foram atualizadas.
        //
        // Isso acontece, por exemplo, quando a posição do marcador muda,
        // quando a câmera se move, ou quando o estado de rastreamento muda.
        foreach (var imagem in args.updated)
        {
            AtualizarImagem(imagem);
        }

        // Loop para imagens que foram removidas do rastreamento.
        //
        // Em versões recentes do AR Foundation, args.removed pode vir como pares internos,
        // por isso acessamos imagemRemovida.Value.
        foreach (var imagemRemovida in args.removed)
        {
            var imagem = imagemRemovida.Value;

            // Se a imagem removida for uma das imagens que conhecemos,
            // escondemos o objeto 3D correspondente e a identificação contextual.
            EsconderSeForImagemConhecida(imagem.referenceImage.name);
        }
    }

    /// <summary>
    /// Método central para decidir o que fazer com uma imagem rastreada.
    /// 
    /// Ele verifica:
    /// - se a imagem detectada está cadastrada na nossa lista;
    /// - se ela está sendo rastreada com precisão;
    /// - se devemos mostrar ou esconder o modelo 3D correspondente.
    /// </summary>
    private void AtualizarImagem(ARTrackedImage imagem)
    {
        // Pega o nome da imagem detectada.
        //
        // Esse nome precisa ser exatamente igual ao nome cadastrado
        // na Reference Image Library da Unity.
        string nomeImagem = imagem.referenceImage.name;

        // Se essa imagem não estiver cadastrada no nosso dicionário,
        // significa que este script não precisa fazer nada com ela.
        if (!prefabsPorImagem.ContainsKey(nomeImagem))
            return;

        // Verifica se a câmera está conseguindo rastrear a imagem com precisão.
        if (imagem.trackingState == TrackingState.Tracking)
        {
            // Se a imagem está sendo rastreada corretamente,
            // ativamos o objeto 3D correspondente a ela.
            AtivarObjetoDaImagem(nomeImagem, imagem);
        }
        else
        {
            // Se o rastreamento está limitado ou perdido,
            // escondemos o objeto para ele não ficar flutuando no lugar errado.
            EsconderSeForImagemConhecida(nomeImagem);
        }
    }

    /// <summary>
    /// Ativa o objeto 3D correspondente à imagem detectada.
    /// 
    /// Se o objeto ainda não existir, ele será instanciado.
    /// Se já existir, ele será apenas reativado e reposicionado.
    /// 
    /// Também atualiza a identificação contextual (título + ano) na tela
    /// e registra o objeto como alvo atual do botão de Pausar/Retomar.
    /// </summary>
    private void AtivarObjetoDaImagem(string nomeImagem, ARTrackedImage imagem)
    {
        // Se a regra for permitir apenas um objeto ativo por vez,
        // escondemos todos os outros objetos antes de mostrar o novo.
        if (permitirApenasUmObjetoAtivo)
        {
            EsconderTodosExceto(nomeImagem);
        }

        // Busca o objeto já criado, ou instancia um novo caso seja a primeira detecção.
        GameObject objeto = ObterOuCriarObjeto(nomeImagem, imagem);

        // Faz o objeto virar filho do marcador detectado.
        //
        // Assim, quando a Unity atualizar a posição/rotação do marcador,
        // o objeto 3D acompanhará automaticamente.
        objeto.transform.SetParent(imagem.transform);

        // Como o objeto agora é filho do marcador,
        // zeramos a posição local para ele ficar exatamente sobre a imagem.
        objeto.transform.localPosition = Vector3.zero;

        // Também zeramos a rotação local para ele seguir a rotação do marcador.
        // objeto.transform.localRotation = Quaternion.identity;

        // Garante que o objeto fique visível.
        objeto.SetActive(true);

        // Registra esse objeto como o alvo atual do botão único de Pausar/Retomar.
        //
        // Assim, não importa qual marcador esteja sendo exibido: o botão
        // sempre vai pausar/retomar a animação do objeto que está na tela agora.
        AnimationPauseController controlePausa = objeto.GetComponentInChildren<AnimationPauseController>();
        if (controlePausa != null && AnimationManager.Instance != null)
        {
            AnimationManager.Instance.SetActiveTarget(controlePausa);
        }

        // Guarda qual imagem está ativa no momento.
        imagemAtivaAtual = nomeImagem;

        // Atualiza o texto de identificação contextual no canto da tela.
        AtualizarTextoIdentificacao(nomeImagem);
    }

    /// <summary>
    /// Retorna o objeto 3D correspondente a uma imagem.
    /// 
    /// Se o objeto já foi instanciado antes, ele é reutilizado.
    /// Se ainda não existe, ele é criado com Instantiate.
    /// </summary>
    private GameObject ObterOuCriarObjeto(string nomeImagem, ARTrackedImage imagem)
    {
        // Verifica se já existe um objeto instanciado para esse marcador.
        //
        // Se existir, retornamos ele em vez de criar outro.
        if (objetosInstanciados.TryGetValue(nomeImagem, out GameObject objetoExistente))
        {
            return objetoExistente;
        }

        // Busca o prefab correspondente ao marcador.
        GameObject prefab = prefabsPorImagem[nomeImagem];

        // Instancia o prefab na posição e rotação atuais da imagem rastreada.
        //
        // O quarto parâmetro faz o novo objeto nascer como filho do marcador.
        GameObject novoObjeto = Instantiate(
            prefab,
            imagem.transform.position,
            imagem.transform.rotation,
            imagem.transform
        );

        // Garante que o objeto fique centralizado em relação ao marcador.
        novoObjeto.transform.localPosition = Vector3.zero;

        // Garante que o objeto siga a rotação do marcador sem rotação extra.
        // novoObjeto.transform.localRotation = Quaternion.identity;

        // Procura um Animator no objeto ou em algum de seus filhos.
        Animator animador = novoObjeto.GetComponentInChildren<Animator>();

        // Mantém o estado atual da animação caso o objeto seja desativado temporariamente.
        //
        // Isso evita que a animação recomece do zero toda vez que o marcador sair e voltar.
        if (animador != null)
        {
            animador.keepAnimatorStateOnDisable = true;
        }

        // Salva o objeto criado no dicionário para reutilizá-lo depois.
        objetosInstanciados.Add(nomeImagem, novoObjeto);

        // Retorna o objeto criado.
        return novoObjeto;
    }

    /// <summary>
    /// Esconde o objeto 3D correspondente ao nome da imagem,
    /// caso essa imagem esteja cadastrada e o objeto já tenha sido criado.
    /// 
    /// Também esconde a identificação contextual, caso esse marcador fosse o ativo,
    /// e limpa o alvo do AnimationManager para o botão de pausa não controlar
    /// um objeto que não está mais visível.
    /// </summary>
    private void EsconderSeForImagemConhecida(string nomeImagem)
    {
        // Tenta encontrar o objeto instanciado correspondente a esse marcador.
        //
        // Se não encontrar, significa que ele ainda não foi criado,
        // então não há nada para esconder.
        if (!objetosInstanciados.TryGetValue(nomeImagem, out GameObject objeto))
            return;

        // Esconde o objeto.
        objeto.SetActive(false);

        // Se o objeto escondido era o objeto ativo atual,
        // limpamos a variável de controle e escondemos a identificação contextual.
        if (imagemAtivaAtual == nomeImagem)
        {
            imagemAtivaAtual = null;

            // Evita que o botão único de Pausar/Retomar continue apontando
            // para um objeto que acabou de ser escondido.
            if (AnimationManager.Instance != null)
            {
                AnimationManager.Instance.SetActiveTarget(null);
            }

            if (textoIdentificacao != null)
            {
                textoIdentificacao.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Esconde todos os objetos instanciados, exceto o objeto ligado ao marcador informado.
    /// 
    /// Esse método é usado quando queremos garantir que apenas um objeto fique ativo por vez.
    /// </summary>
    private void EsconderTodosExceto(string nomeImagemQueDeveContinuar)
    {
        // Percorre todos os objetos já instanciados.
        foreach (var par in objetosInstanciados)
        {
            string nomeImagem = par.Key;
            GameObject objeto = par.Value;

            // Se este é o objeto que deve continuar visível,
            // pulamos para o próximo item.
            if (nomeImagem == nomeImagemQueDeveContinuar)
                continue;

            // Se o objeto existe, escondemos ele.
            if (objeto != null)
            {
                objeto.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Atualiza o texto exibido no canto da tela com o título do tópico
    /// e o ano escolar sugerido, de acordo com o marcador detectado.
    /// 
    /// Se o marcador não tiver título configurado, o texto fica escondido
    /// para não exibir uma UI vazia.
    /// </summary>
    private void AtualizarTextoIdentificacao(string nomeImagem)
    {
        // Se nenhuma referência de UI foi configurada no Inspector, não há o que fazer.
        if (textoIdentificacao == null)
            return;

        // Busca os metadados (título/ano) cadastrados para esse marcador.
        if (!dadosPorImagem.TryGetValue(nomeImagem, out MarcadorObjeto dados))
        {
            textoIdentificacao.gameObject.SetActive(false);
            return;
        }

        // Monta o texto no formato "Tópico (Ano escolar)".
        string texto = dados.tituloTopico;

        if (!string.IsNullOrWhiteSpace(dados.anoEscolar))
        {
            texto += $" ({dados.anoEscolar})";
        }

        // Se não houver nenhum texto configurado, escondemos a UI.
        if (string.IsNullOrWhiteSpace(texto))
        {
            textoIdentificacao.gameObject.SetActive(false);
            return;
        }

        // Atualiza e exibe o texto na tela.
        textoIdentificacao.text = texto;
        textoIdentificacao.gameObject.SetActive(true);
    }
}
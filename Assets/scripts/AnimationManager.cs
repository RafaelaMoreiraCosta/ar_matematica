using UnityEngine;
using TMPro;

/// <summary>
/// Gerenciador central de pausa. Existe um único botão na cena de
/// "Pausar/Retomar" que sempre atua sobre o objeto de animação
/// atualmente ativo (cubo, frações, vértices/arestas/faces ou
/// translação/rotação).
/// </summary>
public class AnimationManager : MonoBehaviour
{
    public static AnimationManager Instance { get; private set; }

    [Header("Texto do botão (opcional)")]
    [Tooltip("Componente de texto (TMP)")]
    [SerializeField] private TextMeshProUGUI textoBotao;

    [SerializeField] private string textoQuandoPausar = "Pausar";
    [SerializeField] private string textoQuandoRetomar = "Retomar";

    private AnimationPauseController _currentTarget;

    void Awake()
    {
        // Garante que só existe um gerenciador na cena
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    // Chame isso sempre que um novo objeto/tópico for exibido em cena.
    
    public void SetActiveTarget(AnimationPauseController target)
    {
        _currentTarget = target;
        AtualizarTextoBotao();
    }

    //Botão único chama este método
    public void TogglePause()
    {
        if (_currentTarget == null)
        {
            Debug.LogWarning("AnimationManager: nenhum objeto ativo definido ainda.");
            return;
        }
        _currentTarget.TogglePause();
        AtualizarTextoBotao();
    }

    public void PauseActive()
    {
        _currentTarget?.PauseAnimation();
        AtualizarTextoBotao();
    }

    public void ResumeActive()
    {
        _currentTarget?.ResumeAnimation();
        AtualizarTextoBotao();
    }

    public void RestartActive()
    {
        _currentTarget?.RestartAnimation();
        AtualizarTextoBotao();
    }

    public bool IsActivePaused()
    {
        return _currentTarget != null && _currentTarget.IsPaused;
    }


    //Atualiza o texto do botão de acordo com o estado atual (pausado ou não).
    //Chamado automaticamente sempre que o estado de pausa muda.
    
    private void AtualizarTextoBotao()
    {
        if (textoBotao == null)
            return;

        textoBotao.text = IsActivePaused() ? textoQuandoRetomar : textoQuandoPausar;
    }
}
using UnityEngine;

/// <summary>
/// Script genérico para pausar/retomar animações controladas por Animator.
/// Funciona para qualquer objeto animado: cubo se planificando, frações,
/// vértices/arestas/faces, translação/rotação — desde que a animação
/// esteja no componente Animator (Animation Controller).
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimationPauseController : MonoBehaviour
{
    private Animator _animator;
    private bool _isPaused = false;

    // Guarda a velocidade original
    private float _originalSpeed = 1f;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _originalSpeed = _animator.speed;
    }

    //Pausa a animação no frame atual
    public void PauseAnimation()
    {
        _animator.speed = 0f;
        _isPaused = true;
    }

    //Retoma a animação de onde parou
    public void ResumeAnimation()
    {
        _animator.speed = _originalSpeed;
        _isPaused = false;
    }

    //Alterna entre pausar e retomar
    public void TogglePause()
    {
        if (_isPaused)
            ResumeAnimation();
        else
            PauseAnimation();
    }

    //Reinicia a animação do zero 
    public void RestartAnimation()
    {
        _animator.speed = _originalSpeed;
        _animator.Play(0, -1, 0f); 
        _isPaused = false;
    }

    public bool IsPaused => _isPaused;
}
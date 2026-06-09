using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTargetSpawner : MonoBehaviour
{
    [Header("Configuracoes de Realidade Aumentada")]
    [SerializeField] private ARTrackedImageManager gerenciadorImagemAR;

    [Header("Modelo 3D")]
    [SerializeField] private GameObject modelo3D;

    [Header("Nome da imagem da biblioteca")]
    [SerializeField] private string nomeImagemAlvo = "marcadorCubo";

    private GameObject objetoInstanciado;

    private void OnEnable()
    {
        if (gerenciadorImagemAR != null)
        {
            gerenciadorImagemAR.trackablesChanged.AddListener(AoAlterarImagens);
        }
    }

    private void OnDisable()
    {
        if (gerenciadorImagemAR != null)
        {
            gerenciadorImagemAR.trackablesChanged.RemoveListener(AoAlterarImagens);
        }
    }

    private void AoAlterarImagens(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var imagem in args.added)
        {
            AtualizarImagem(imagem);
        }

        foreach (var imagem in args.updated)
        {
            AtualizarImagem(imagem);
        }

        foreach (var imagemRemovida in args.removed)
        {
            if (objetoInstanciado != null)
            {
                objetoInstanciado.SetActive(false);
            }
        }
    }

    private void AtualizarImagem(ARTrackedImage imagem)
    {
        if (imagem.referenceImage.name != nomeImagemAlvo)
            return;

        if (imagem.trackingState == TrackingState.Tracking)
        {
            if (objetoInstanciado == null)
            {
                objetoInstanciado = Instantiate(
                    modelo3D,
                    imagem.transform.position,
                    imagem.transform.rotation
                );
            }
            else
            {
                objetoInstanciado.transform.SetPositionAndRotation(
                    imagem.transform.position,
                    imagem.transform.rotation
                );

                objetoInstanciado.SetActive(true);
            }
        }
        else
        {
            if (objetoInstanciado != null)
            {
                objetoInstanciado.SetActive(false);
            }
        }
    }
}
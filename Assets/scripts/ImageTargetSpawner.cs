using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Script responsável por instanciar e gerenciar um modelo 3D sobre uma imagem rastreada em Realidade Aumentada.
/// </summary>
public class ImageTargetSpawner : MonoBehaviour
{
    [Header("Configuracoes de Realidade Aumentada")]
    // Referência ao componente que gerencia as imagens que a câmera detecta no ambiente
    [SerializeField] private ARTrackedImageManager gerenciadorImagemAR;

    [Header("Modelo 3D")]
    // O modelo 3D que vai aparecer em cima do marcador (ex: a peça de roupa sendo projetada no Pocket Fitting)
    [SerializeField] private GameObject modelo3D;

    [Header("Nome da imagem da biblioteca")]
    // O nome exato da imagem que foi cadastrada na Reference Image Library da Unity
    [SerializeField] private string nomeImagemAlvo = "marcadorCubo";

    // Variável interna para guardar a referência do objeto 3D depois que ele for criado na cena
    private GameObject objetoInstanciado;

    // OnEnable é chamado automaticamente quando este script (ou o objeto em que ele está) é ativado
    private void OnEnable()
    {
        if (gerenciadorImagemAR != null)
        {
            // "Inscreve" o nosso método AoAlterarImagens para ser avisado sempre que o status 
            // de qualquer imagem mudar (quando a câmera acha, atualiza ou perde o rastreio)
            gerenciadorImagemAR.trackablesChanged.AddListener(AoAlterarImagens);
        }
    }

    // OnDisable é chamado automaticamente quando o script é desativado
    private void OnDisable()
    {
        if (gerenciadorImagemAR != null)
        {
            // Remove a inscrição para evitar erros de memória e chamadas desnecessárias caso o objeto seja desligado
            gerenciadorImagemAR.trackablesChanged.RemoveListener(AoAlterarImagens);
        }
    }

    // Este método recebe as informações das imagens rastreadas a cada frame em que há mudanças
    private void AoAlterarImagens(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // 1. Loop para as imagens novas que a câmera acabou de encontrar
        foreach (var imagem in args.added)
        {
            AtualizarImagem(imagem);
        }

        // 2. Loop para as imagens que já foram encontradas e estão apenas se movendo
        foreach (var imagem in args.updated)
        {
            AtualizarImagem(imagem);
        }

        // 3. Loop para as imagens que saíram da visão da câmera
        foreach (var imagemRemovida in args.removed)
        {
            // Verifica se a imagem que sumiu é de fato o nosso alvo específico
            if (imagemRemovida.Value.referenceImage.name == nomeImagemAlvo)
            {
                // Se for o nosso alvo e o modelo estiver instanciado na cena, nós o escondemos
                if (objetoInstanciado != null)
                {
                    objetoInstanciado.SetActive(false);
                }
            }
        }
    }

    // Método central para avaliar o estado da imagem e decidir o que fazer com o modelo 3D
    private void AtualizarImagem(ARTrackedImage imagem)
    {
        // Se a imagem detectada não tiver o nome que queremos ("marcadorCubo"), ignoramos e saímos do método
        if (imagem.referenceImage.name != nomeImagemAlvo)
            return;

        // Verifica se a câmera está conseguindo rastrear a imagem com precisão
        if (imagem.trackingState == TrackingState.Tracking)
        {
            // Se o modelo ainda não foi criado (é a primeira vez que detectamos a imagem com sucesso)
            if (objetoInstanciado == null)
            {
                // Cria (Instancia) o modelo 3D na mesma posição e rotação da imagem real.
                // O quarto parâmetro (imagem.transform) faz o modelo 3D virar "filho" do marcador.
                // Assim, a própria Unity move o objeto junto com o marcador automaticamente, poupando processamento.
                objetoInstanciado = Instantiate(
                    modelo3D,
                    imagem.transform.position,
                    imagem.transform.rotation,
                    imagem.transform
                );
            }
            else
            {
                // Se o objeto já havia sido criado antes, só garantimos que ele volte a ficar visível
                objetoInstanciado.SetActive(true);
            }
        }
        else // Cai aqui caso o rastreamento esteja ruim (TrackingState.Limited) ou perdido (TrackingState.None)
        {
            // Se perdemos a precisão do marcador, escondemos o modelo 3D para ele não ficar flutuando no lugar errado
            if (objetoInstanciado != null)
            {
                objetoInstanciado.SetActive(false);
            }
        }
    }
}
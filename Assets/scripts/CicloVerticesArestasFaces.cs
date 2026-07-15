using System.Collections;
using UnityEngine;

/// <summary>
/// Script responsável por alternar automaticamente, em sequência, entre os destaques de
/// Vértices, Arestas e Faces 
/// </summary>
public class CicloVerticesArestasFaces : MonoBehaviour
{
    [System.Serializable]
    public class Etapa
    {
        [Tooltip("Identificação: (ex: Vértices)")]
        public string nomeEtapa;

        [Tooltip("Objeto base sempre visível (ex: Vertices, Arestas ou Faces)")]
        public GameObject objetoBase;

        [Tooltip("Objeto destacado (ex: Vertices_Destacadas)")]
        public GameObject objetoDestacado;

        [Tooltip("Texto exibido acima do objeto (ex: Texto_Vertices)")]
        public GameObject texto;
    }

    [Header("Etapas do Ciclo")]
    [Tooltip("Ordem em que as etapas serão exibidas.Vértices -> Arestas -> Faces")]
    [SerializeField] private Etapa[] etapas;

    [Header("Configuração do Ciclo")]
    [Tooltip("Quantos segundos cada etapa fica visível antes de passar para a próxima")]
    [SerializeField] private float tempoExibicao = 3f;

    [Tooltip("Ao terminar a última etapa o ciclo volta para a primeira automaticamente")]
    [SerializeField] private bool repetirEmLoop = true;

    [Tooltip("Ciclo começa sozinho assim que o objeto é ativado na cena")]
    [SerializeField] private bool iniciarAutomaticamente = true;

    // Referência da coroutine em execução, usada para poder pausar/parar o ciclo se necessário.
    private Coroutine cicloEmExecucao;

    // Índice da etapa atualmente visível.
    private int indiceAtual = -1;

    private void OnEnable()
    {
        // Antes de começar, garante que todos os destaques e textos comecem escondidos,
        // evitando que várias etapas apareçam sobrepostas ao mesmo tempo.
        EsconderTodasAsEtapas();

        if (iniciarAutomaticamente)
        {
            IniciarCiclo();
        }
    }

    private void OnDisable()
    {
        // Interrompe a coroutine ao desativar o objeto, para não deixá-la rodando em segundo plano.
        PararCiclo();
    }
    public void IniciarCiclo()
    {
        // Evita iniciar duas coroutines ao mesmo tempo caso o método seja chamado mais de uma vez.
        PararCiclo();

        if (etapas == null || etapas.Length == 0)
        {
            Debug.LogWarning("CicloVerticesArestasFaces: nenhuma etapa configurada no Inspector.");
            return;
        }

        cicloEmExecucao = StartCoroutine(RodarCiclo());
    }
    public void PararCiclo()
    {
        if (cicloEmExecucao != null)
        {
            StopCoroutine(cicloEmExecucao);
            cicloEmExecucao = null;
        }
    }
    private IEnumerator RodarCiclo()
    {
        indiceAtual = -1;

        do
        {
            indiceAtual++;

            // Quando chega ao fim da lista, decide se repete ou encerra.
            if (indiceAtual >= etapas.Length)
            {
                if (repetirEmLoop)
                {
                    indiceAtual = 0;
                }
                else
                {
                    yield break;
                }
            }

            MostrarEtapa(indiceAtual);

            yield return new WaitForSeconds(tempoExibicao);

            EsconderEtapa(indiceAtual);

        } while (repetirEmLoop || indiceAtual < etapas.Length - 1);
    }
    private void MostrarEtapa(int indice)
    {
        Etapa etapa = etapas[indice];

        if (etapa.objetoBase != null)
            etapa.objetoBase.SetActive(true);

        if (etapa.objetoDestacado != null)
            etapa.objetoDestacado.SetActive(true);

        if (etapa.texto != null)
            etapa.texto.SetActive(true);
    }
    private void EsconderEtapa(int indice)
    {
        Etapa etapa = etapas[indice];

        if (etapa.objetoDestacado != null)
            etapa.objetoDestacado.SetActive(false);

        if (etapa.texto != null)
            etapa.texto.SetActive(false);
    }
    private void EsconderTodasAsEtapas()
    {
        if (etapas == null)
            return;

        foreach (var etapa in etapas)
        {
            if (etapa == null)
                continue;

            if (etapa.objetoDestacado != null)
                etapa.objetoDestacado.SetActive(false);

            if (etapa.texto != null)
                etapa.texto.SetActive(false);
        }
    }
}
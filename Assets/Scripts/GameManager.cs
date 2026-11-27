using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // o GameManager "oficial": qualquer script acha ele por aqui
    public static GameManager Instance;

    public LevelList levelList;
    public CanvasFade fade;   // filho do GameManager: sobrevive à troca de cena junto com ele

    private bool isLoading;   // evita começar duas trocas de fase ao mesmo tempo

    void Awake()
    {
        // já existe um GameManager vivo (veio de uma fase anterior): este aqui sobra
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameEvents.BallFinished += OnBallFinished;
    }

    void OnDestroy()
    {
        // só o GameManager oficial se desinscreve e libera o lugar
        // (a cópia que se destruiu no Awake nunca se inscreveu)
        if (Instance == this)
        {
            GameEvents.BallFinished -= OnBallFinished;
            Instance = null;
        }
    }

    private void OnBallFinished()
    {
        if (isLoading) return;

        StartCoroutine(LoadNextLevel());
    }

    private IEnumerator LoadNextLevel()
    {
        isLoading = true;

        // 1. escurece a tela e espera terminar
        yield return fade.FadeOut();

        // 2. com a tela preta, carrega a próxima fase e espera ela terminar de carregar
        string currentScene = SceneManager.GetActiveScene().name;
        string nextScene = levelList.GetNextLevel(currentScene);
        yield return SceneManager.LoadSceneAsync(nextScene);

        // 3. clareia a tela, já na fase nova
        yield return fade.FadeIn();

        isLoading = false;
    }
}

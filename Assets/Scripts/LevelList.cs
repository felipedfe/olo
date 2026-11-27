using UnityEngine;

// A ordem das fases do jogo. É um asset: fica na pasta do projeto, não numa cena
[CreateAssetMenu(fileName = "LevelList", menuName = "Olo/Level List")]
public class LevelList : ScriptableObject
{
    // nomes das cenas na ordem em que o jogador joga
    public string[] levelScenes;

    // Devolve o nome da fase que vem depois de "currentScene".
    public string GetNextLevel(string currentScene)
    {
        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (levelScenes[i] == currentScene)
            {
                int nextIndex = i + 1;

                // depois da última fase, volta para a primeira (até existir uma tela de fim de jogo)
                if (nextIndex >= levelScenes.Length)
                {
                    nextIndex = 0;
                }

                return levelScenes[nextIndex];
            }
        }

        // a cena atual não está na lista (ex.: uma cena de teste): começa pela primeira
        return levelScenes[0];
    }
}

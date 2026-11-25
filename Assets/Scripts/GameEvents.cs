using UnityEngine;

public static class GameEvents
{
    // a lista de quem quer ser avisado (começa vazia = null)
    public static event System.Action BallReset;

    // a porta oficial para avisar todos da lista
    public static void AnnounceBallReset()
    {
        if (BallReset != null)   // tem alguém na lista?
        {
            BallReset();          // avisa todos
        }
    }
}

// Quem quer ouvir:    GameEvents.BallReset += MeuMetodo;   (entra na lista)
// Quem para de ouvir: GameEvents.BallReset -= MeuMetodo;   (sai da lista)
// Quem anuncia:       GameEvents.AnnounceBallReset();
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class PacmanAI : MonoBehaviour
{

    // Structure FSM
    private FSM Brain;

    // Vitesse de déplacement
    public float Speed;

    // Direction vers laquelle se dirige le personnage
    private Vector3 Direction;

    // Tiles
    private TileManager.Tile CurrentTile;
    private TileManager.Tile NextTile;
    Queue<TileManager.Tile> Waypoints;

    // Variables utilitaires
    private List<TileManager.Tile> Tiles = new List<TileManager.Tile>();
    private TileManager Manager;
    private bool _deadPlaying = false;
    private GameManager GM;
    private ScoreManager SM;
    private GameGUINavigation GUINav;

    // Permet d'afficher le debug des corridors empruntés
    private List<TileManager.Tile> debug_corridor;

    /// <summary>
    /// Appelé à la mort de pacman
    /// </summary>
    public void ResetVariables()
    {
        // Réinitialisation de toutes les variables de déplacement
        NextTile = null;
        Waypoints.Clear();
    }

    void Awake()
    {
        // Initialisation des variables
        Manager = GameObject.Find("Game Manager").GetComponent<TileManager>();
        GM = GameObject.Find("Game Manager").GetComponent<GameManager>();
        SM = GameObject.Find("Game Manager").GetComponent<ScoreManager>();
        GUINav = GameObject.Find("UI Manager").GetComponent<GameGUINavigation>();

        Waypoints = new Queue<TileManager.Tile>();
        Tiles = Manager.tiles;

    }

    void Start()
    {
        // Démarrage de la FSM
        Brain = new FSM(AI_InitialState);
    }

    #region Fonctions d'affichage
    void Animate()
    {
        GetComponent<Animator>().SetFloat("DirX", Direction.x);
        GetComponent<Animator>().SetFloat("DirY", Direction.y);
    }

    IEnumerator PlayDeadAnimation()
    {
        _deadPlaying = true;
        GetComponent<Animator>().SetBool("Die", true);
        yield return new WaitForSeconds(1);
        GetComponent<Animator>().SetBool("Die", false);
        _deadPlaying = false;

        if (GameManager.lives <= 0)
        {
            Debug.Log("Treshold for High Score: " + SM.LowestHigh());
            if (GameManager.score >= SM.LowestHigh())
                GUINav.getScoresMenu();
            else
                GUINav.H_ShowGameOverScreen();
        }

        else
            GM.ResetScene();
    }
    #endregion

    void Update()
    {
        // Debug affichage du chemin
        if (Waypoints.Count != 0)
        {
            List<TileManager.Tile> path = new List<TileManager.Tile>(Waypoints);
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 first = new Vector3(path[i].x, path[i].y);
                Vector3 secnd = new Vector3(path[i + 1].x, path[i + 1].y);

                Debug.DrawLine(first, secnd);
            }
        }
    }

    void FixedUpdate()
    {
        // Suivant l'état du jeu on appel l'IA ou l'animation de mort
        switch (GameManager.gameState)
        {
            case GameManager.GameState.Game:
                // Appel de l'IA
                Brain.RunFSM();
                Animate();
                break;

            case GameManager.GameState.Dead:
                if (!_deadPlaying)
                    StartCoroutine("PlayDeadAnimation");
                break;
        }

        // Mise à jours de la tile actuelle par rapport à la position du pacman
        Vector3 CurrentPos = new Vector3(transform.position.x + 0.499f, transform.position.y + 0.499f);
        CurrentTile = Tiles[Manager.Index((int)CurrentPos.x, (int)CurrentPos.y)];
    }

    void AI_InitialState()
    {
        if (Random.Range(0f, 1f) > 0.5)
            NextTile = CurrentTile.right;
        else
            NextTile = CurrentTile.left;

        Brain.SetState(AI_MovingToNextTile);
    }

    void AI_MovingToNextTile()
    {
        if (NextTile == null)
        {
            Brain.SetState(AI_DecideDirection);
            return;
        }

        // Si on atteint le tile destination
        if (Vector3.Distance(transform.position, new Vector3(NextTile.x, NextTile.y)) <= 0.000000000001)
        {
            // Si on a une liste de waypoints, continuer
            if (Waypoints.Count > 0)
            {
                NextTile = Waypoints.Dequeue();

                // A changer
                List<TileManager.Tile> wp = new List<TileManager.Tile>(Waypoints);
                // On vérifie si un ghost ne s'est pas mis sur notre chemin
                foreach(TileManager.Tile t in wp)
                {
                    // Si c'est le cas on annule tout et on cherche un autre chemin
                    if (t.isDangerous)
                    {
                        Brain.SetState(AI_DecideDirection);
                        Waypoints.Clear();
                        NextTile = null;
                        break;
                    }
                }
            }

            // Sinon reset le next
            else
            {
                NextTile = null;
                // Si on arrive à une intersection, choisir une direction
                if (CurrentTile.isIntersection)
                    Brain.SetState(AI_DecideDirection);
            }
        }
        else
        {
            // On n'a pas encore atteint le prochain tile, on se dirige vers celui-ci
            MoveTowardsTile(NextTile);
        }
    }

    void AI_DecideDirection()
    {
        // Si le prochain tile est déjà défini, s'y déplacer
        if (NextTile != null)
        {
            Brain.SetState(AI_MovingToNextTile);
            return;
        }

        Dictionary<TileManager.TILEDIRECTION, int> nextChoice = new Dictionary<TileManager.TILEDIRECTION, int>();

        // Calcul des différentes valeurs de chemin
        if (CurrentTile.up != null)
            nextChoice[TileManager.TILEDIRECTION.UP] = EvaluateCorridor(TileManager.GetCorridor(CurrentTile.up, TileManager.TILEDIRECTION.UP));

        if (CurrentTile.right != null)
            nextChoice[TileManager.TILEDIRECTION.RIGHT] = EvaluateCorridor(TileManager.GetCorridor(CurrentTile.right, TileManager.TILEDIRECTION.RIGHT));

        if (CurrentTile.down != null)
            nextChoice[TileManager.TILEDIRECTION.DOWN] = EvaluateCorridor(TileManager.GetCorridor(CurrentTile.down, TileManager.TILEDIRECTION.DOWN));

        if (CurrentTile.left != null)
            nextChoice[TileManager.TILEDIRECTION.LEFT] = EvaluateCorridor(TileManager.GetCorridor(CurrentTile.left, TileManager.TILEDIRECTION.LEFT));

        // On tri par ordre décroissant
        IOrderedEnumerable<KeyValuePair<TileManager.TILEDIRECTION, int>> ordered = from pair in nextChoice orderby pair.Value descending select pair;

        // Quelle direction a le plus de value
        switch(ordered.ElementAt(0).Key)
        {
            case TileManager.TILEDIRECTION.UP:
                NextTile = CurrentTile.up;
                break;
            case TileManager.TILEDIRECTION.RIGHT:
                NextTile = CurrentTile.right;
                break;
            case TileManager.TILEDIRECTION.DOWN:
                NextTile = CurrentTile.down;
                break;
            case TileManager.TILEDIRECTION.LEFT:
                NextTile = CurrentTile.left;
                break;
        }

        debug_corridor = TileManager.GetCorridor(NextTile, ordered.First().Key);
        Waypoints = new Queue<TileManager.Tile>(TileManager.GetCorridor(NextTile, ordered.First().Key));
    }

    /// <summary>
    /// Déplace pacman en direction d'une tile
    /// </summary>
    /// <param name="t">La tile cible</param>
    void MoveTowardsTile(TileManager.Tile t)
    {
        Vector2 p = Vector2.MoveTowards(transform.position, new Vector3(t.x, t.y), Speed);

        Direction = new Vector3(t.x, t.y) - transform.position;
        GetComponent<Rigidbody2D>().MovePosition(p);
    }

    /// <summary>
    /// Détermine la valeur d'un couloir
    /// </summary>
    /// <param name="corridor">Liste de tiles représentant le couloir</param>
    /// <returns>La valeur du couloir</returns>
    int EvaluateCorridor(List<TileManager.Tile> corridor)
    {
        int value = 0;
        foreach(TileManager.Tile t in corridor)
        {
            if (t.hasPacdot) value += 1;
            if (t.isDangerous) value -= 10;
        }

        return value;
    }

    public void OnDrawGizmos()
    {
        if (debug_corridor != null)
        {
            foreach (TileManager.Tile t in debug_corridor)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(new Vector3(t.x, t.y), new Vector3(1f, 1f));
            }
        }

        foreach (TileManager.Tile t in Tiles)
        {
            if (t.isDangerous)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(new Vector3(t.x, t.y), new Vector3(1f, 1f));
            }
        }
    }
}

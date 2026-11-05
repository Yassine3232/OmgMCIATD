using UnityEngine;
using UnityEngine.SceneManagement;

// Les différents états possibles de la partie.
public enum EtatJeu { EnCours, Pause, Victoire, Defaite }

// =====================================================================
// GameManager : le "chef d'orchestre" du jeu.
// =====================================================================
// RÔLE : centraliser les règles (vies, score, victoire, défaite).
//
// SCHÉMA DE COMMUNICATION :
//   Collectible / GuardAI / ZoneChute / Checkpoint / ZoneSortie
//        |  (Triggers : ils PRÉVIENNENT le GameManager)
//        v
//   GameManager --> UIManager     (met à jour le HUD et les menus)
//               --> AudioManager  (joue les sons)
//               --> Effets de particules (prefabs instanciés)
//               --> PlayerController (réapparition)
//
// Les autres scripts n'ont donc jamais à modifier les vies ou le score
// eux-mêmes : ils appellent seulement une méthode publique d'ici.
//
// MISE EN PLACE : un seul GameManager dans la scène (objet vide).
// =====================================================================
public class GameManager : MonoBehaviour
{
    // Singleton : accessible partout avec GameManager.Instance.
    public static GameManager Instance { get; private set; }

    [Header("Règles du jeu")]
    [SerializeField] private int viesMax = 3;

    // Nourriture à collecter pour pouvoir gagner.
    // 0 = toute la nourriture présente dans la scène.
    [SerializeField] private int nourritureRequise = 0;

    // Après un dégât, le joueur est protégé quelques secondes.
    [SerializeField] private float dureeInvulnerabilite = 1.5f;

    [Header("Références de la scène")]
    [SerializeField] private PlayerController joueur;
    [SerializeField] private CameraFollow cameraSuiveuse;
    [SerializeField] private UIManager ui;
    [SerializeField] private AudioManager son;

    // Porte de sortie ouverte automatiquement quand assez de nourriture
    // est collectée. Laisser vide si la porte est ouverte par un levier.
    [SerializeField] private DoorController porteSortie;

    [Header("Effets de particules (prefabs, optionnels)")]
    [SerializeField] private GameObject effetCollecte;
    [SerializeField] private GameObject effetDegat;
    [SerializeField] private GameObject effetCheckpoint;
    [SerializeField] private GameObject effetVictoire;

    // --- État du jeu (lecture publique, modification interne) ---
    public EtatJeu Etat { get; private set; } = EtatJeu.EnCours;
    public bool JeuEnCours => Etat == EtatJeu.EnCours;
    public int Vies { get; private set; }
    public int Score { get; private set; }
    public int NourritureCollectee { get; private set; }
    public int NourritureTotale { get; private set; }

    // Dernier point de contrôle atteint (position de réapparition).
    private Vector3 positionReapparition;
    private float finInvulnerabilite;

    private void Awake()
    {
        // Singleton : s'il existe déjà un GameManager, on supprime celui-ci.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Après un redémarrage, le temps pourrait être resté à 0.
        Time.timeScale = 1f;

        // Recherche automatique des références oubliées dans l'Inspector.
        if (joueur == null) joueur = FindFirstObjectByType<PlayerController>();
        if (cameraSuiveuse == null) cameraSuiveuse = FindFirstObjectByType<CameraFollow>();
        if (ui == null) ui = FindFirstObjectByType<UIManager>();
        if (son == null) son = FindFirstObjectByType<AudioManager>();
    }

    private void Start()
    {
        Vies = viesMax;
        Score = 0;
        NourritureCollectee = 0;

        // Compte toute la nourriture présente au début de la partie.
        NourritureTotale = FindObjectsByType<Collectible>(
            FindObjectsSortMode.None).Length;

        if (nourritureRequise <= 0 || nourritureRequise > NourritureTotale)
        {
            nourritureRequise = NourritureTotale;
        }

        // Le premier point de réapparition est la position de départ.
        if (joueur != null) positionReapparition = joueur.transform.position;

        RafraichirInterface();
    }

    private void Update()
    {
        // Échap : pause / reprise (seulement si la partie n'est pas finie).
        if (Input.GetKeyDown(KeyCode.Escape)
            && (Etat == EtatJeu.EnCours || Etat == EtatJeu.Pause))
        {
            BasculerPause();
        }
    }

    // ------------------------------------------------------------------
    // ÉVÉNEMENTS ENVOYÉS PAR LES OBJETS DU JEU
    // ------------------------------------------------------------------

    // Appelée par Collectible quand le joueur ramasse de la nourriture.
    public void CollecterNourriture(int points, Vector3 position)
    {
        if (!JeuEnCours) return;

        NourritureCollectee++;
        Score += points;

        JouerEffet(effetCollecte, position);
        if (son != null) son.JouerCollecte();
        RafraichirInterface();

        // Assez de nourriture : on ouvre la porte de sortie (si assignée).
        if (NourritureCollectee >= nourritureRequise)
        {
            if (porteSortie != null) porteSortie.Ouvrir();
            if (ui != null) ui.AfficherMessage("Objectif atteint ! Rejoignez la sortie.");
        }
    }

    // Appelée par Checkpoint : mémorise le nouveau point de réapparition.
    public void ActiverCheckpoint(Vector3 position)
    {
        if (!JeuEnCours) return;

        positionReapparition = position;
        JouerEffet(effetCheckpoint, position);
        if (son != null) son.JouerCheckpoint();
        if (ui != null) ui.AfficherMessage("Point de contrôle !");
    }

    // Appelée par ZoneChute : le joueur est tombé dans le vide.
    public void JoueurTombe()
    {
        // Une chute doit toujours replacer le joueur, même invulnérable.
        InfligerDegat(false);
    }

    // Appelée par GuardAI : un garde a attrapé le joueur.
    public void JoueurAttrape()
    {
        // Ici, l'invulnérabilité s'applique (évite de perdre toutes ses vies d'un coup).
        InfligerDegat(true);
    }

    // Appelée par ZoneSortie : le joueur atteint la sortie.
    public void AtteindreSortie()
    {
        if (!JeuEnCours) return;

        if (NourritureCollectee < nourritureRequise)
        {
            int manque = nourritureRequise - NourritureCollectee;
            if (ui != null) ui.AfficherMessage($"Il manque {manque} nourriture(s) !");
            return;
        }

        Etat = EtatJeu.Victoire;
        JouerEffet(effetVictoire, joueur != null ? joueur.transform.position : Vector3.zero);
        if (son != null) son.JouerVictoire();
        if (ui != null) ui.AfficherVictoire(Score);
        Time.timeScale = 0f; // fige le jeu
    }

    // ------------------------------------------------------------------
    // LOGIQUE INTERNE
    // ------------------------------------------------------------------

    private void InfligerDegat(bool respecterInvulnerabilite)
    {
        if (!JeuEnCours) return;
        if (respecterInvulnerabilite && Time.time < finInvulnerabilite) return;

        Vies--;
        finInvulnerabilite = Time.time + dureeInvulnerabilite;

        if (joueur != null) JouerEffet(effetDegat, joueur.transform.position);
        if (son != null) son.JouerDegat();
        RafraichirInterface();

        if (Vies <= 0)
        {
            Defaite();
        }
        else
        {
            Reapparaitre();
        }
    }

    private void Reapparaitre()
    {
        if (joueur != null) joueur.Reapparaitre(positionReapparition);
        if (cameraSuiveuse != null) cameraSuiveuse.TeleporterSurCible();
    }

    private void Defaite()
    {
        Etat = EtatJeu.Defaite;
        if (son != null) son.JouerDefaite();
        if (ui != null) ui.AfficherDefaite(Score);
        Time.timeScale = 0f;
    }

    private void RafraichirInterface()
    {
        if (ui == null) return;
        ui.MettreAJourVies(Vies, viesMax);
        ui.MettreAJourScore(Score);
        ui.MettreAJourNourriture(NourritureCollectee, nourritureRequise);
    }

    // Crée un effet de particules à une position, puis le supprime.
    private void JouerEffet(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        GameObject effet = Instantiate(prefab, position, Quaternion.identity);
        Destroy(effet, 3f);
    }

    // ------------------------------------------------------------------
    // MENUS (méthodes à relier aux boutons dans l'Inspector : OnClick)
    // ------------------------------------------------------------------

    public void BasculerPause()
    {
        if (Etat == EtatJeu.EnCours)
        {
            Etat = EtatJeu.Pause;
            Time.timeScale = 0f;
            if (ui != null) ui.AfficherPause(true);
        }
        else if (Etat == EtatJeu.Pause)
        {
            Reprendre();
        }
    }

    public void Reprendre()
    {
        Etat = EtatJeu.EnCours;
        Time.timeScale = 1f;
        if (ui != null) ui.AfficherPause(false);
    }

    // Recharge la scène actuelle : tout est remis à zéro.
    public void Redemarrer()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quitter()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // quitte aussi le mode Play
#endif
    }
}
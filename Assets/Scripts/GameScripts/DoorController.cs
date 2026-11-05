using UnityEngine;

// =====================================================================
// DoorController : porte qui coulisse (par défaut vers le haut).
// =====================================================================
// QUI L'OUVRE ?
// - GameManager : quand assez de nourriture est collectée.
// - Lever : quand le joueur actionne un levier.
//
// AMÉLIORATIONS PAR RAPPORT À LA VERSION DE BASE :
// - Les positions sont calculées dans Awake() : plus de problème si
//   Ouvrir() est appelée avant Start().
// - Ouvrir(), Fermer() et Basculer() sont disponibles.
// - Un son est joué à chaque changement d'état.
// =====================================================================
public class DoorController : MonoBehaviour
{
    // Partie mobile de la porte (le battant). Si vide, on utilise cet objet.
    [SerializeField] private Transform battant;

    // Déplacement appliqué pour ouvrir (ici : 3 unités vers le haut).
    [SerializeField] private Vector3 decalageOuverture = new Vector3(0f, 3f, 0f);

    // Vitesse de déplacement en unités/seconde.
    [SerializeField] private float vitesse = 2f;

    private Vector3 positionFermee;
    private Vector3 positionOuverte;

    // Lecture publique : la porte est-elle (demandée) ouverte ?
    public bool EstOuverte { get; private set; }

    private void Awake()
    {
        if (battant == null) battant = transform;

        // Positions locales : elles suivent le parent s'il bouge.
        positionFermee = battant.localPosition;
        positionOuverte = positionFermee + decalageOuverture;
    }

    private void Update()
    {
        Vector3 cible = EstOuverte ? positionOuverte : positionFermee;

        // MoveTowards : avance à vitesse constante et s'arrête sur la cible.
        battant.localPosition = Vector3.MoveTowards(
            battant.localPosition, cible, vitesse * Time.deltaTime);
    }

    public void Ouvrir()
    {
        if (EstOuverte) return; // déjà ouverte : rien à faire

        EstOuverte = true;
        if (AudioManager.Instance != null) AudioManager.Instance.JouerPorte();
    }

    public void Fermer()
    {
        if (!EstOuverte) return;

        EstOuverte = false;
        if (AudioManager.Instance != null) AudioManager.Instance.JouerPorte();
    }

    // Ouvre si fermée, ferme si ouverte (utile pour un levier réutilisable).
    public void Basculer()
    {
        if (EstOuverte) Fermer();
        else Ouvrir();
    }
}
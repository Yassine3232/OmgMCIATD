using UnityEngine;
using UnityEngine.AI;

// =====================================================================
// GuardAI : garde qui patrouille, poursuit le joueur, puis revient.
// =====================================================================
// MACHINE À ÉTATS :
//
//   [Patrouille] --(voit le joueur)--> [Poursuite]
//        ^                                  |
//        |                          (perd le joueur)
//        |                                  v
//        +------(arrivé au point)------ [Retour]
//
// Si le joueur est vu pendant le Retour, le garde repart en Poursuite.
//
// MISE EN PLACE :
// 1. Le sol doit être "baké" : Window > AI > Navigation > Bake
//    (ou composant NavMeshSurface selon la version de Unity).
// 2. Le garde a un NavMeshAgent (ajouté auto.).
// 3. Ajouter un Collider "Is Trigger" sur le garde (zone de capture).
// 4. Glisser les points de patrouille (objets vides) dans la liste.
// =====================================================================
[RequireComponent(typeof(NavMeshAgent))]
public class GuardAI : MonoBehaviour
{
    private enum Etat { Patrouille, Poursuite, Retour }

    [Header("Patrouille")]
    // Points parcourus en boucle par le garde.
    [SerializeField] private Transform[] pointsPatrouille;
    [SerializeField] private float vitessePatrouille = 2f;

    // Temps d'attente à chaque point (en secondes).
    [SerializeField] private float tempsAttente = 1f;

    [Header("Poursuite")]
    [SerializeField] private float vitessePoursuite = 4.5f;

    // Temps pendant lequel le garde continue à poursuivre
    // après avoir perdu le joueur de vue.
    [SerializeField] private float tempsAvantAbandon = 3f;

    [Header("Détection")]
    // Distance maximale à laquelle le garde voit le joueur.
    [SerializeField] private float distanceVision = 8f;

    // Angle total du champ de vision (en degrés).
    [SerializeField] private float angleVision = 110f;

    // Hauteur des yeux du garde (pour le rayon de visibilité).
    [SerializeField] private float hauteurYeux = 1f;

    // Couches qui bloquent la vue (murs...). Par défaut : tout.
    [SerializeField] private LayerMask couchesObstacles = ~0;

    private NavMeshAgent agent;
    private Transform joueur;
    private Etat etat = Etat.Patrouille;

    private int indexPoint;
    private float minuteurAttente;
    private float dernierInstantVu;
    private Vector3 positionDepart;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        positionDepart = transform.position;
    }

    private void Start()
    {
        GameObject objetJoueur = GameObject.FindGameObjectWithTag("Player");
        if (objetJoueur != null) joueur = objetJoueur.transform;

        PasserEnPatrouille();
    }

    private void Update()
    {
        if (joueur == null) return;

        bool voitJoueur = PeutVoirJoueur();
        if (voitJoueur) dernierInstantVu = Time.time;

        switch (etat)
        {
            case Etat.Patrouille:
                MettreAJourPatrouille();
                if (voitJoueur) PasserEnPoursuite();
                break;

            case Etat.Poursuite:
                MettreAJourPoursuite();
                break;

            case Etat.Retour:
                MettreAJourRetour();
                if (voitJoueur) PasserEnPoursuite();
                break;
        }
    }

    // ------------------------ Changements d'état ------------------------

    private void PasserEnPatrouille()
    {
        etat = Etat.Patrouille;
        agent.speed = vitessePatrouille;
        minuteurAttente = 0f;
        AllerAuPointActuel();
    }

    private void PasserEnPoursuite()
    {
        etat = Etat.Poursuite;
        agent.speed = vitessePoursuite;
        agent.isStopped = false;
    }

    private void PasserEnRetour()
    {
        etat = Etat.Retour;
        agent.speed = vitessePatrouille;
        AllerAuPointActuel();
    }

    // ------------------------ Comportements ------------------------

    private void MettreAJourPatrouille()
    {
        // Sans points de patrouille, le garde reste en place.
        if (pointsPatrouille == null || pointsPatrouille.Length == 0) return;

        if (!ArriveAuPoint()) return;

        // Arrivé : on attend, puis on passe au point suivant.
        minuteurAttente += Time.deltaTime;
        if (minuteurAttente >= tempsAttente)
        {
            minuteurAttente = 0f;
            indexPoint = (indexPoint + 1) % pointsPatrouille.Length;
            AllerAuPointActuel();
        }
    }

    private void MettreAJourPoursuite()
    {
        // Le garde fonce vers la position actuelle du joueur.
        agent.SetDestination(joueur.position);

        // Si le joueur est hors de vue depuis trop longtemps : abandon.
        if (Time.time - dernierInstantVu > tempsAvantAbandon)
        {
            PasserEnRetour();
        }
    }

    private void MettreAJourRetour()
    {
        // Une fois revenu sur son parcours, il reprend la patrouille.
        if (ArriveAuPoint()) PasserEnPatrouille();
    }

    // ------------------------ Outils ------------------------

    // Envoie le garde vers le point de patrouille courant
    // (ou vers sa position de départ s'il n'a aucun point).
    private void AllerAuPointActuel()
    {
        agent.isStopped = false;

        if (pointsPatrouille != null && pointsPatrouille.Length > 0)
        {
            agent.SetDestination(pointsPatrouille[indexPoint].position);
        }
        else
        {
            agent.SetDestination(positionDepart);
        }
    }

    // Vrai si le garde a atteint sa destination.
    private bool ArriveAuPoint()
    {
        return !agent.pathPending
            && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    // Le garde voit le joueur si :
    // 1. il est assez proche,
    // 2. il est dans le cône de vision,
    // 3. aucun mur ne bloque la vue (Raycast).
    private bool PeutVoirJoueur()
    {
        Vector3 yeux = transform.position + Vector3.up * hauteurYeux;
        Vector3 cibleVisee = joueur.position + Vector3.up * hauteurYeux;
        Vector3 versJoueur = cibleVisee - yeux;
        float distance = versJoueur.magnitude;

        if (distance > distanceVision) return false;

        // Angle entre l'avant du garde et la direction du joueur.
        // On compare à la MOITIÉ du champ de vision total.
        if (Vector3.Angle(transform.forward, versJoueur) > angleVision * 0.5f)
            return false;

        // Rayon vers le joueur : le premier objet touché doit être le joueur.
        if (Physics.Raycast(yeux, versJoueur.normalized, out RaycastHit impact,
                distance, couchesObstacles, QueryTriggerInteraction.Ignore))
        {
            return impact.collider.CompareTag("Player")
                || impact.collider.transform.IsChildOf(joueur);
        }

        return false;
    }

    // Trigger de capture : le garde touche le joueur -> on prévient le GameManager.
    private void OnTriggerEnter(Collider autre)
    {
        if (!autre.CompareTag("Player")) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.JoueurAttrape();
        }
    }

    // Dessine la zone de vision dans la vue Scene (aide au réglage).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanceVision);

        Vector3 gauche = Quaternion.Euler(0f, -angleVision * 0.5f, 0f) * transform.forward;
        Vector3 droite = Quaternion.Euler(0f, angleVision * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + gauche * distanceVision);
        Gizmos.DrawLine(transform.position, transform.position + droite * distanceVision);
    }
}
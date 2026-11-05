using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// MovingPlatform : plateforme qui fait des allers-retours et
// transporte le joueur posé dessus.
// =====================================================================
// FONCTIONNEMENT :
// 1. La plateforme part de sa position de départ (A).
// 2. Elle se déplace vers A + deplacement (B), attend, puis revient.
// 3. Chaque "passager" (joueur) posé dessus est déplacé du même
//    montant, sinon la plateforme glisserait sous ses pieds.
//
// MISE EN PLACE :
// - Un Collider NON trigger (le joueur doit pouvoir marcher dessus).
// - Le Rigidbody est ajouté automatiquement et réglé en "Kinematic".
// - Le joueur doit avoir le tag "Player".
// =====================================================================
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Mouvement")]
    // Déplacement entre le point A (départ) et le point B.
    // Exemple : (6, 0, 0) = la plateforme va 6 unités vers la droite.
    [SerializeField] private Vector3 deplacement = new Vector3(6f, 0f, 0f);

    // Vitesse en unités par seconde.
    [SerializeField] private float vitesse = 2f;

    // Pause aux extrémités (en secondes).
    [SerializeField] private float tempsPause = 1f;

    private Rigidbody corps;
    private Collider collisionneur;
    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 destination;
    private float minuteurPause;

    // Rigidbody des objets actuellement posés sur la plateforme.
    private readonly List<Rigidbody> passagers = new List<Rigidbody>();

    private void Awake()
    {
        corps = GetComponent<Rigidbody>();
        collisionneur = GetComponent<Collider>();

        // Kinematic : la plateforme est déplacée par le script,
        // pas par la gravité ni par les chocs.
        corps.isKinematic = true;
        corps.interpolation = RigidbodyInterpolation.Interpolate;

        pointA = transform.position;
        pointB = pointA + deplacement;
        destination = pointB;
    }

    private void FixedUpdate()
    {
        // Phase d'attente à une extrémité.
        if (minuteurPause > 0f)
        {
            minuteurPause -= Time.fixedDeltaTime;
            return;
        }

        // Calcul du prochain pas de déplacement.
        Vector3 nouvellePosition = Vector3.MoveTowards(
            corps.position, destination, vitesse * Time.fixedDeltaTime);
        Vector3 deltaMouvement = nouvellePosition - corps.position;

        corps.MovePosition(nouvellePosition);

        // On déplace les passagers du même montant que la plateforme.
        foreach (Rigidbody passager in passagers)
        {
            if (passager != null) passager.position += deltaMouvement;
        }

        // Extrémité atteinte : on inverse la destination et on attend.
        if (Vector3.Distance(nouvellePosition, destination) < 0.01f)
        {
            destination = (destination == pointB) ? pointA : pointB;
            minuteurPause = tempsPause;
        }
    }

    // OnCollisionStay est appelée à chaque pas physique tant qu'un objet
    // touche la plateforme : on vérifie s'il est bien DESSUS (et pas à côté).
    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        // Les pieds du joueur sont-ils au niveau du dessus de la plateforme ?
        bool estDessus = collision.collider.bounds.min.y >= collisionneur.bounds.max.y - 0.2f;

        if (estDessus && !passagers.Contains(rb))
        {
            passagers.Add(rb);
        }
        else if (!estDessus)
        {
            passagers.Remove(rb);
        }
    }

    // Le joueur quitte la plateforme : il n'est plus transporté.
    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody != null)
        {
            passagers.Remove(collision.rigidbody);
        }
    }

    // Affiche le trajet dans la vue Scene (aide au réglage).
    private void OnDrawGizmosSelected()
    {
        Vector3 depart = Application.isPlaying ? pointA : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(depart, depart + deplacement);
        Gizmos.DrawWireSphere(depart + deplacement, 0.3f);
    }
}
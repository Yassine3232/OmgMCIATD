using System.Collections.Generic;
using UnityEngine;

// =====================================================================
// MovingPlatform : la plateforme va du point A au point B, et revient.
//
// MISE EN PLACE :
// 1. Créez 2 GameObjects vides (A et B), PAS en enfant de la plateforme.
// 2. Glissez-les dans les champs "Point A" et "Point B".
// 3. Un Collider NON trigger sur la plateforme, un joueur avec le tag
//    "Player" et un Rigidbody.
// =====================================================================
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float vitesse = 2f;

    private Rigidbody corps;
    private Vector3 positionActuelle;
    private Vector3 destination;
    private readonly List<Rigidbody> passagers = new List<Rigidbody>();

    private void Awake()
    {
        corps = GetComponent<Rigidbody>();
        corps.isKinematic = true;
        corps.interpolation = RigidbodyInterpolation.Interpolate;

        positionActuelle = pointA.position;
        destination = pointB.position;
        corps.position = positionActuelle;
    }

    private void FixedUpdate()
    {
        Vector3 nouvellePosition = Vector3.MoveTowards(
            positionActuelle, destination, vitesse * Time.fixedDeltaTime);
        Vector3 delta = nouvellePosition - positionActuelle;

        positionActuelle = nouvellePosition;
        corps.MovePosition(nouvellePosition);

        // Les passagers bougent du même montant.
        foreach (Rigidbody passager in passagers)
        {
            passager.MovePosition(passager.position + delta);
        }

        // Arrivé : on repart dans l'autre sens.
        if (positionActuelle == destination)
        {
            destination = (destination == pointA.position) ? pointB.position : pointA.position;
        }
    }

    // Le joueur est-il DESSUS ? (la normale du contact pointe vers le bas)
    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (collision.rigidbody == null) return;

        bool estDessus = collision.GetContact(0).normal.y < -0.5f;

        if (estDessus && !passagers.Contains(collision.rigidbody))
            passagers.Add(collision.rigidbody);
        else if (!estDessus)
            passagers.Remove(collision.rigidbody);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.rigidbody != null) passagers.Remove(collision.rigidbody);
    }
}
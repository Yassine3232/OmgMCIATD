using UnityEngine;

// =====================================================================
// Checkpoint : point de contrôle.
// =====================================================================
// RÔLE : quand le joueur le touche, le GameManager mémorise sa position
// comme nouveau point de réapparition (après une chute ou une capture).
//
// MISE EN PLACE : Collider "Is Trigger" ; joueur avec le tag "Player".
// (Optionnel) un objet enfant "pointReapparition" pour choisir
// précisément l'endroit où le joueur réapparaît.
// =====================================================================
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    // Endroit exact de réapparition. Si vide : la position du checkpoint.
    [SerializeField] private Transform pointReapparition;

    private bool active; // un checkpoint ne s'active qu'une fois

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider autre)
    {
        if (active || !autre.CompareTag("Player")) return;

        active = true;

        Vector3 position = pointReapparition != null
            ? pointReapparition.position
            : transform.position;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ActiverCheckpoint(position);
        }
    }
}
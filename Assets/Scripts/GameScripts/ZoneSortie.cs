using UnityEngine;

// =====================================================================
// ZoneSortie : zone de fin de niveau (derrière la porte de sortie).
// =====================================================================
// RÔLE : quand le joueur y entre, on prévient le GameManager.
// Le GameManager vérifie lui-même si assez de nourriture a été
// collectée, puis déclenche la victoire (ou affiche un message).
//
// MISE EN PLACE : Collider "Is Trigger" ; joueur avec le tag "Player".
// =====================================================================
[RequireComponent(typeof(Collider))]
public class ZoneSortie : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider autre)
    {
        if (!autre.CompareTag("Player")) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AtteindreSortie();
        }
    }
}
using UnityEngine;

// =====================================================================
// ZoneChute : grande zone invisible placée sous le niveau.
// =====================================================================
// RÔLE : si le joueur tombe dedans, on prévient le GameManager.
// C'est LUI qui retire une vie et replace le joueur au dernier
// checkpoint.
//
// MISE EN PLACE : Collider "Is Trigger" large, sous les plateformes.
// =====================================================================
[RequireComponent(typeof(Collider))]
public class ZoneChute : MonoBehaviour
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
            GameManager.Instance.JoueurTombe();
        }
    }
}
using UnityEngine;

// =====================================================================
// Lever : levier que le joueur actionne avec la touche E.
// =====================================================================
// FONCTIONNEMENT :
// 1. Le joueur entre dans la zone Trigger du levier.
// 2. Il appuie sur E.
// 3. Le levier bascule, joue un son et actionne les portes associées.
//
// MISE EN PLACE :
// - Un Collider "Is Trigger" plus grand que le levier (zone d'interaction).
// - Glisser une ou plusieurs portes dans la liste "portes".
// - (Optionnel) glisser la poignée à faire pivoter dans "poignee".
// =====================================================================
[RequireComponent(typeof(Collider))]
public class Lever : MonoBehaviour
{
    [Header("Portes contrôlées")]
    [SerializeField] private DoorController[] portes;

    [Header("Comportement")]
    // Vrai : le levier peut être utilisé plusieurs fois (ouvre/ferme).
    // Faux : une seule utilisation.
    [SerializeField] private bool reutilisable = false;

    [Header("Animation (optionnelle)")]
    // Partie qui pivote quand on actionne le levier.
    [SerializeField] private Transform poignee;

    // Angle de rotation (axe X local) quand le levier est activé.
    [SerializeField] private float angleActive = 60f;
    [SerializeField] private float vitesseRotation = 180f;

    private bool joueurProche;
    private bool active;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        // Interaction : joueur dans la zone + touche E pressée.
        if (joueurProche && Input.GetKeyDown(KeyCode.E)
            && GameManager.Instance != null && GameManager.Instance.JeuEnCours)
        {
            Actionner();
        }

        AnimerPoignee();
    }

    private void Actionner()
    {
        // Levier à usage unique déjà utilisé : on ne fait rien.
        if (active && !reutilisable) return;

        active = !active;

        foreach (DoorController porte in portes)
        {
            if (porte == null) continue;

            // Réutilisable : on bascule. Sinon : on ouvre seulement.
            if (reutilisable) porte.Basculer();
            else porte.Ouvrir();
        }

        if (AudioManager.Instance != null) AudioManager.Instance.JouerLevier();
    }

    // Fait pivoter la poignée vers l'angle voulu (si elle est assignée).
    private void AnimerPoignee()
    {
        if (poignee == null) return;

        Quaternion cible = Quaternion.Euler(active ? angleActive : -angleActive, 0f, 0f);
        poignee.localRotation = Quaternion.RotateTowards(
            poignee.localRotation, cible, vitesseRotation * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider autre)
    {
        if (autre.CompareTag("Player")) joueurProche = true;
    }

    private void OnTriggerExit(Collider autre)
    {
        if (autre.CompareTag("Player")) joueurProche = false;
    }
}
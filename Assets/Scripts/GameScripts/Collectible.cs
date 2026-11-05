using UnityEngine;

// =====================================================================
// Collectible : nourriture que le joueur peut ramasser.
// =====================================================================
// RÔLE :
// 1. Tourner et flotter pour attirer l'œil (animation décorative).
// 2. Détecter le joueur avec un Trigger.
// 3. Prévenir le GameManager, puis disparaître.
//
// MISE EN PLACE : un Collider avec "Is Trigger" coché (fait auto. par
// Reset() quand on ajoute le script). Le joueur doit avoir le tag "Player".
// =====================================================================
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    [Header("Valeur")]
    // Points gagnés par le joueur.
    [SerializeField] private int points = 10;

    [Header("Animation décorative")]
    // Rotation autour de l'axe Y en degrés/seconde.
    [SerializeField] private float vitesseRotation = 60f;

    // Hauteur maximale du flottement (en unités).
    [SerializeField] private float amplitudeFlottement = 0.15f;

    // Rapidité du flottement.
    [SerializeField] private float vitesseFlottement = 2f;

    private Vector3 positionInitiale;
    private bool collectee; // empêche de compter deux fois le même objet

    // Reset() est appelée par Unity quand on ajoute le script dans l'éditeur :
    // on en profite pour cocher "Is Trigger" automatiquement.
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        positionInitiale = transform.position;

        // Décale la phase du flottement pour que tous les objets
        // ne montent pas et ne descendent pas en même temps.
        vitesseFlottement += Random.Range(-0.2f, 0.2f);
    }

    private void Update()
    {
        // Rotation continue autour de l'axe Y du monde.
        transform.Rotate(Vector3.up, vitesseRotation * Time.deltaTime, Space.World);

        // Flottement vertical avec une fonction sinus (valeur entre -1 et 1).
        float decalageY = Mathf.Sin(Time.time * vitesseFlottement) * amplitudeFlottement;
        transform.position = positionInitiale + Vector3.up * decalageY;
    }

    private void OnTriggerEnter(Collider autre)
    {
        // Ignorer si déjà ramassée ou si ce n'est pas le joueur.
        if (collectee || !autre.CompareTag("Player")) return;

        collectee = true;

        // On prévient le GameManager : il gère score, son, particules, UI.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollecterNourriture(points, transform.position);
        }

        Destroy(gameObject);
    }
}
using System.Collections;
using TMPro;
using UnityEngine;

// =====================================================================
// UIManager : affichage du HUD et des menus.
// =====================================================================
// RÔLE : uniquement de l'AFFICHAGE. Ce script ne décide jamais de rien :
// c'est le GameManager qui l'appelle quand quelque chose change.
//
// MISE EN PLACE :
// - Créer un Canvas avec des textes TextMeshPro (TMP) pour le HUD.
// - Créer 3 panneaux (Pause, Victoire, Défaite), désactivés au départ.
// - Sur chaque bouton, dans OnClick, appeler une méthode du GameManager :
//     Reprendre() / Redemarrer() / Quitter()
// =====================================================================
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text texteVies;
    [SerializeField] private TMP_Text texteScore;
    [SerializeField] private TMP_Text texteNourriture;

    // Message temporaire au centre de l'écran ("Point de contrôle !"...).
    [SerializeField] private TMP_Text texteMessage;
    [SerializeField] private float dureeMessage = 2f;

    [Header("Menus")]
    [SerializeField] private GameObject panneauPause;
    [SerializeField] private GameObject panneauVictoire;
    [SerializeField] private GameObject panneauDefaite;

    [Header("Scores finaux (optionnel)")]
    [SerializeField] private TMP_Text texteScoreVictoire;
    [SerializeField] private TMP_Text texteScoreDefaite;

    private Coroutine messageEnCours;

    private void Start()
    {
        // Au démarrage, tous les menus sont cachés.
        if (panneauPause != null) panneauPause.SetActive(false);
        if (panneauVictoire != null) panneauVictoire.SetActive(false);
        if (panneauDefaite != null) panneauDefaite.SetActive(false);
        if (texteMessage != null) texteMessage.gameObject.SetActive(false);
    }

    // ---------------------------- HUD ----------------------------

    public void MettreAJourVies(int vies, int viesMax)
    {
        if (texteVies != null) texteVies.text = $"Vies : {vies}/{viesMax}";
    }

    public void MettreAJourScore(int score)
    {
        if (texteScore != null) texteScore.text = $"Score : {score}";
    }

    public void MettreAJourNourriture(int collectee, int requise)
    {
        if (texteNourriture != null)
            texteNourriture.text = $"Nourriture : {collectee}/{requise}";
    }

    // Affiche un message quelques secondes, puis le fait disparaître.
    public void AfficherMessage(string message)
    {
        if (texteMessage == null) return;

        // Si un message est déjà affiché, on l'interrompt.
        if (messageEnCours != null) StopCoroutine(messageEnCours);
        messageEnCours = StartCoroutine(AfficherMessageTemporaire(message));
    }

    private IEnumerator AfficherMessageTemporaire(string message)
    {
        texteMessage.text = message;
        texteMessage.gameObject.SetActive(true);

        // "Realtime" : fonctionne même si Time.timeScale = 0 (pause).
        yield return new WaitForSecondsRealtime(dureeMessage);

        texteMessage.gameObject.SetActive(false);
    }

    // ---------------------------- Menus ----------------------------

    public void AfficherPause(bool afficher)
    {
        if (panneauPause != null) panneauPause.SetActive(afficher);
    }

    public void AfficherVictoire(int scoreFinal)
    {
        if (panneauVictoire != null) panneauVictoire.SetActive(true);
        if (texteScoreVictoire != null) texteScoreVictoire.text = $"Score final : {scoreFinal}";
    }

    public void AfficherDefaite(int scoreFinal)
    {
        if (panneauDefaite != null) panneauDefaite.SetActive(true);
        if (texteScoreDefaite != null) texteScoreDefaite.text = $"Score final : {scoreFinal}";
    }
}
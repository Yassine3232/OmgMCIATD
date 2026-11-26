using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// =====================================================================
// UIManager : affichage du HUD et des menus.
// =====================================================================
// RÔLE : uniquement de l'AFFICHAGE. Ce script ne décide jamais de rien :
// c'est le GameManager ou PlayerController qui l'appelle quand quelque chose change.
// =====================================================================
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text texteVies;
    [SerializeField] private TMP_Text texteScore;
    [SerializeField] private TMP_Text texteNourriture;

    [Header("HUD - Coeurs")]
    [SerializeField] private Image[] coeurs;
    [SerializeField] private Sprite coeurRouge;
    [SerializeField] private Sprite coeurVide;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitialiserCoeurs();
    }

    private void Start()
    {
        // Au démarrage, tous les menus sont cachés.
        if (panneauPause != null) panneauPause.SetActive(false);
        if (panneauVictoire != null) panneauVictoire.SetActive(false);
        if (panneauDefaite != null) panneauDefaite.SetActive(false);
        if (texteMessage != null) texteMessage.gameObject.SetActive(false);

        // Initialiser l'affichage avec 3 coeurs pleins par défaut
        MettreAJourCoeurs(3);
    }

    public void InitialiserCoeurs()
    {
        if (coeurRouge == null)
        {
            coeurRouge = Resources.Load<Sprite>("UI/Red Heart");
        }
        if (coeurVide == null)
        {
            coeurVide = Resources.Load<Sprite>("UI/Empty Heart");
        }
#if UNITY_EDITOR
        if (coeurRouge == null)
        {
            coeurRouge = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Red Heart.jpg");
        }
        if (coeurVide == null)
        {
            coeurVide = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Empty Heart.jpg");
        }
#endif

        if (coeurs == null || coeurs.Length == 0)
        {
            TrouverCoeurs();
        }
    }

    public void TrouverCoeurs()
    {
        Transform conteneurCoeurs = transform.Find("Hearts");
        if (conteneurCoeurs == null)
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Equals("Hearts", System.StringComparison.OrdinalIgnoreCase))
                {
                    conteneurCoeurs = t;
                    break;
                }
            }
        }

        if (conteneurCoeurs != null)
        {
            List<Image> liste = new List<Image>();
            for (int i = 0; i < conteneurCoeurs.childCount; i++)
            {
                Image img = conteneurCoeurs.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    liste.Add(img);
                }
            }

            // Trier de gauche à droite selon la position X
            liste.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));
            coeurs = liste.ToArray();
        }
    }

    // ---------------------------- HUD ----------------------------

    public void MettreAJourVies(int vies, int viesMax)
    {
        if (texteVies != null) texteVies.text = $"Vies : {vies}/{viesMax}";
        MettreAJourCoeurs(vies);
    }

    public void MettreAJourCoeurs(int vies)
    {
        if (coeurs == null || coeurs.Length == 0)
        {
            TrouverCoeurs();
        }

#if UNITY_EDITOR
        if (coeurRouge == null || coeurVide == null)
        {
            InitialiserCoeurs();
        }
#endif

        if (coeurs != null)
        {
            for (int i = 0; i < coeurs.Length; i++)
            {
                if (coeurs[i] == null) continue;

                if (i < vies)
                {
                    if (coeurRouge != null)
                    {
                        coeurs[i].sprite = coeurRouge;
                    }
                }
                else
                {
                    if (coeurVide != null)
                    {
                        coeurs[i].sprite = coeurVide;
                    }
                }
            }
        }
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
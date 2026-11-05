using UnityEngine;

// =====================================================================
// AudioManager : musique de fond et effets sonores.
// =====================================================================
// RÔLE : jouer les sons. Les autres scripts appellent seulement
// AudioManager.Instance.JouerXxx() : ils n'ont pas besoin de connaître
// les AudioSource ni les fichiers audio.
//
// MISE EN PLACE : un objet vide "AudioManager" dans la scène, puis
// glisser les fichiers audio dans les champs de l'Inspector.
// Un champ vide = pas de son (aucune erreur).
// =====================================================================
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Musique")]
    [SerializeField] private AudioClip musiqueFond;
    [SerializeField, Range(0f, 1f)] private float volumeMusique = 0.4f;

    [Header("Effets sonores")]
    [SerializeField] private AudioClip sonSaut;
    [SerializeField] private AudioClip sonCollecte;
    [SerializeField] private AudioClip sonDegat;
    [SerializeField] private AudioClip sonCheckpoint;
    [SerializeField] private AudioClip sonLevier;
    [SerializeField] private AudioClip sonPorte;
    [SerializeField] private AudioClip sonVictoire;
    [SerializeField] private AudioClip sonDefaite;
    [SerializeField, Range(0f, 1f)] private float volumeEffets = 0.8f;

    private AudioSource sourceMusique;
    private AudioSource sourceEffets;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Deux AudioSource créés automatiquement :
        // une pour la musique (en boucle), une pour les effets.
        sourceMusique = gameObject.AddComponent<AudioSource>();
        sourceMusique.loop = true;
        sourceMusique.playOnAwake = false;
        sourceMusique.volume = volumeMusique;

        sourceEffets = gameObject.AddComponent<AudioSource>();
        sourceEffets.playOnAwake = false;
    }

    private void Start()
    {
        if (musiqueFond != null)
        {
            sourceMusique.clip = musiqueFond;
            sourceMusique.Play();
        }
    }

    // Jouer un son une fois, sans couper les autres sons en cours.
    private void Jouer(AudioClip clip)
    {
        if (clip == null) return;
        sourceEffets.PlayOneShot(clip, volumeEffets);
    }

    // --- Méthodes publiques, une par événement du jeu ---
    public void JouerSaut() { Jouer(sonSaut); }
    public void JouerCollecte() { Jouer(sonCollecte); }
    public void JouerDegat() { Jouer(sonDegat); }
    public void JouerCheckpoint() { Jouer(sonCheckpoint); }
    public void JouerLevier() { Jouer(sonLevier); }
    public void JouerPorte() { Jouer(sonPorte); }

    public void JouerVictoire()
    {
        sourceMusique.Stop(); // la musique s'arrête pour laisser place au jingle
        Jouer(sonVictoire);
    }

    public void JouerDefaite()
    {
        sourceMusique.Stop();
        Jouer(sonDefaite);
    }
}
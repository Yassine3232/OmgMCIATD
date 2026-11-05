using UnityEngine;

// =====================================================================
// PlayerController : contrôle du joueur (déplacement, saut, animation).
// =====================================================================
// FONCTIONNEMENT (3e personne, caméra à la souris) :
// - La souris dirige la caméra (voir CameraFollow).
// - Le joueur REGARDE dans la même direction que la caméra.
// - Les touches se déplacent par rapport à la caméra :
//     W / flèche haut   : avancer (là où regarde la caméra)
//     S / flèche bas    : reculer
//     A / flèche gauche : se déplacer sur la gauche
//     D / flèche droite : se déplacer sur la droite
//     Espace            : sauter
//
// QUI L'UTILISE ?
// - GameManager : appelle Reapparaitre() après une chute ou une capture.
// - CameraFollow : lit la vitesse du Rigidbody pour anticiper.
// - Animator : reçoit les paramètres "Speed" (float) et "IsGrounded" (bool).
//
// MISE EN PLACE DANS UNITY :
// - Tag du GameObject : "Player".
// - Un Collider (Capsule conseillée) + un Rigidbody (ajouté auto).
// - Le Collider devrait avoir un Physic Material sans friction,
//   pour que le joueur ne "colle" pas aux murs.
// - La caméra est trouvée automatiquement (Camera.main : tag MainCamera).
// =====================================================================
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Déplacement")]
    // Vitesse de marche en unités/seconde.
    [SerializeField] private float vitesse = 5f;

    [Header("Orientation")]
    // Coché : le joueur regarde toujours dans la direction de la caméra.
    // Décoché : il se tourne seulement vers la direction où il marche.
    [SerializeField] private bool orienterSurCamera = true;

    // Rapidité avec laquelle le joueur se tourne vers la direction voulue.
    [SerializeField] private float vitesseRotation = 15f;

    // Caméra de référence (Camera.main si laissé vide).
    [SerializeField] private Transform cameraTransform;

    [Header("Saut")]
    // Hauteur du saut en unités (plus intuitif qu'une "force").
    [SerializeField] private float hauteurSaut = 1.5f;

    // Gravité supplémentaire à la descente : rend le saut moins "flottant".
    // 1 = gravité normale, 2 = chute deux fois plus rapide.
    [SerializeField] private float multiplicateurChute = 2f;

    // "Coyote time" : on peut encore sauter un court instant
    // après avoir quitté le bord d'une plateforme.
    [SerializeField] private float tempsCoyote = 0.12f;

    // Tampon de saut : si le joueur appuie un peu AVANT d'atterrir,
    // le saut est quand même déclenché à l'atterrissage.
    [SerializeField] private float tempsTamponSaut = 0.12f;

    [Header("Détection du sol")]
    // Rayon de la sphère placée sous les pieds pour détecter le sol.
    [SerializeField] private float rayonDetectionSol = 0.25f;

    // Couches considérées comme du sol (par défaut : toutes).
    [SerializeField] private LayerMask coucheSol = ~0;

    [Header("Animation")]
    // Animator du modèle du joueur (cherché automatiquement si vide).
    [SerializeField] private Animator animator;

    // Les noms des paramètres Animator sont convertis en nombres (hash) :
    // c'est plus rapide que d'utiliser des chaînes à chaque image.
    private static readonly int ParamVitesse = Animator.StringToHash("Speed");
    private static readonly int ParamAuSol = Animator.StringToHash("IsGrounded");

    private Rigidbody corps;
    private Collider collisionneur;

    // Direction de déplacement voulue, dans le monde (longueur max = 1).
    private Vector3 direction;

    // Direction (à plat) vers laquelle le joueur doit regarder.
    private Vector3 directionRegard;

    // Mémoire des instants importants pour le coyote time et le tampon.
    private float dernierAppuiSaut = -10f;
    private float dernierInstantAuSol = -10f;

    // Tableau réutilisé pour la détection du sol (évite de créer des déchets).
    private readonly Collider[] resultatsSol = new Collider[8];

    // Lecture publique : "le joueur est-il au sol ?"
    public bool EstAuSol { get; private set; }

    private void Awake()
    {
        corps = GetComponent<Rigidbody>();
        collisionneur = GetComponentInChildren<Collider>();

        // Empêche le joueur de basculer (on ne le tourne que sur Y).
        corps.constraints = RigidbodyConstraints.FreezeRotationX
                          | RigidbodyConstraints.FreezeRotationZ;

        // Interpolation : mouvement visuel plus fluide (utile pour la caméra).
        corps.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // Update() : lecture des touches (une fois par image).
    private void Update()
    {
        // Si la partie est en pause ou terminée, on ignore les touches.
        if (!JoueurPeutAgir())
        {
            direction = Vector3.zero;
            directionRegard = Vector3.zero;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D ou flèches
        float vertical = Input.GetAxisRaw("Vertical");     // W/S ou flèches

        // Les axes "avant" et "droite" de la caméra, mis à plat (sans Y).
        Vector3 avantCamera = ObtenirAvantCamera();
        Vector3 droiteCamera = Vector3.Cross(Vector3.up, avantCamera);

        // Direction du déplacement = touches exprimées dans le repère
        // de la caméra. ClampMagnitude évite d'aller plus vite en diagonale.
        direction = Vector3.ClampMagnitude(
            avantCamera * vertical + droiteCamera * horizontal, 1f);

        // Vers où le joueur doit-il regarder ?
        if (orienterSurCamera)
        {
            directionRegard = avantCamera;   // toujours comme la caméra
        }
        else
        {
            directionRegard = direction;     // là où il marche
        }

        // On mémorise l'instant où la touche Saut (Espace) est pressée.
        if (Input.GetButtonDown("Jump"))
        {
            dernierAppuiSaut = Time.time;
        }
    }

    // FixedUpdate() : toute la physique (pas de temps fixe).
    private void FixedUpdate()
    {
        VerifierSol();

        Vector3 vitesseActuelle = corps.linearVelocity;

        // Déplacement horizontal : on remplace X et Z mais on garde Y,
        // sinon on casserait la gravité et le saut.
        vitesseActuelle.x = direction.x * vitesse;
        vitesseActuelle.z = direction.z * vitesse;

        // Le saut est autorisé si :
        // - le joueur a appuyé récemment (tampon) ET
        // - il était au sol récemment (coyote time).
        bool veutSauter = Time.time - dernierAppuiSaut <= tempsTamponSaut;
        bool peutSauter = Time.time - dernierInstantAuSol <= tempsCoyote;

        if (veutSauter && peutSauter)
        {
            // Formule physique : v = racine(2 * g * h)
            // donne la vitesse de départ pour atteindre la hauteur voulue.
            vitesseActuelle.y =
                Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * hauteurSaut);

            // On "consomme" le saut pour éviter un double saut involontaire.
            dernierAppuiSaut = -10f;
            dernierInstantAuSol = -10f;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.JouerSaut();
            }
        }
        else if (vitesseActuelle.y < 0f)
        {
            // En descente : on ajoute de la gravité pour une chute plus nerveuse.
            vitesseActuelle.y += Physics.gravity.y
                * (multiplicateurChute - 1f) * Time.fixedDeltaTime;
        }

        corps.linearVelocity = vitesseActuelle;

        TournerVersDirection();
        EnvoyerParametresAnimator();
    }

    // Direction "avant" de la caméra, mise à plat (sans composante verticale).
    // Ainsi, regarder vers le bas n'enfonce pas le joueur dans le sol.
    private Vector3 ObtenirAvantCamera()
    {
        // Sans caméra, on utilise l'orientation actuelle du joueur.
        Vector3 avant = cameraTransform != null
            ? cameraTransform.forward
            : transform.forward;

        avant.y = 0f;

        // Cas rare : caméra pile à la verticale -> direction nulle.
        if (avant.sqrMagnitude < 0.0001f) avant = transform.forward;

        return avant.normalized;
    }

    // Détecte si le joueur touche le sol avec une petite sphère sous les pieds.
    private void VerifierSol()
    {
        Bounds limites = collisionneur.bounds;
        Vector3 centrePieds = new Vector3(
            limites.center.x, limites.min.y - 0.05f, limites.center.z);

        int nombre = Physics.OverlapSphereNonAlloc(
            centrePieds, rayonDetectionSol, resultatsSol,
            coucheSol, QueryTriggerInteraction.Ignore);

        EstAuSol = false;
        for (int i = 0; i < nombre; i++)
        {
            // On ignore les colliders du joueur lui-même.
            if (resultatsSol[i].transform.IsChildOf(transform)) continue;
            EstAuSol = true;
            break;
        }

        if (EstAuSol)
        {
            dernierInstantAuSol = Time.time;
        }
    }

    // Tourne progressivement le joueur vers "directionRegard".
    private void TournerVersDirection()
    {
        if (directionRegard.sqrMagnitude <= 0.001f) return;

        Quaternion cible = Quaternion.LookRotation(directionRegard, Vector3.up);
        corps.MoveRotation(Quaternion.Slerp(
            corps.rotation, cible, vitesseRotation * Time.fixedDeltaTime));
    }

    // Envoie "Speed" et "IsGrounded" à l'Animator (s'il existe).
    private void EnvoyerParametresAnimator()
    {
        if (animator == null) return;

        Vector3 vitesseHorizontale = corps.linearVelocity;
        vitesseHorizontale.y = 0f;

        animator.SetFloat(ParamVitesse, vitesseHorizontale.magnitude);
        animator.SetBool(ParamAuSol, EstAuSol);
    }

    // Appelée par le GameManager : replace le joueur (chute, capture...).
    public void Reapparaitre(Vector3 position)
    {
        corps.position = position;
        transform.position = position;
        corps.linearVelocity = Vector3.zero;
        corps.angularVelocity = Vector3.zero;
    }

    // Vrai si le jeu est en cours (ni pause, ni victoire, ni défaite).
    private bool JoueurPeutAgir()
    {
        return GameManager.Instance == null || GameManager.Instance.JeuEnCours;
    }
}
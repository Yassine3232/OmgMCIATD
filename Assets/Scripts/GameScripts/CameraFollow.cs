using UnityEngine;

// =====================================================================
// CameraFollow : caméra à la 3e personne, dirigée à la SOURIS.
// =====================================================================
// FONCTIONNEMENT :
// 1. La souris fait tourner la caméra autour du joueur :
//    - gauche/droite = rotation horizontale (yaw),
//    - haut/bas      = inclinaison verticale (pitch, limitée).
// 2. La caméra reste TOUJOURS derrière le joueur, à une distance fixe.
//    (Le PlayerController tourne le joueur pour qu'il regarde dans
//    la même direction que la caméra.)
// 3. Le point autour duquel la caméra tourne (le "pivot") suit le joueur
//    avec un lissage SmoothDamp : le suivi est fluide, mais la rotation
//    à la souris reste instantanée.
// 4. Décalage dynamique : le pivot anticipe légèrement le déplacement
//    et monte/descend selon les sauts et les chutes.
// 5. Collision caméra : si un mur passe entre le joueur et la caméra,
//    la caméra se rapproche pour ne pas traverser le mur.
// 6. Le curseur est verrouillé pendant le jeu et libéré dans les menus
//    (pause, victoire, défaite).
//
// MISE EN PLACE : placer ce script sur la Main Camera et glisser le
// joueur dans le champ "Cible" (sinon trouvé via le tag "Player").
// =====================================================================
public class CameraFollow : MonoBehaviour
{
    [Header("Cible")]
    // Objet suivi (le joueur).
    [SerializeField] private Transform cible;

    // Hauteur du pivot au-dessus des pieds du joueur
    // (environ à hauteur des épaules / de la tête).
    [SerializeField] private float hauteurPivot = 1.6f;

    // Distance entre le pivot et la caméra (vers l'arrière).
    [SerializeField] private float distance = 6f;

    [Header("Souris")]
    // Sensibilité de la rotation horizontale et verticale.
    [SerializeField] private float sensibiliteX = 3f;
    [SerializeField] private float sensibiliteY = 2f;

    // Coché : bouger la souris vers le haut baisse la caméra (style "avion").
    [SerializeField] private bool inverserAxeY = false;

    // Limites de l'inclinaison verticale, en degrés.
    // Positif = la caméra regarde vers le bas (elle est au-dessus du joueur).
    [SerializeField] private float anglePitchMin = -20f;
    [SerializeField] private float anglePitchMax = 60f;

    // Cache et verrouille le curseur pendant le jeu.
    [SerializeField] private bool verrouillerCurseur = true;

    [Header("Fluidité")]
    // Temps approximatif pour que le pivot rattrape le joueur (secondes).
    // Petit = caméra nerveuse, grand = caméra souple.
    [SerializeField] private float tempsLissage = 0.08f;

    [Header("Décalage dynamique (effet visuel)")]
    [SerializeField] private bool activerDecalageDynamique = true;

    // Anticipation horizontale : distance ajoutée = vitesse * ce facteur.
    [SerializeField] private float facteurAnticipation = 0.01f;
    [SerializeField] private float anticipationMax = 1.5f;

    // Effet vertical pendant les sauts : vitesse verticale * ce facteur.
    [SerializeField] private float facteurVertical = 0.2f;
    [SerializeField] private float decalageVerticalMax = 1.5f;

    // Lissage propre au décalage dynamique (évite les secousses).
    [SerializeField] private float tempsLissageDecalage = 0.1f;

    [Header("Collision de la caméra")]
    // Empêche la caméra de traverser les murs.
    [SerializeField] private bool activerCollision = true;

    // Couches qui bloquent la caméra (par défaut : tout).
    [SerializeField] private LayerMask couchesObstacles = ~0;

    // Épaisseur du "rayon" de détection et distance minimale au pivot.
    [SerializeField] private float rayonCollision = 0.3f;
    [SerializeField] private float distanceMinimale = 1f;

    // --- Variables internes ---
    private Rigidbody corpsCible;       // pour lire la vitesse du joueur
    private float yaw;                  // angle horizontal de la caméra
    private float pitch = 15f;          // angle vertical de la caméra
    private Vector3 pivotLisse;         // position lissée du pivot
    private Vector3 vitessePivot;       // mémoire de SmoothDamp (pivot)
    private Vector3 decalageDynamique;  // décalage dynamique actuel (lissé)
    private Vector3 vitesseDecalage;    // mémoire de SmoothDamp (décalage)

    // Tableau réutilisé pour la détection de collision (évite les déchets).
    private readonly RaycastHit[] impacts = new RaycastHit[8];

    private void Start()
    {
        // Si aucune cible n'est assignée, on cherche le joueur par son tag.
        if (cible == null)
        {
            GameObject joueur = GameObject.FindGameObjectWithTag("Player");
            if (joueur != null) cible = joueur.transform;
        }

        DefinirCible(cible);

        if (cible != null)
        {
            // Au départ, la caméra regarde dans la même direction que le joueur.
            yaw = cible.eulerAngles.y;
            pivotLisse = cible.position + Vector3.up * hauteurPivot;
        }
    }

    // Permet de changer la cible pendant le jeu.
    public void DefinirCible(Transform nouvelleCible)
    {
        cible = nouvelleCible;
        corpsCible = cible != null ? cible.GetComponent<Rigidbody>() : null;
    }

    // LateUpdate : après tous les Update(), donc après le mouvement du joueur.
    private void LateUpdate()
    {
        if (cible == null) return;

        // La souris ne dirige la caméra que si la partie est en cours.
        bool actif = JeuActif();
        GererCurseur(actif);
        if (actif) LireSouris();

        CalculerDecalageDynamique();

        // 1) Le pivot suit le joueur de façon fluide.
        Vector3 pivotVoulu = cible.position
                           + Vector3.up * hauteurPivot
                           + decalageDynamique;
        pivotLisse = Vector3.SmoothDamp(
            pivotLisse, pivotVoulu, ref vitessePivot, tempsLissage);

        // 2) La rotation vient directement de la souris (pas de lissage).
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // 3) La caméra se place derrière le pivot, dans l'axe de sa rotation.
        //    Vector3.back = (0, 0, -1) : "derrière" dans le repère de la caméra.
        Vector3 versArriere = rotation * Vector3.back;
        float distanceLibre = CalculerDistanceLibre(pivotLisse, versArriere);

        transform.SetPositionAndRotation(
            pivotLisse + versArriere * distanceLibre, rotation);
    }

    // Lit les mouvements de la souris et met à jour yaw et pitch.
    private void LireSouris()
    {
        // Pas de Time.deltaTime ici : la souris donne déjà un déplacement
        // par image, pas une vitesse.
        float sourisX = Input.GetAxis("Mouse X") * sensibiliteX;
        float sourisY = Input.GetAxis("Mouse Y") * sensibiliteY;

        yaw += sourisX;

        // Souris vers le haut => pitch diminue => la caméra regarde plus haut.
        pitch += inverserAxeY ? sourisY : -sourisY;
        pitch = Mathf.Clamp(pitch, anglePitchMin, anglePitchMax);
    }

    // Verrouille le curseur en jeu, le libère dans les menus.
    private void GererCurseur(bool jeuActif)
    {
        if (!verrouillerCurseur) return;

        Cursor.lockState = jeuActif ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !jeuActif;
    }

    private bool JeuActif()
    {
        return GameManager.Instance == null || GameManager.Instance.JeuEnCours;
    }

    private void CalculerDecalageDynamique()
    {
        Vector3 decalageVoulu = Vector3.zero;

        if (activerDecalageDynamique && corpsCible != null)
        {
            Vector3 vitesse = corpsCible.linearVelocity;

            // Anticipation horizontale dans le sens du déplacement.
            Vector3 horizontal = new Vector3(vitesse.x, 0f, vitesse.z) * facteurAnticipation;
            horizontal = Vector3.ClampMagnitude(horizontal, anticipationMax);

            // Décalage vertical : positif en montée, négatif en chute.
            float vertical = Mathf.Clamp(
                vitesse.y * facteurVertical,
                -decalageVerticalMax, decalageVerticalMax);

            decalageVoulu = horizontal + Vector3.up * vertical;
        }

        // On lisse aussi le décalage pour éviter les secousses.
        decalageDynamique = Vector3.SmoothDamp(
            decalageDynamique, decalageVoulu,
            ref vitesseDecalage, tempsLissageDecalage);
    }

    // Distance réellement disponible derrière le pivot :
    // raccourcie si un obstacle (mur...) se trouve sur le chemin.
    private float CalculerDistanceLibre(Vector3 pivot, Vector3 direction)
    {
        if (!activerCollision) return distance;

        // On lance une "sphère" depuis le pivot vers la caméra.
        int nombre = Physics.SphereCastNonAlloc(
            pivot, rayonCollision, direction, impacts, distance,
            couchesObstacles, QueryTriggerInteraction.Ignore);

        float plusProche = distance;
        for (int i = 0; i < nombre; i++)
        {
            // Le joueur ne doit pas bloquer sa propre caméra.
            if (impacts[i].transform.IsChildOf(cible)) continue;

            plusProche = Mathf.Min(plusProche, impacts[i].distance);
        }

        return Mathf.Max(distanceMinimale, plusProche);
    }

    // Place immédiatement la caméra sur sa position finale (sans lissage).
    // Appelée par le GameManager après une réapparition.
    public void TeleporterSurCible()
    {
        if (cible == null) return;

        decalageDynamique = Vector3.zero;
        vitesseDecalage = Vector3.zero;
        vitessePivot = Vector3.zero;
        pivotLisse = cible.position + Vector3.up * hauteurPivot;
    }
}
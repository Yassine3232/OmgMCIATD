using System.Collections;
using UnityEngine;

public class CinematiqueIntro : MonoBehaviour
{
    public Camera camera;                 // la caméra principale
    public Transform joueur;              // le joueur
    public Transform chat;                // l'objectif
    public Transform[] pointsSurvol;      // points de passage au-dessus de la carte, dans l'ordre
    public float vitesse = 8f;            // vitesse du survol
    public float angleVersLeBas = 90f;    // 0 = regarde droit devant, 90 = regarde tout droit vers le bas
    public float pauseSurChat = 2f;       // temps d'arrêt sur le chat
    public Vector3 decalageSuivi = new Vector3(0, 10, -8); // position de la caméra par rapport au joueur
    public MonoBehaviour controleJoueur;  // glisse ton PlayerController ici

    CameraFollow suiviCamera;

    IEnumerator Start()
    {
        // On trouve le script CameraFollow tout seul (même s'il est décoché)
        suiviCamera = camera.GetComponent<CameraFollow>();

        // On bloque le joueur et le suivi de caméra pendant la cinématique
        if (controleJoueur != null) controleJoueur.enabled = false;
        if (suiviCamera != null) suiviCamera.enabled = false;

        // On place la caméra directement sur le premier point, regardant vers le bas
        if (pointsSurvol.Length > 0)
        {
            camera.transform.position = pointsSurvol[0].position;
            camera.transform.rotation = RotationBas(pointsSurvol[0].eulerAngles.y);
        }

        // Survol de la carte
        foreach (Transform p in pointsSurvol)
            yield return StartCoroutine(AllerVers(p.position, RotationBas(p.eulerAngles.y)));

        // Arrêt sur le chat
        Vector3 posChat = chat.position + decalageSuivi * 0.6f;
        Quaternion rotChat = Quaternion.LookRotation(chat.position - posChat);
        yield return StartCoroutine(AllerVers(posChat, rotChat));
        yield return new WaitForSeconds(pauseSurChat);

        // Retour instantané sur le joueur
        camera.transform.position = joueur.position + decalageSuivi;
        camera.transform.rotation = Quaternion.LookRotation(joueur.position - camera.transform.position);

        // On réactive le contrôle et le suivi de caméra
        if (controleJoueur != null) controleJoueur.enabled = true;
        if (suiviCamera != null) suiviCamera.enabled = true;
    }

    Quaternion RotationBas(float yaw)
    {
        return Quaternion.Euler(angleVersLeBas, yaw, 0f);
    }

    IEnumerator AllerVers(Vector3 pos, Quaternion rot)
    {
        Transform c = camera.transform;
        while (Vector3.Distance(c.position, pos) > 0.05f)
        {
            c.position = Vector3.MoveTowards(c.position, pos, vitesse * Time.deltaTime);
            c.rotation = Quaternion.RotateTowards(c.rotation, rot, 90f * Time.deltaTime);
            yield return null;
        }
        c.rotation = rot;
    }
}
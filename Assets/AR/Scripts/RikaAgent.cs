using UnityEngine;

public class RikaAgent : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject avatarModel;
    public ParticleSystem poofParticles;

    void Start()
    {
        // Start hidden when the app boots up
        if (avatarModel != null) 
        {
            avatarModel.SetActive(false);
        }
    }

    public void Materialize()
    {
        if (poofParticles != null) poofParticles.Play(); 
        if (avatarModel != null) avatarModel.SetActive(true);
    }

    public void PoofAway()
    {
        if (poofParticles != null) poofParticles.Play(); 
        if (avatarModel != null) avatarModel.SetActive(false);
    }
}
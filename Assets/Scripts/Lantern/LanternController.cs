using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LanternController : MonoBehaviour
{
    [SerializeField] private Light2D lanternLight; // La lumière de la lanterne
    private float baseIntensity = 0.5f; // Intensité de base de la lumière
    private float baseOuterRadius = 5f; // Rayon extérieur de la lumière
    private float flickerAmount = 0.2f; // Amplitude du scintillement
    private float flickerSpeed = 3f; // Vitesse du scintillement
    private bool isLanternOn = true; // Si la lanterne est allumée ou non
    private float flickerOffset; // Décalage aléatoire pour éviter que le bruit soit trop fixe

    private void Start()
    {
        if (lanternLight == null)
        {
            lanternLight = GetComponent<Light2D>(); // Prendre la lumière attachée si pas assignée
        }

        // Initialiser un décalage de bruit aléatoire pour chaque lumière
        flickerOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (isLanternOn)
        {
            // Appliquer un bruit aléatoire sur l'intensité et le rayon pour un scintillement organique et cohérent
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset); // Perlin noise pour un scintillement fluide
            float flickerValue = (noise - 0.5f) * flickerAmount; // Le -0.5f rend le bruit centré autour de 0

            // Moduler l'intensité et le rayon en même temps
            lanternLight.intensity = baseIntensity + flickerValue;
            lanternLight.pointLightOuterRadius = baseOuterRadius + flickerValue * 0.5f; // Ajuste le rayon proportionnellement
        }
        else
        {
            // Si la lanterne est éteinte, on la rend complètement noire et sans rayon
            lanternLight.intensity = 0f;
            lanternLight.pointLightOuterRadius = 0f;
        }
    }

    // Fonction pour activer/désactiver la lanterne
    public void ToggleLantern(bool state)
    {
        isLanternOn = state;
    }
}

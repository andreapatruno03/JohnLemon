using UnityEngine;

public class ArrowFade : MonoBehaviour
{
    private Material material;
    private Color originalColor;
    private bool isFadingIn = false;
    private float fadeDuration = 1f; // Durata in secondi

    void Start()
{
    material = GetComponent<Renderer>().material;
    originalColor = material.color;
    material.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1); // Visibile al 100%
}


    public void FadeIn()
{
    if (isFadingIn) return; // Evita di avviare nuovamente la dissolvenza
    isFadingIn = true;
    StopAllCoroutines();
    StartCoroutine(Fade(0, originalColor.a));
}

public void FadeOut()
{
    if (!isFadingIn) return; // Evita di avviare una dissolvenza non necessaria
    isFadingIn = false;
    StopAllCoroutines();
    StartCoroutine(Fade(originalColor.a, 0));
}


    private System.Collections.IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        material.color = new Color(originalColor.r, originalColor.g, originalColor.b, endAlpha);
    }
}

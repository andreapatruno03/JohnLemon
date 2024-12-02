using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    public ArrowFade arrow; // L'oggetto che gestisce la dissolvenza della freccia

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && arrow != null)
        {
            arrow.FadeIn();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && arrow != null)
        {
            arrow.FadeOut();
        }
    }
}

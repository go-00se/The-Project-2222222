using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BlinkMove bm = other.GetComponent<BlinkMove>();
        if (bm != null)
        {
            bm.SetCheckpoint(transform.position);
            Debug.Log("Checkpoint activated! New fixed pos = " + transform.position);
        }
    }
}
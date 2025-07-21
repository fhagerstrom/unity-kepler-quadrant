using UnityEngine;

public class DebugGizmo : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, .5f);
        Gizmos.DrawSphere(transform.position, .15f);
    }
}

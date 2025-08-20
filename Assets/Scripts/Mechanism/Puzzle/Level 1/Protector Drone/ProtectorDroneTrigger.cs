using UnityEngine;

public class ProtectorDroneTrigger : MonoBehaviour
{
    [Header("Protector Drone Setting")]
    [SerializeField] GameObject protectorDrone;
    [SerializeField] float posY;

    public void MoveUp()
    {
        if (protectorDrone != null)
        {
            LeanTween.move(protectorDrone, new Vector3(39.84f, posY, -5.432948f), 1f).setEase(LeanTweenType.easeInOutQuad);
        }
        else
        {
            Debug.LogError("ProtectorDrone tidak ditemukan pada parent!", gameObject);
        }
    }
}

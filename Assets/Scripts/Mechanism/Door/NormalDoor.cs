using Unity.VisualScripting;
using UnityEngine;

public class NormalDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("Door Gameobject.")]
    [SerializeField] GameObject door;
    [Tooltip("Door Position to be moved.")]
    [SerializeField] float doorPositionY;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenDoor()
    {
        if (door != null)
        {
            LeanTween.moveY(door, doorPositionY, 0.5f).setEase(LeanTweenType.easeInOutQuad);            
        }
        else
        {
            Debug.LogWarning("Door GameObject is not assigned.");
        }
    }
}

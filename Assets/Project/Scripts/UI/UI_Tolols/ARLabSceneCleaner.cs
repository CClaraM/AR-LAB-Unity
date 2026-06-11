using UnityEngine;

public class ARLabSceneCleaner : MonoBehaviour
{
    [Header("Objects To Disable")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Objects To Destroy")]
    [SerializeField] private GameObject[] objectsToDestroy;

    [Header("Camera")]
    [SerializeField] private GameObject arCameraObject;
    [SerializeField] private bool disableARCamera = true;

    public void CleanScene()
    {
        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        foreach (GameObject obj in objectsToDestroy)
        {
            if (obj != null)
                Destroy(obj);
        }

        if (disableARCamera && arCameraObject != null)
            arCameraObject.SetActive(false);
    }
}
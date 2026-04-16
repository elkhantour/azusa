using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Raycaster : MonoBehaviour
{

    public static Raycaster Instance { get; private set; }
    public static bool Locked { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional, if you want the Raycaster to persist between scenes.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static GameObject GetHitObject()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            //Return hitObject
            return hitObject;

        }

        return null;

    }

    public static Vector3 MouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    private void SendMessageTo(GameObject target, string message)
    {
        target.SendMessage(message, SendMessageOptions.DontRequireReceiver);

    }

    public static void Lock()
    {
        Locked = true;
    }

    public static void Unlock()
    {
        Locked = false;
    }
}

using System;
using UnityEngine;

public class Draggable : MonoBehaviour
{
    /*
     * Handles object hovering raycasting and moving along cursor freely
     */


    public string HitNameFilter { get; set; } = null;
    public GameObject Target { get; private set; } = null;

    private bool Hovered = false;
    private bool Hooked = false;

    private Vector3 Offset;

    private void Update()
    {

        //Define Target
        GameObject hit = Raycaster.GetHitObject();
        if (hit
            && (HitNameFilter != null && hit.name == HitNameFilter)
            && Raycaster.Locked == false
           )
        {
            Target = hit;
        }


        //Define Events
        if (Target)
        {

            /*if (Hovered)
            {
                OnOut();
            }
            if (!Hovered)
            {
                OnHover();
            }*/

            if (Parent(Target).GetInstanceID() == gameObject.transform.GetInstanceID() && Input.GetMouseButtonDown(0))
            {
                Debug.Log(Parent(Target).GetInstanceID() == gameObject.transform.GetInstanceID());
                Offset = Target.transform.parent.position - Raycaster.MouseWorldPosition();
                OnMove();
                Raycaster.Lock();
            }
        }

        //Global Unlock/ Reset on mouse up
        if (Input.GetMouseButtonUp(0))
        {
            Target = null;
            Raycaster.Unlock();
        }
    }

    private Transform Parent(GameObject target)
    {
        return target.transform.parent;
    }

    public void OnMove()
    {
        Debug.Log("move");
        Target.transform.parent.position = Raycaster.MouseWorldPosition() + Offset;
    }


    public void OnHover()
    {
    }

    public void OnOut()
    {
    }

}

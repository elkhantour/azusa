using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridTile : GridMaster
{

    //Parameters
    public bool locked = false; //occupied by a grid object
    private bool active = false; //visually visible, can welcome an object
    private int level = 0; //grid level

    //Private
    private Material material;
    private Color defaultTileColor;

    private void Start()
    {
        //Instiantiate new material from existing one
        material = new Material(GetComponent<Renderer>().material);
        

        //Assign new instance to gameObject
        gameObject.GetComponent<Renderer>().material = material;
        defaultTileColor = material.GetColor("_TileColor");

    }

    private void Update()
    {
        //UpdateStrokeWidth();
    }


    public void setLocked(bool locked)
    {
        if (locked)
        {
            material.SetColor("_TileColor", Color.red);
        }
        else
        {
            material.SetColor("_TileColor", defaultTileColor);
        }
    }

    public void SetGlow(bool glow)
    {
        if (glow)
        {
            material.SetInt("_Glow", 1);
        }
        else
        {
            material.SetInt("_Glow", 0);
        }
    }

    public void SetActive(bool act)
    {
        active = act;

        //disable Mesh Renderer and Collider Component
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        MeshCollider meshCollider = gameObject.GetComponent<MeshCollider>();
        if (active)
        {
            meshRenderer.enabled = true;
            meshCollider.enabled = true;
        }
        else if(active == false)
        {
            meshRenderer.enabled = false;
            meshCollider.enabled = false;
        }
    }

    public void SetLevel(int lvl)
    {
        level = lvl;
    }


    private void OnMouseEnter()
    {

        if(CameraManager.Instance.GetState() == CameraState.Rotate || active == false)
        {
            return;
        }

        SetGlow(true);
        //Update LastTilePosition in Grid
        lastActiveTile = this;

        Vector3 newPosition = transform.position;

        //If a grid object is active
        if (lastActiveObject != null)
        {
            //1.Move the last active object
            //check if object dimension is pair or not
            int zSize = lastActiveObject._zSize;
            int xSize = lastActiveObject._xSize;

            if (xSize % 2 == 0)
            {
                newPosition.x += xSize / (2f * xSize);
            }
            if (zSize % 2 == 0)
            {
                newPosition.z += zSize / (2f * zSize);
            }

            lastActiveObject.MoveToTile(newPosition);

            //2. Check for hovered tiles to make them glow
            /*foreach (GameObject tl in tiles)
            {
                if (GridMasterHelper.isWithinBounds(tl, lastActiveObject.gameObject, xSize, zSize))
                {
                    //if tile is within active object bounds then make it glow to show potential new location
                    tl.GetComponentInChildren<GridTile>().SetGlow(true);
                }
            }*/


        }

        //Move gridPainter
        if (gridPainter != null)
        {
            gridPainter.MoveTo(newPosition);
        }
    }

    private void OnMouseExit()
    {
        SetGlow(false);
    }

    private void UpdateStrokeWidth()
    {

        float maxDistance = 10.0f;
        float maxStrokeWidth = 1.0f; // Set your desired maximum stroke width

        // Calculate the distance to the camera
        float distanceToCamera = Vector3.Distance(transform.parent.position, Camera.main.transform.position);

        // Calculate the new stroke width value based on the distance
        float newStrokeWidth = maxStrokeWidth - (maxStrokeWidth / maxDistance) * distanceToCamera;

        // Make sure the stroke width doesn't go below 0
        newStrokeWidth = Mathf.Max(newStrokeWidth, 0f) * 20;

        Debug.Log(newStrokeWidth);
        // Update the shader property in the material
        material.SetFloat("_StrokeWidth", newStrokeWidth);
    }

}

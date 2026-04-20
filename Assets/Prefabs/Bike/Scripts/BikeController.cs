using UnityEngine;

public class BikeController : MonoBehaviour
{
    public Transform SeatAnchor;
    public bool IsOccupied = false;
    
    // Add your bike movement logic here (rolling, etc.)
    public void SetPhysicsEnabled(bool state)
    {
        // Enable/Disable your bike's custom movement script here
        // this.enabled = state; 
    }
}

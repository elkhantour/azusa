using UnityEngine;

public enum CameraState
{
    REST,
    ROTATE,
    MOVE
}

public enum CameraMode
{
    Editor,
    FollowPlayer,
}

public class CameraManager : MonoBehaviour
{

    private static CameraManager _instance;

    public static CameraManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [Header("Core")]
    public GameObject CameraObject;

    [Space]
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float zoomSpeed = 5f;
    public float rotationSpeed = 2.0f;
    public bool restrictBelowFloor = true;

    //Orbit
    private Vector3 lastHitPoint;
    private float lastAngle = 0.0f;
    private float xSmooth = 0.0f;
    private float mouseX = 0.0f;
    public float smoothTime = 0.3f;
    private float xVelo = 0.0f;

    [Space]
    [Header("Angle")]
    public Transform targetObject; // The object the camera is looking at
    public float minDistance = 5f; // Minimum distance from the object
    public float maxDistance = 20f; // Maximum distance from the object
    public float minAngle = 10f; // Minimum rotation angle when close to the object
    public float maxAngle = 45f; // Maximum rotation angle when far from the object

    public static CameraState state;



    private void Awake()
    {
        _instance = this;
    }

    private void Update()
    {
        // Move the camera
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        MoveCamera(horizontal, vertical);

        // Zoom the camera
        float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
        ZoomCamera(scrollWheel);

        // Orbit around the floor using horizontal scroll only when right mouse button is pressed
        Orbit();

        // Check for right mouse button input
        if (Input.GetMouseButtonDown(1))
        {
            state = CameraState.ROTATE;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            state = CameraState.REST;
        }
    }

    private void MoveCamera(float horizontal, float vertical)
    {

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);
    }

    private void ZoomCamera(float scrollWheel)
    {

        transform.Translate(scrollWheel * zoomSpeed * CameraObject.transform.forward, Space.Self);

        // Raycast downward to find the position of the floor
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit))
        {
            // Check if the hit object is a child of the floor
            if (targetObject && hit.transform.IsChildOf(targetObject))
            {
                // Calculate the distance between the camera and the hit point
                float distance = Vector3.Distance(transform.position, hit.point);

                // Clamp the distance between minDistance and maxDistance
                distance = Mathf.Clamp(distance, minDistance, maxDistance);

                // Interpolate the rotation angle based on the distance
                float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
                float targetAngle = Mathf.Lerp(minAngle, maxAngle, t);

                // Update the child camera's rotation based on the target angle
                Quaternion targetRotation = Quaternion.Euler(targetAngle, transform.eulerAngles.y, transform.eulerAngles.z);
                CameraObject.transform.rotation = Quaternion.Lerp(CameraObject.transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    private void Orbit()
    {
        //means damping is over and no click down
        if (lastAngle == transform.rotation.y && state == CameraState.REST)
        {
            mouseX = 0.0f;

            if (xSmooth > -0.001 && xSmooth < 0.001)
            {
                //only release previous targeted hitpoint when damping is actually close to none (0)
                lastHitPoint = Vector3.zero;
                return;
            }
        }

        if (state == CameraState.ROTATE)
        {
            // Cast a ray from the camera to the floor
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {

                if (lastHitPoint == Vector3.zero)
                {
                    lastHitPoint = hit.point;
                }

                // Orbit the camera around the hit point
                mouseX += (float)(Input.GetAxis("Mouse X") * rotationSpeed * 0.2);

            }
        }
        xSmooth = Mathf.SmoothDamp(xSmooth, mouseX, ref xVelo, smoothTime);
        transform.RotateAround(lastHitPoint, Vector3.up, xSmooth);
        lastAngle = transform.rotation.y;
    }

    private void LateUpdate()
    {
        // Restrict camera below the floor if enabled
        if (restrictBelowFloor)
        {
            RestrictBelowFloor();
        }
    }

    private void RestrictBelowFloor()
    {
        float yPos = Mathf.Max(0f, transform.position.y);
        transform.position = new Vector3(transform.position.x, yPos, transform.position.z);
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
        {
            angle += 360;
        }

        if (angle > 360)
        {
            angle -= 360;
        }

        return Mathf.Clamp(angle, min, max);

    }
}

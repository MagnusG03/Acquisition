using UnityEngine;

public class MainMenuShipFloating : MonoBehaviour
{
    public GameObject ship;
    private float verticalDistance = 0.8f; // How far above and below the starting Y the ship will float
    private float verticalPeriod = 16f; // Time (in seconds) for a full up‐and‐down cycle
    private float horizontalDistance = 0.2f; //How far left and right of the starting X the ship will float
    private float horizontalPeriod = 20f; //Time (in seconds) for a full left‐and‐right cycle
    private Vector3 startPosition;
    private float startTime;

    void Start()
    {
        startPosition = ship.transform.position;

        startTime = Time.time;
    }

    void Update()
    {
        float t = Time.time - startTime;

        float verticalAngle = Mathf.PI * 2f * (t / verticalPeriod);
        float newY = startPosition.y + verticalDistance * Mathf.Sin(verticalAngle);

        float horizontalAngle = Mathf.PI * 2f * (t / horizontalPeriod);
        float newX = startPosition.x + horizontalDistance * Mathf.Sin(horizontalAngle);

        float newZ = startPosition.z;

        ship.transform.position = new Vector3(newX, newY, newZ);
    }
}

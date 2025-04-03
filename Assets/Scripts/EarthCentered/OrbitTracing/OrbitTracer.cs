using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class OrbitTracer
{
    public Satellite satellite;
    public Earth earth;
    private float gravitationalConstant;
    private float timeStep;
    private LineRenderer lineRenderer;
    public int maxSteps;

    public OrbitTracer(Satellite satellite, Earth earth, float gravitationalConstant, float timeStep, int maxSteps)
    {
        this.satellite = satellite;
        this.earth = earth;
        this.gravitationalConstant = gravitationalConstant;
        this.timeStep = timeStep;
        this.maxSteps = maxSteps;

        this.lineRenderer = satellite.GetComponent<LineRenderer>();
    }
    
    public MomentaryOrbitalElements[] CalculateOrbit()
    {
        bool passedAxis = false; // Flag to check if the satellite has passed the axis
        // Definition of vector arrays to store positions, velocities, and momentary orbital elements
        // The size of the arrays is arbitrary and can be adjusted based on the maximum expected number of steps in the simulation
        Vector3[] positions = new Vector3[this.maxSteps];
        Vector3[] velocities = new Vector3[this.maxSteps];
        MomentaryOrbitalElements[] momentaryOrbitalElements = new MomentaryOrbitalElements[this.maxSteps];

        // Calculating values for initial step
        velocities[0] = satellite.velocity;
        positions[0] = satellite.transform.position;

        momentaryOrbitalElements[0].trueAnomaly = GetTrueAnomaly(positions[0], velocities[0]);
        momentaryOrbitalElements[0].radius = (positions[0] - earth.gameObject.transform.position).magnitude;
        momentaryOrbitalElements[0].tangentialVelocity = velocities[0].magnitude;
        momentaryOrbitalElements[0].position = positions[0];

        int i = 0;

        while (i + 1 < this.maxSteps)
        {
            // Calculate satellite acceleration and, using Euler's method, update the velocity and position
            Vector3 acceleration = GetAcceleration(positions[i]);
            velocities[i + 1] = velocities[i] + acceleration * timeStep;
            positions[i + 1] = positions[i] + velocities[i + 1] * timeStep;

            //Debug.Log($"Step: {i} Position: {positions[i + 1]} Velocity: {velocities[i + 1]} Acceleration: {acceleration}");

            // Calculate the true anomaly
            float trueAnomaly = GetTrueAnomaly(positions[i + 1], velocities[i + 1]);

            // Check if the orbit is complete
            if (momentaryOrbitalElements[0].trueAnomaly > trueAnomaly && passedAxis)
            {
                break;
            }

            // Check if the satellite has passed the axis
            else if (momentaryOrbitalElements[i].trueAnomaly - trueAnomaly > 180)
            {
                passedAxis = true;
            }


            // Update the momentary orbital elements
            momentaryOrbitalElements[i + 1].trueAnomaly = trueAnomaly;
            momentaryOrbitalElements[i + 1].radius = (positions[i + 1] - earth.gameObject.transform.position).magnitude;
            momentaryOrbitalElements[i + 1].tangentialVelocity = velocities[i + 1].magnitude;
            momentaryOrbitalElements[i + 1].position = positions[i + 1];

            i += 1;
        }

        return momentaryOrbitalElements;
    }

    public OrbitalElements CalculateGlobalOrbitalElements(MomentaryOrbitalElements[] momentaryOrbitalElements)
    {
        OrbitalElements orbitalElements = new OrbitalElements();

        // Find apoapsis and periapsis from the momentary orbital elements
        float[] radiuss = new float[momentaryOrbitalElements.Length];
        for (int j = 0; j <= radiuss.Length; j++)
        {
            radiuss[j] = momentaryOrbitalElements[j].radius;
        }
        orbitalElements.apoapsis = radiuss.Max();
        orbitalElements.periapsis = radiuss.Min();
        orbitalElements.semiMajorAxis = (orbitalElements.apoapsis + orbitalElements.periapsis) / 2;
        orbitalElements.eccentricity = (orbitalElements.apoapsis - orbitalElements.periapsis) / (orbitalElements.apoapsis + orbitalElements.periapsis);
        orbitalElements.inclination = GetInclination(momentaryOrbitalElements.Last().position, momentaryOrbitalElements.Last().velocity);
        orbitalElements.longitudeOfAscendingNode = GetLongitudeOfAscendingNode(momentaryOrbitalElements.Last().position, momentaryOrbitalElements.Last().velocity);
        orbitalElements.argumentOfPeriapsis = GetArgumentOfPeriapsis(momentaryOrbitalElements.Last().position, momentaryOrbitalElements.Last().velocity);

        return orbitalElements;
    }

    public void DrawOrbit()
    {
        MomentaryOrbitalElements[] momentaryOrbitalElements = CalculateOrbit();
        //OrbitalElements orbitalElements = CalculateGlobalOrbitalElements(momentaryOrbitalElements);

        // Set the number of positions in the line renderer
        lineRenderer.positionCount = momentaryOrbitalElements.Length;
        // Set the positions of the line renderer to the calculated positions
        Vector3[] positions = new Vector3[momentaryOrbitalElements.Length];
        for (int j = 0; j < momentaryOrbitalElements.Length; j++)
        {
            positions[j] = momentaryOrbitalElements[j].position;
        }

        lineRenderer.SetPositions(positions);
        lineRenderer.enabled = true; // Enable the line renderer to visualize the orbit
    }

    private Vector3 GetAcceleration(Vector3 position)
    {
        Vector3 direction = earth.gameObject.transform.position - position;
        float distance = direction.magnitude;
        float scalarAcceleration = (float)(gravitationalConstant * earth.mass / Mathf.Pow(distance, 2));
        Vector3 acceleration = direction.normalized * scalarAcceleration;

        return acceleration;
    }

    private float GetTrueAnomaly(Vector3 position, Vector3 velocity)
    {
        Vector3 r = position - earth.gameObject.transform.position;
        Vector3 v = velocity;

        // Calculate the specific angular momentum vector
        Vector3 h = Vector3.Cross(r, v);

        // Calculate the eccentricity vector
        Vector3 e = Vector3.Cross(h, v)/(float)(gravitationalConstant * earth.mass) - r.normalized;

        // Calculate the true anomaly
        float trueAnomaly = Mathf.Acos(Vector3.Dot(e, r) / (e.magnitude * r.magnitude));

        return trueAnomaly * Mathf.Rad2Deg; // Convert to degrees
    }

    private float GetInclination(Vector3 position, Vector3 velocity)
    {
        Vector3 r = position - earth.gameObject.transform.position;
        Vector3 v = velocity;

        // Calculate the specific angular momentum vector
        Vector3 h = Vector3.Cross(r, v);

        // Calculate the inclination
        float inclination = Mathf.Acos(h.z / h.magnitude) * Mathf.Rad2Deg; // Convert to degrees

        return inclination;
    }

    private float GetLongitudeOfAscendingNode(Vector3 position, Vector3 velocity)
    {
        Vector3 r = position - earth.gameObject.transform.position;
        Vector3 v = velocity;

        // Calculate the specific angular momentum vector
        Vector3 h = Vector3.Cross(r, v);

        // Calculate the node vector
        Vector3 n = new Vector3(-h.y, h.x, 0); // Node vector in the XY plane

        // Calculate the longitude of ascending node
        float longitudeOfAscendingNode = Mathf.Acos(n.x / n.magnitude) * Mathf.Rad2Deg; // Convert to degrees
        if (n.y < 0)
        {
            longitudeOfAscendingNode = 360 - longitudeOfAscendingNode; // Adjust for the correct quadrant
        }

        return longitudeOfAscendingNode;
    }

    private float GetArgumentOfPeriapsis(Vector3 position, Vector3 velocity)
    {
        Vector3 r = position - earth.gameObject.transform.position;
        Vector3 v = velocity;

        // Calculate the specific angular momentum vector
        Vector3 h = Vector3.Cross(r, v);

        // Calculate the eccentricity vector
        Vector3 e = Vector3.Cross(h, v) / (float)(gravitationalConstant * earth.mass) - r.normalized;

        // Calculate the node vector
        Vector3 n = new Vector3(-h.y, h.x, 0); // Node vector in the XY plane

        // Calculate the argument of periapsis
        float argumentOfPeriapsis = (Mathf.Acos(Vector3.Dot(n, e)) / (n.magnitude * e.magnitude)) * Mathf.Rad2Deg; // Convert to degrees
        if (e.y < 0)
        {
            argumentOfPeriapsis = 360 - argumentOfPeriapsis; // Adjust for the correct quadrant
        }

        return argumentOfPeriapsis;
    }
}

public struct OrbitalElements
{
    public float apoapsis; //r_a
    public float periapsis; //r_p
    public double semiMajorAxis;//a
    public double eccentricity; //e
    public double inclination; //i
    public double longitudeOfAscendingNode; //Omega
    public double argumentOfPeriapsis; //Little omega
}

public struct MomentaryOrbitalElements
{
    public float trueAnomaly; //Theta
    public Vector3 velocity; 
    public Vector3 position; 
    public float radius; //r
    public float tangentialVelocity; //v
}

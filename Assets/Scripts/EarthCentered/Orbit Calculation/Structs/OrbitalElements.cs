using UnityEngine;

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

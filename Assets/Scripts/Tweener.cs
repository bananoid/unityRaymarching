using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Tweener : MonoBehaviour
{
    public float speed = 1f;

    float time = 0;

    public Vector2 scaleRange = new Vector2(0.7f, 1.0f);

    public float randomSpeedSize = 100;
    Vector3 rotationSpeed = new Vector3(0, 1, 0);
    Vector3 rotation = new Vector3();
    Vector3 oscillationSpeed;
    public float oscillationSpeedSize = 100;
    float oscillationRadius = 1;

    public Color color;

    public Rigidbody rb;

    public Vector3 attractionPoint = new Vector3(0,0,0);
    public float attractionForce = 1;

    public float pulseK = 40;
    public float pulseN = 40;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        time += Time.deltaTime * speed;
        time = Mathf.Clamp01(time);

        float scale = Mathf.Lerp(
            scaleRange.y,
            scaleRange.x,
            Out(time)
            );

        transform.localScale = new Vector3(scale, scale, scale);
        //transform.position = new Vector3(Mathf.Sin())

        //rotation += rotationSpeed * Time.deltaTime; 
        //transform.rotation = Quaternion.Euler(rotation);


        
        rb.velocity = (attractionPoint - transform.position).normalized * attractionForce;
        //rb.angularVelocity = rotationSpeed;


        float intensity = Mathf.Lerp(
            0.8f,
            0.0f,
            Out(time)
            );
        GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", color * intensity);
    }
    
    public float Out(float k)
    {
        return 1f + ((k -= 1f) * k * k * k * k);
    }

    public float PolyImpulse(float k, float n, float x)
    {
        return (n / (n - 1.0f)) * math.pow((n - 1.0f) * k, 1.0f / n) * x / (1.0f + k * math.pow(x, n));
    }

    float QuaImpulse(float k, float x)
    {
        return 2.0f * math.sqrt(k) * x / (1.0f + k * x * x);
    }

    public void Trig()
    {
        time = 0;

        rotationSpeed.x = UnityEngine.Random.Range(-randomSpeedSize, randomSpeedSize);
        rotationSpeed.y = UnityEngine.Random.Range(-randomSpeedSize, randomSpeedSize);
        rotationSpeed.z = UnityEngine.Random.Range(-randomSpeedSize, randomSpeedSize);

        color = Color.HSVToRGB( Mathf.Abs(UnityEngine.Random.Range(0f,0.12f)) , 0.9f, 1.0f);

        oscillationSpeed.x = UnityEngine.Random.Range(-1, 1) * oscillationSpeedSize;
        oscillationSpeed.y = UnityEngine.Random.Range(-1, 1) * oscillationSpeedSize;
        oscillationSpeed.z = UnityEngine.Random.Range(-1, 1) * oscillationSpeedSize;
        oscillationRadius = UnityEngine.Random.Range(0.1f, 1.0f);

        Vector3 pos = new Vector3();
        pos.x = Mathf.Sin(oscillationSpeed.x * Time.time + oscillationSpeed.z) * oscillationRadius;
        pos.y = Mathf.Cos(oscillationSpeed.y * Time.time + oscillationSpeed.x) * oscillationRadius;
        transform.position = pos;

        rb.velocity += oscillationSpeed;
        rb.angularVelocity = rotationSpeed;
    }
}

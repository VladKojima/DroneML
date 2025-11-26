using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    public DronePickTarget prototype;
    public float radius;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public GameObject Spawn()
    {
        Vector3 center = this.transform.position;
        var obj = Instantiate(prototype).gameObject;
        obj.transform.position = new Vector3(
            center.x + Random.Range(-radius, radius),
            center.y + Random.Range(-radius, radius),
            center.z + Random.Range(-radius, radius)
            );
        obj.SetActive(true);
        return obj;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class CamerasPhoto : MonoBehaviour
{
    private List<Camera> cameras = new List<Camera>();

    private Texture2D takePhoto(Camera cam)
    {
        var currentRT = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        cam.Render();

        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);

        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();

        RenderTexture.active = currentRT;
        return image;
    }

    public void takePhotos()
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            takePhoto(cameras[i]).GetPixels();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (Camera cam in GetComponentsInChildren<Camera>())
        {
            cameras.Add(cam);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}

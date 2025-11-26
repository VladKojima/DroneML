using UnityEditor;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [Header("Blocks")]
    public GameObject wall;
    public GameObject door;
    public GameObject floor;

    [Header("Params")]
    public int width = 1;
    public int length = 1;
    public int seed = 42;
    [Range(0, 1)]
    public float chance = 0.5f;

    private bool[] placed;

    private void Grow(int x, int y, Transform point)
    {
        var cell = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/scripts/Level/Floor.prefab"), point);

        cell.tag = "Floor";

        string[] pnames = { "pointTop", "pointBottom", "pointLeft", "pointRight" };

        foreach (string pname in pnames)
        {
            if (Random.value < chance)
            {
                Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/scripts/Level/Wall.prefab"), cell.transform.Find(pname));
            }
        }

        placed[x + y * width] = true;

        if (y > 0 && !placed[x + (y - 1) * width])
            Grow(x, y - 1, cell.transform.Find("growBottom"));

        if (y < length - 1 && !placed[x + (y + 1) * width])
            Grow(x, y + 1, cell.transform.Find("growTop"));

        if (x > 0 && !placed[x - 1 + y * width])
            Grow(x - 1, y, cell.transform.Find("growLeft"));

        if (x < width - 1 && !placed[x + 1 + y * width])
            Grow(x + 1, y, cell.transform.Find("growRight"));
    }

    void Start()
    {
        Random.InitState(seed);

        placed = new bool[length * width];

        Grow(0, 0, transform);
    }
}

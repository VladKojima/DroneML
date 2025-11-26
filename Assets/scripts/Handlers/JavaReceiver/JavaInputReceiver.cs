using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class JavaInputReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    public int listenPort = 5005;

    private UdpClient udpClient;
    private Thread listenThread;
    private volatile bool running = false;

    // Последние полученные значения
    public float thrust = 0f;
    public float pitch = 0f;
    public float roll = 0f;
    public float yaw = 0f;

    void Start()
    {
        udpClient = new UdpClient(listenPort);
        running = true;
        listenThread = new Thread(ListenLoop);
        listenThread.IsBackground = true;
        listenThread.Start();
        Debug.Log($"JavaInputReceiver: слушаем UDP на порту {listenPort}");
    }

    void ListenLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, listenPort);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string msg = Encoding.UTF8.GetString(data);

                //Debug.Log($"{msg}");
                // Ожидаем JSON вида {"thrust":0.5,"pitch":0.1,"roll":-0.2,"yaw":0.3}
                JavaInputPacket packet = JsonUtility.FromJson<JavaInputPacket>(msg);

                thrust = (Mathf.Clamp(packet.thrust, 0, 1f) * 2) - 1;
                pitch = Mathf.Clamp(packet.pitch, -1f, 1f);
                roll = Mathf.Clamp(packet.roll, -1f, 1f) * -1;
                yaw = Mathf.Clamp(packet.yaw, -1f, 1f);
                Debug.Log($"парс - {packet.ToString()}");
            }
            catch { }
        }
    }

    void OnDestroy()
    {
        running = false;
        if (listenThread != null && listenThread.IsAlive)
            listenThread.Abort();
        udpClient?.Close();
    }
}

[System.Serializable]
public class JavaInputPacket
{
    public float thrust;
    public float pitch;
    public float roll;
    public float yaw;
}

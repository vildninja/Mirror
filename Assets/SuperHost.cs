using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.UI;

public class SuperHost : MonoBehaviour
{
    public const byte SetName = 1;
    public const byte MatchDropped = 2;
    public const byte MatchWon = 3;
    public const byte MatchLost = 4;
    public const byte MatchStarting = 5;
    public const byte RequestMatch = 6;

    public bool isClient = true;

    public string myName = "player";
    public bool inMatch = false;

    private int myHostID;
    private int myConID;
    private int reliableID;
    private int stateID;
    private byte error = 0;
    private readonly byte[] bytes = new byte[300];

    private MemoryStream stream;
    private BinaryReader reader;
    private BinaryWriter writer;

    private readonly List<int> matchQueue = new List<int>();
    private readonly Dictionary<int, string> players = new Dictionary<int, string>();
    private readonly Dictionary<int, int> matches = new Dictionary<int, int>();

    public GameObject connectingPanel;
    public GameObject namePanel;
    public GameObject youWonPanel;
    public GameObject youLostPanel;
    public GameObject gameDroppedPanel;
    public GameObject waitingForMatchPanel;

    void Awake()
    {
        stream = new MemoryStream(bytes);
        reader = new BinaryReader(stream);
        writer = new BinaryWriter(stream);
    }

    private void ShowSinglePanel(GameObject show)
    {
        connectingPanel.SetActive(connectingPanel == show);
        namePanel.SetActive(namePanel == show);
        youWonPanel.SetActive(youWonPanel == show);
        youLostPanel.SetActive(youLostPanel == show);
        gameDroppedPanel.SetActive(gameDroppedPanel == show);
        waitingForMatchPanel.SetActive(waitingForMatchPanel == show);
    }

	// Use this for initialization
	void Start ()
    {
        var global = new GlobalConfig();
	    global.MaxPacketSize = 300;

        NetworkTransport.Init();

        var config = new ConnectionConfig();
	    reliableID = config.AddChannel(QosType.ReliableSequenced);
	    stateID = config.AddChannel(QosType.Unreliable);

        var topology = new HostTopology(config, 200);
	    myHostID = NetworkTransport.AddHost(topology, isClient ? 0 : 16688);

        Debug.Log("Host ID " + myHostID);


	    if (isClient)
	    {
            // pew.dk "188.226.164.147"
            // local "192.168.1.14"
            myConID = NetworkTransport.Connect(myHostID, "188.226.164.147", 16688, 0, out error);
	    }

	    if (!TestError("Connecting to host"))
        {
            Debug.Log("Connection established " + myConID);
        }
	}

    private float countDown = 0;
	// Update is called once per frame
	void Update ()
	{
	    int hostID;
	    int connectionID;
	    int channelID;
	    int receivedSize;

	    for (int i = 0; i < 10; i++)
	    {
	        switch (NetworkTransport.Receive(out hostID, out connectionID, out channelID, bytes, 300, out receivedSize, out error))
	        {
	            case NetworkEventType.DataEvent:
	                if (!isClient)
                    {
	                    if (channelID == reliableID)
                            ReliableSuper(connectionID, receivedSize, bytes);
	                    else if (channelID == stateID)
                            RelayMessage(connectionID, receivedSize);
	                }
	                else
	                {
                        if (channelID == reliableID)
                            Info();
	                    else if (channelID == stateID)
	                        State();
	                }
	                break;
	            case NetworkEventType.ConnectEvent:
	                if (!isClient)
	                {
	                    if (!players.ContainsKey(connectionID))
	                    {
	                        players.Add(connectionID, "P" + connectionID);
                            Debug.Log("Player connected: " + connectionID);
	                    }
	                }
	                else
                    {
                        ShowSinglePanel(namePanel);
	                }
	                break;
                case NetworkEventType.DisconnectEvent:
                    if (!isClient)
                    {
                        Debug.Log("Player left: " + connectionID);
                        if (players.ContainsKey(connectionID))
                        {
                            players.Remove(connectionID);
                            matchQueue.Remove(connectionID);

                            if (matches.ContainsKey(connectionID))
                            {
                                var other = matches[connectionID];
                                matches.Remove(connectionID);
                                matches.Remove(other);
                                if (players.ContainsKey(other))
                                    SendMatchDropped(other);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Lost conn to server");
                        myConID = NetworkTransport.Connect(myHostID, "188.226.164.147", 16688, 0, out error);
                        ShowSinglePanel(connectingPanel);
                    }
	                break;
	            case NetworkEventType.BroadcastEvent:
                    break;
                default:
                    i = 10;
                    break;
	        }
	    }

	    if (isClient && inMatch)
	    {
	        countDown -= Time.deltaTime;

	        if (countDown < 0)
	        {
	            countDown = 0.1f;
	            stream.Position = 0;
	            CityManager.instance.WriteData(writer);
	            NetworkTransport.Send(myHostID, myConID, stateID, bytes, (int) stream.Position, out error);
	            TestError("Sending State Update of " + stream.Position + " bytes");

	            if (CityManager.instance.myScore >= 10)
	            {
	                bytes[0] = MatchWon;
                    NetworkTransport.Send(myHostID, myConID, reliableID, bytes, 1, out error);
	            }
	        }
	    }
	    
        if (!isClient)
	    {
	        if (matchQueue.Count > 1)
	        {
	            int a = matchQueue[0];
	            int b = matchQueue[1];
	            matchQueue.RemoveAt(0);
	            matchQueue.RemoveAt(0);

	            matches.Add(a, b);
	            matches.Add(b, a);

	            bytes[0] = MatchStarting;
	            bytes[1] = 1;
	            int l = WriteText(2, players[a]);
	            NetworkTransport.Send(myHostID, b, reliableID, bytes, 2 + l, out error);

	            TestError("Initiate b: " + error + " a " + a + " b " + b);

                bytes[1] = 0;
                l = WriteText(2, players[b]);
	            NetworkTransport.Send(myHostID, a, reliableID, bytes, 2 + l, out error);

	            TestError("Initiate a: " + error + " a " + a + " b " + b);

	            Debug.Log("Match Initiated: " + a + " vs " + b);
	        }
	    }
	}

    public void SetNameAndJoinQueue()
    {
        var field = namePanel.GetComponentInChildren<InputField>();

        if (string.IsNullOrEmpty(field.text))
        {
            field.Select();
            return;
        }

        myName = field.text;

        CityManager.instance.me.name = myName;

        ShowSinglePanel(null);

        bytes[0] = SetName;
        int l = WriteText(1, myName);
        NetworkTransport.Send(myHostID, myConID, reliableID, bytes, 1 + l, out error);
        TestError("SetName after connect");

        JustJoinTheQueue();
    }

    public void JustJoinTheQueue()
    {
        bytes[0] = RequestMatch;
        NetworkTransport.Send(myHostID, myConID, reliableID, bytes, 1, out error);
        TestError("RequestMatch first time");

        ShowSinglePanel(waitingForMatchPanel);
    }

    private bool TestError(string info)
    {
        if (error > 0)
        {
            Debug.LogError("Err_" + error + " > " + info);
            return true;
        }
        return false;
    }

    private void ReliableSuper(int conID, int size, byte[] bytes)
    {
        switch (bytes[0])
        {
            case SetName:
                players[conID] = ReadText(1);
                Debug.Log("player #" + conID + " changed name to " + players[conID]);
                break;
            case RequestMatch:
                Debug.Log("player #" + conID + " joined the queue");
                if (!matchQueue.Contains(conID))
                    matchQueue.Add(conID);
                break;
            case MatchWon:
                int other;
                if (matches.TryGetValue(conID, out other))
                {
                    bytes[0] = MatchWon;
                    NetworkTransport.Send(myHostID, conID, reliableID, bytes, 1, out error);
                    bytes[0] = MatchLost;
                    NetworkTransport.Send(myHostID, other, reliableID, bytes, 1, out error);
                    matches.Remove(conID);
                    matches.Remove(other);
                }
                break;
        }
    }

    private void RelayMessage(int conID, int size)
    {
        int to;
        bool found = false;
        if (matches.TryGetValue(conID, out to))
        {
            found = true;
            if (size < 10)
                Debug.Log("Relay " + size + " bytes from + " + conID + " to " + to);
            NetworkTransport.Send(myHostID, to, stateID, bytes, size, out error);
        }
        else 
        {
            SendMatchDropped(conID);
        }

        TestError("Relay to " + (found ? "c" + to : "missing"));
    }

    private void SendMatchDropped(int conID)
    {
        bytes[0] = MatchDropped;
        Debug.Log("RelayFailed Missing other player");
        NetworkTransport.Send(myHostID, conID, reliableID, bytes, 1, out error);
    }

    private void State()
    {
        stream.Position = 0;
        CityManager.instance.ReadData(reader);
    }

    private void Info()
    {
        switch (bytes[0])
        {
            case MatchStarting:
                CityManager.instance.flipData = bytes[1] == 1;
                string text = ReadText(2);
                CityManager.instance.other.name = text;
                CityManager.instance.BuildCity();
                ShowSinglePanel(null);
                inMatch = true;
                break;
            case MatchLost:
                ShowSinglePanel(youLostPanel);
                inMatch = false;
                break;
            case MatchWon:
                ShowSinglePanel(youWonPanel);
                inMatch = false;
                break;
            case MatchDropped:
                // ignore this if match is already over
                if (inMatch)
                {
                    ShowSinglePanel(gameDroppedPanel);
                    inMatch = false;
                }
                break;
        }
    }

    private string ReadText(int start, int cap = 20)
    {
        return System.Text.Encoding.UTF8.GetString(bytes, start + 1, Mathf.Clamp(bytes[start], 1, cap));
    }

    private int WriteText(int start, string text, int cap = 20)
    {
        if (string.IsNullOrEmpty(text))
            text = "_";
        if (text.Length > cap)
            text = text.Substring(0, cap);
        var b = System.Text.Encoding.UTF8.GetBytes(text);

        bytes[start] = (byte) b.Length;
        for (int i = 0; i < b.Length; i++)
        {
            bytes[start + 1 + i] = b[i];
        }

        return b.Length + 1;
    }

    public void KillConnection()
    {
        NetworkTransport.Disconnect(myHostID, myConID, out error);
    }

    void OnDestroy()
    {
        if (NetworkTransport.IsStarted)
        {
            NetworkTransport.Shutdown();
        }
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}

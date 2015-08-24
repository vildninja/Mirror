using UnityEngine;
using System.Collections;
using System.Text;


public class CityCube : MonoBehaviour
{
    public GameObject debris;

    public bool isUpper;

    private Renderer rend;

    private int _hp = 2;

    public int hp
    {
        get { return _hp; }
        set
        {
            _hp = value;
            changed = true;
        }
    }
    public int x , y;

    public Texture2D cracks;

    public bool changed = false;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
    }

    public void SetState(bool upper, bool fromNet = false)
    {
        if (!fromNet)
            changed = true;

        isUpper = upper;
        _hp = 2;
        UpdateGfx();
        gameObject.layer = LayerMask.NameToLayer(isUpper ? "UpperCity" : "LowerCity");
        if (!isUpper)
        {
            rend.transform.localPosition = new Vector3(0, 0, -0.5f);
        }
        else
        {
            rend.transform.localPosition = new Vector3(0, 0, 0.5f);
        }
    }

    void OnMouseDown()
    {
        changed = true;
    }

    public void UpdateGfx()
    {
        rend.material.color = CityManager.GetTeamColor(!isUpper);
        if (hp == 1)
            rend.material.mainTexture = cracks;
        else
            rend.material.mainTexture = null;
    }


    private const byte _0 = 0;
    private const byte _1 = 1 << 0;
    private const byte _2 = 1 << 1;
    private const byte _3 = 1 << 2;
    private const byte _4 = 1 << 3;
    private const byte _5 = 1 << 4;
    private const byte _6 = 1 << 5;
    private const byte _7 = 1 << 6;
    private const byte _8 = 1 << 7;

    public void FromNet(byte b, bool flip)
    {
        bool u = (b & _1) == _1;
        if (flip) u = !u;

        bool d = (b & _2) == _2;
        bool c = (b & _3) == _3;

        string txt = Bits(b);

        if (changed && d == (_hp == 1) && u == isUpper)
        {
            changed = false;
        }
        else if (c)
        {
            changed = false;
            //Debug.Log("Changed block: " + x + "," + y + " lcb " + lcb + " ch " + change);
            if (u != isUpper)
            {
                SetState(u, true);
            }
            if (d && hp != 1)
            {
                _hp = 1;
                UpdateGfx();
            }
        }
    }


    public byte ToNet(bool flip)
    {
        //(flip ? !isUpper : isUpper)
        return (byte)((isUpper != flip ? _1 : _0) | (_hp == 1 ? _2 : _0) | (changed ? _3 : _0));
    }

    //[ContextMenu("Print net")]
    //public void PrintNetByte()
    //{
    //    Debug.Log(" u " + isUpper + " d " + (_hp == 1));
    //    Debug.Log("Net byte: " + Bits(ToNet()) + " lcb: " + Bits(lcb));

    //    var b = ToNet();

    //    bool u = (b & _7) == _7;
    //    bool d = (b & _8) == _8;
    //    byte change = (byte)(b & _t);

    //    Debug.Log(" u " + u + " d " + d + " change " + Bits((change)) + " n:" + b + " lcb:" + lcb);
    //}

    private string Bits(byte b)
    {
        string str = "";
        str += ((b & _1) == _1 ? 1 : 0);
        str += ((b & _2) == _2 ? 1 : 0);
        str += ((b & _3) == _3 ? 1 : 0);
        str += ((b & _4) == _4 ? 1 : 0);
        str += ((b & _5) == _5 ? 1 : 0);
        str += ((b & _6) == _6 ? 1 : 0);
        str += ((b & _7) == _7 ? 1 : 0);
        str += ((b & _8) == _8 ? 1 : 0);
        return str;
    }
}

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public class CityManager : MonoBehaviour
{
    public static CityManager instance;

    public int width;
    public int height;
    public byte[] level;
    public List<CityCube[]> city;

    public CityCube cubePrefab;

    private Vector3 ll;

    public Color upperColor;
    public Color lowerColor;

    public Text topText;
    public Text bottomText;

    public Rigidbody me;
    public Rigidbody other;

    public int myScore = 0;
    public int otherScore = 0;

    public bool flipData;

    public static Color GetTeamColor(bool upper)
    {
        return upper ? instance.upperColor : instance.lowerColor;
    }

	// Use this for initialization
	void Awake ()
	{
	    instance = this;

        city = new List<CityCube[]>();
        level = new byte[width];

        ll = new Vector3(-width / 2f + 0.5f, -height / 2f + 0.5f);

	    BuildCity();
	}

    public void BuildCity()
    {
        foreach (var col in city)
        {
            for (int y = 0; y < col.Length; y++)
            {
                Destroy(col[y].gameObject);
            }
        }
        city.Clear();


        float lastGround = height / 2f;
        float ground;
        for (int x = 0; x < width; x++)
        {
            var cc = new CityCube[height];
            city.Add(cc);
            do
            {
                ground = Random.Range(height / 3f, 2 * height / 3f);
            } while (Mathf.Abs(ground - lastGround) < height / 7f);
            lastGround = ground;
            level[x] = (byte)Mathf.FloorToInt(ground);
            for (int y = 0; y < height; y++)
            {
                var cube = Instantiate(cubePrefab);
                cube.transform.parent = transform;
                cube.transform.localPosition = ll + new Vector3(x, y);
                cube.x = x;
                cube.y = y;

                cc[y] = cube;

                cube.SetState(ground < y, true);

                if (!flipData)
                    cube.changed = true;
            }
        }

        myScore = 0;
        otherScore = 0;

        me.position = Vector3.up * 7;
        me.velocity = Vector3.zero;
        other.position = Vector3.down * 7;
        other.velocity = Vector3.zero;
    }

    void Update()
    {
        bottomText.text = otherScore + " " + other.name;
        topText.text = me.name + " " + myScore;
    }

    void FixedUpdate()
    {
        // Gravity
        me.AddForce(Vector3.down * 10, ForceMode.Acceleration);
        other.AddForce(Vector3.up * 10, ForceMode.Acceleration);
    }

    public void ScorePoint()
    {
        me.position = Vector3.up * 7;
        me.velocity = Vector3.zero;

        myScore++;

        for (int x = 0; x < width; x++)
        {
            var col = city[x];
            for (int y = 0; y < height / 3; y++)
            {
                if (!col[y].isUpper)
                    col[y].SetState(false);
            }
        }
    }

    public void ReadData(BinaryReader stream)
    {
        stream.ReadBoolean();
        otherScore = stream.ReadInt32();

        for (int x = 0; x < width; x++)
        {
            var col = city[x];

            for (int y = 0; y < height; y++)
            {
                // flipData ? (height - 1 - y) : 
                col[flipData ? (height - 1 - y) : y].FromNet(stream.ReadByte(), flipData);
            }
        }

        Vector2 pos;
        Vector2 vel;

        pos.x = stream.ReadSingle();
        pos.y = -stream.ReadSingle();
        vel.x = stream.ReadSingle();
        vel.y = -stream.ReadSingle();

        other.position = pos;
        other.velocity = vel;
    }

    private ulong counter;
    public void WriteData(BinaryWriter stream)
    {
        stream.Write(flipData);
        stream.Write(myScore);

        for (int x = 0; x < width; x++)
        {
            var col = city[x];
            for (int y = 0; y < height; y++)
            {
                // flipData ? (height - 1 - y) : 
                stream.Write(col[flipData ? (height - 1 - y) : y].ToNet(flipData));
            }
        }

        Vector2 pos = me.position;
        Vector2 vel = me.velocity;

        stream.Write(pos.x);
        stream.Write(pos.y);
        stream.Write(vel.x);
        stream.Write(vel.y);
    }

    public void Hit(GameObject block, int power = 1)
    {
        var cc = block.GetComponent<CityCube>();
        var col = city[cc.x];

        bool upper = cc.isUpper;

        DestroyBlock(col, cc.y, power, cc.isUpper);

        //for (int y = cc.y; y >= 0 && y < col.Length && col[y].isUpper == upper; y += upper ? -1 : 1)
        //{

        //}

        //if (cc.isUpper)
        //{

        //}

        //cc.hp -= force > 0.5f ? 2 : 1;
        //if (cc.hp <= 0)
        //    cc.SetState(!cc.isUpper);
        //else
        //    cc.UpdateGfx();
    }

    private bool DestroyBlock(CityCube[] col, int y, int power, bool upper)
    {
        if (power <= 0)
            return false;

        if (y < 0 || y >= height)
            return true;

        var cc = col[y];
        if (cc.isUpper != upper)
            return true;

        if (cc.hp == 2)
        {
            cc.hp = 1;
            cc.UpdateGfx();
            DestroyBlock(col, y + (upper ? -1 : 1), power - 1, upper);
            return false;
        }

        if (cc.hp == 1)
        {
            if (DestroyBlock(col, y + (upper ? -1 : 1), power, upper))
            {
                cc.SetState(!upper);
                return true;
            }
            return false;
        }

        cc.SetState(!upper);
        return true;
    }
}

using UnityEngine;
using System.Collections;
using System.Linq;

public class ControllerLayer
{
    public bool isUpper;

    public bool Jump
    {
        get
        {
            return isUpper ? Input.GetKeyDown(KeyCode.W) : Input.GetKeyDown(KeyCode.K);
        }
    }

    public int Move
    {
        get
        {
            int move = isUpper ? (Input.GetKey(KeyCode.A) ? -1 : 0) : (Input.GetKey(KeyCode.J) ? -1 : 0);
            return move + (isUpper ? (Input.GetKey(KeyCode.D) ? 1 : 0) : (Input.GetKey(KeyCode.L) ? 1 : 0));
        }
    }

    public int Hit
    {
        get
        {
            int hit = isUpper ? (Input.GetKeyDown(KeyCode.Q) ? -1 : 0) : (Input.GetKeyDown(KeyCode.U) ? -1 : 0);
            return hit + (isUpper ? (Input.GetKeyDown(KeyCode.E) ? 1 : 0) : (Input.GetKeyDown(KeyCode.O) ? 1 : 0));
        }
    }

    public bool Smash
    {
        get
        {
            return isUpper ? Input.GetKeyDown(KeyCode.S) : Input.GetKeyDown(KeyCode.I);
        }
    }
}

public class Monster : MonoBehaviour {

    private Animator anim;
    private Rigidbody body;
    private Renderer rend;

    private ControllerLayer controller;

    public bool isUpper;

    public float power = 10;
    public float charge = 2;

    public float hitCost = 3;
    public float smashCost = 2;


    public float maxVel = 2;

    public float jumpCooldown = 1;
    private float jumpDelay;

    private bool isGrounded;

    public Renderer chargeBar;
    public Color green;
    public Color yellow;
    public Color red;

    public Collider scoreArea;

	// Use this for initialization
	void Awake ()
    {
        anim = GetComponent<Animator>();
        body = GetComponent<Rigidbody>();
	    rend = GetComponentInChildren<Renderer>();

        controller = new ControllerLayer() {isUpper = isUpper};
	}

    void Start()
    {
        rend.material.color = CityManager.GetTeamColor(isUpper);
    }
	
	// Update is called once per frame
	void Update ()
	{
	    power += charge*Time.deltaTime;
	    power = Mathf.Clamp(power, 0, 10);

        chargeBar.transform.localScale = new Vector3(power, 1f, 0.2f);
	    chargeBar.material.color = power > smashCost ? green : power > hitCost ? yellow : red;

	    jumpDelay += Time.deltaTime;
        if (controller.Jump && jumpDelay > jumpCooldown)
        {
            jumpDelay = 0;
            body.AddForce((isUpper ? Vector3.up : Vector3.down) * 10, ForceMode.Impulse);
        }

	    int hit = controller.Hit;

	    if (hit != 0 && power > hitCost)
	    {
	        power -= hitCost;
	        RaycastHit res;
	        if (Physics.Raycast(transform.position, new Vector3(hit, 0, 0), out res, 0.7f,
	            LayerMask.GetMask((isUpper ? "LowerCity" : "UpperCity"))))
	        {
	            CityManager.instance.Hit(res.collider.gameObject);
	        }
	    }

        if (controller.Smash && power > smashCost && !isGrounded)
        {
            power -= smashCost;
            body.AddForce((isUpper ? Vector3.down : Vector3.up) * 5, ForceMode.Impulse);
            smashStarted = true;
        }
	}

    private bool smashStarted = false;
    void FixedUpdate()
    {
        if (Physics.OverlapSphere(body.position, 0.2f,
            LayerMask.GetMask((isUpper ? "LowerCity" : "UpperCity"))).Length > 0)
        {
            body.position = body.position + (isUpper ? Vector3.up : Vector3.down);
        }


        float x = body.velocity.x;
        int move = controller.Move;

        if ((move == 1 && x < maxVel) || (move == -1 && x > -maxVel))
        {
            body.AddForce(controller.Move, 0, 0, ForceMode.VelocityChange);
        }
        if (move == 0)
        {
            body.AddForce(-x * 3, 0, 0, ForceMode.Force);
        }

        if (smashStarted)
        {
            body.AddForce((isUpper ? Vector3.down : Vector3.up) * 50, ForceMode.Acceleration);
        }

        isGrounded = false;
    }

    void OnCollisionStay(Collision col)
    {
        if (Mathf.Abs(col.transform.position.x - transform.position.x) < 0.5f &&
            col.transform.position.y < transform.position.y == isUpper)
        {
            isGrounded = true;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        bool hitGround = Mathf.Abs(col.transform.position.x - transform.position.x) < 0.5f &&
                         col.transform.position.y < transform.position.y == isUpper;

        if (!isGrounded && hitGround && smashStarted && Mathf.Abs(col.relativeVelocity.y) > 1)
        {
            int smashCharge = Mathf.RoundToInt(Mathf.Abs(col.relativeVelocity.y) / 10);
            Debug.Log("SMASH: " + smashCharge);
            smashStarted = false;

            foreach (var hit in Physics.RaycastAll(transform.position,
                isUpper ? Vector3.down : Vector3.up, smashCharge,
	            LayerMask.GetMask((isUpper ? "LowerCity" : "UpperCity"))))
            {
                CityManager.instance.Hit(hit.collider.gameObject, smashCharge);
            }
        }

        if (hitGround)
        {
            isGrounded = true;
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col == scoreArea)
        {
            
        }
    }
}

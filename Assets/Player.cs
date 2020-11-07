using System;
using UnityEngine;


public class Player : MonoBehaviour
{
    /// <summary>The player's handheld lantern.</summary>
    public GameObject lantern;
    /// <summary>Whether the player is currently lit by their lantern.</summary>
    public bool HoldingLight => this.lantern.activeSelf;
    /// <summary>The player's current sanity level.</summary>
    public float Sanity { get; private set; } = 1f;
    /// <summary>How lit the player currently is.</summary>
    /// <remarks>A value of .3 is approximately full moonlight, as bright as the game normally gets.</remarks>
    public float Shadow
    {
        get {
            if (Time.fixedTime != this.shadowAge)
                return this.GetShadow();
            else
                return this.shadowCache;
        }
    }
    /// <summary>Camera used to track how hidden the player is.</summary>
    public Camera lightCam;
    /// <summary>Camera the player sees through.</summary>
    public Camera mainCam;

    // texture used to project light camera onto
    private Texture2D texture;
    // time the player's light level was last checked
    private float shadowAge = 0f;
    // player's previous light value
    private float shadowCache;


    // calculate level of light  player is under
    private float GetShadow()
    {
        if (this.HoldingLight)
            return 0.5f;

        // load the colors from the light camera
        RenderTexture camTarget = this.lightCam.targetTexture;
        RenderTexture.active = camTarget;
        Rect area = new Rect(0, 0, camTarget.width, camTarget.height);
        this.texture.ReadPixels(area, 0, 0);
        this.texture.Apply();
        RenderTexture.active = null;

        // get the brightness of the pixels from the light camera
        Color32[] pixels = this.texture.GetPixels32();
        int sumR = 0, sumG = 0, sumB = 0;
        foreach (Color32 color in pixels) {
            sumR += color.r;
            sumG += color.g;
            sumB += color.b;
        }
        this.shadowCache = (sumR * .229f + sumG * .587f + sumB * .114f) / 255 / pixels.Length;
        this.shadowAge = Time.fixedTime;
        Debug.Log(String.Format("light: {0}", this.shadowCache));
        return shadowCache;
    }


    // store references to attached objects and components needed later
    private void Awake()
    {
        RenderTexture camTarget = this.lightCam.targetTexture;
        this.texture = new Texture2D(camTarget.width, camTarget.height);
    }

    // update player sanity each tick
    private void FixedUpdate()
    {
        if (this.CanSeeMonster())
            this.Sanity -= Time.fixedDeltaTime / 6;
        //this.Sanity -= Mathf.Clamp(.2f - this.Shadow, -.05f, .3f)
                       //* Time.fixedDeltaTime / 10;
        this.Sanity = Mathf.Clamp(this.Sanity, 0f, 1f);
        Debug.Log(String.Format("sanity: {0}", this.Sanity));
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        this.transform.Translate(move.normalized * Time.fixedDeltaTime, Space.Self);
    }

    // check if player can see a monster, considering obstacles and face angle
    private bool CanSeeMonster()
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject monster in monsters) {
            Vector3 diff = monster.transform.position - this.transform.position;
            if (Physics.Raycast(
                this.transform.position,
                diff,
                out RaycastHit _,
                diff.magnitude,
                LayerMask.GetMask("Walls")
            ))
                continue;
            float angle = this.mainCam.transform.rotation.eulerAngles.y;
            angle -= Vector3.Angle(
                this.transform.forward,
                monster.transform.position - this.transform.position
            );
            if (Mathf.Abs(angle) < 70)
                return true;
        }
        return false;
    }

    // handle user button input
    private void Update()
    {
        if (Input.GetButtonDown("Lantern"))
            this.lantern.SetActive(!this.lantern.activeSelf);
        RenderSettings.fogEndDistance = this.Sanity * 970 + 30;
    }
}

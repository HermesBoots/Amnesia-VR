using UnityEngine;
using UnityEngine.AI;


/// <summary>Represents what state the monster is using to determine its movement.</summary>
public enum MonsterStates { IDLE, SEARCHING, CHASING }


/// <summary>Represents the monsters that provide the player's primary threat.</summary>
public class Monster : MonoBehaviour
{
    /// <summary>Player object to look for and chase.</summary>
    public Player player;
    /// <summary>This monster's normal patrol route when not chasing.</summary>
    public Vector3[] route;

    // whether the monster saw the player on the last tick
    private bool sawPlayer;
    // which direction from the player's last location the monster is checking
    private char searchDirection;
    // which index in the route array this monster is heading toward
    private int routeIndex;
    // what action this monster is taking
    private MonsterStates state;
    // navigation component
    private NavMeshAgent nav;
    // where a pursuing monster thinks the player is
    private Vector3 target;


    // change the monster's current state to the given one, calling its init function
    private void ChangeState(MonsterStates state)
    {
        switch (state) {
            case MonsterStates.IDLE:
                this.InitIdle();
                break;
            case MonsterStates.CHASING:
                this.InitChase();
                break;
            case MonsterStates.SEARCHING:
                this.InitSearch();
                break;
        }
        this.state = state;
    }


    // set up the chase state
    void InitChase()
    {
        this.sawPlayer = true;
        this.target = this.player.transform.position;
        this.GetComponent<Renderer>().material.color = Color.red;
    }

    // set up the idle state
    void InitIdle()
    {
        this.GetComponent<Renderer>().material.color = Color.green;
        float minDistance = 10000000f;
        int minIndex = 0;
        for (this.routeIndex = 0; this.routeIndex < this.route.Length; this.routeIndex++) {
            Vector3 position = this.route[this.routeIndex];
            float distance = (position - this.transform.position).sqrMagnitude;
            if (distance < minDistance) {
                minDistance = distance;
                minIndex = this.routeIndex;
            }
        }
        this.routeIndex = minIndex;
        this.target = this.route[this.routeIndex];
        this.nav.SetDestination(this.route[this.routeIndex]);
    }

    // set up the search state
    private void InitSearch()
    {
        this.searchDirection = 'N';
        this.GetComponent<Renderer>().material.color = Color.yellow;
    }


    // handle state where monster chases player
    private void DoChase()
    {
        if (this.CanSee(this.player.transform.position)) {
            this.sawPlayer = true;
            this.target = this.player.transform.position;
        } else if (this.sawPlayer) {
            this.sawPlayer = false;
            this.target = (this.player.transform.position - this.target) * 5;
        } else if (this.CanSee(this.target)) {
            this.ChangeState(MonsterStates.SEARCHING);
        }
        this.nav.SetDestination(this.target);
    }

    // handle state where monster is unaware of player
    private void DoIdle()
    {
        if (this.CanSee(this.player.transform.position))
            this.ChangeState(MonsterStates.CHASING);
        else if ((this.target - this.transform.position).sqrMagnitude < 1f) {
            if (this.routeIndex == this.route.Length - 1)
                this.routeIndex = 0;
            else
                this.routeIndex++;
            this.target = this.route[this.routeIndex];
            this.nav.SetDestination(this.route[this.routeIndex]);
        }
    }

    // handle state where monster just saw player recently
    private void DoSearch()
    {
        Vector3 spot = Vector3.zero;
        switch (this.searchDirection) {
            case 'N':
                spot = new Vector3(this.target.x, this.target.y, this.target.z + 1);
                break;
            case 'E':
                spot = new Vector3(this.target.x + 1, this.target.y, this.target.z);
                break;
            case 'S':
                spot = new Vector3(this.target.x, this.target.y, this.target.z - 1);
                break;
            case 'W':
                spot = new Vector3(this.target.x - 1, this.target.y, this.target.z);
                break;
        }
        if ((spot - this.transform.position).sqrMagnitude < .5f)
            switch (this.searchDirection) {
                case 'N':
                    this.searchDirection = 'E';
                    break;
                case 'E':
                    this.searchDirection = 'S';
                    break;
                case 'S':
                    this.searchDirection = 'W';
                    break;
                case 'W':
                    this.searchDirection = 'X';
                    break;
            }
        if (this.searchDirection == 'X')
            this.ChangeState(MonsterStates.IDLE);
        else
            this.nav.SetDestination(spot);
    }


    // check whether the monster can see a target, considering obstacles,
    // face angle, and (for player only) light level
    private bool CanSee(Vector3 target)
    {
        if (this.CastAt(target).distance > 0f)
            return false;
        if (Mathf.Abs(
            this.transform.rotation.eulerAngles.y -
            Vector3.Angle(this.transform.position, target)
        ) > 60)
            return false;
        if (target == this.player.transform.position) {
            float distance = Mathf.Abs(Vector3.Distance(
                this.transform.position,
                target
            ));
            if (distance < .05f)
                return true;
            // with this, extreme darkness visible at .5m and moonlight at 20m
            if (Mathf.Pow(distance / (this.player.Shadow * 10f), 3f) < 1000f)
                return true;
        }
        return false;
    }

    // cast a ray at a point, checking if any walls are in the way
    private RaycastHit CastAt(Vector3 target)
    {
        RaycastHit ret;
        Vector3 direction = target - this.transform.position;
        Physics.Raycast(
            this.transform.position,
            target - this.transform.position,
            out ret,
            direction.magnitude,
            LayerMask.GetMask("Walls")
        );
        return ret;
    }


    // store references to needed components on this object
    private void Awake() => this.nav = this.GetComponent<NavMeshAgent>();

    // put the monster in its idle state when it's activated
    private void Start() => this.ChangeState(MonsterStates.IDLE);

    // call the appropriate state function every tick
    private void FixedUpdate()
    {
        switch (this.state) {
            case MonsterStates.IDLE:
                this.DoIdle();
                break;
            case MonsterStates.CHASING:
                this.DoChase();
                break;
            case MonsterStates.SEARCHING:
                this.DoSearch();
                break;
        }
        GameObject.Find("Tracer").transform.position = this.target;
    }

    // draw a line in front of the monster to show which way it's facing
    private void Update()
    {
        Vector3 angle = Vector3.RotateTowards(this.transform.forward, this.transform.right, Mathf.PI / 3, 0f);
        Debug.DrawRay(this.transform.position, angle * 2, Color.red);
        angle = Vector3.RotateTowards(this.transform.forward, -this.transform.right, Mathf.PI / 3, 0f);
        Debug.DrawRay(this.transform.position, angle * 2, Color.red);
    }
}

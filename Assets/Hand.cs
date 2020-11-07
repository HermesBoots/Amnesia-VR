using UnityEngine;


/// <summary>Represents one of the player's hands, for lifting objects.</summary>
public class Hand : MonoBehaviour
{
    // which object the hand is currently in contact with
    private Rigidbody held = null, touching = null;
    // how far away the hand is from the object it holds
    private Vector3 offset;

    // flag when touching a phsyics object
    private void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.TryGetComponent<Rigidbody>(out this.touching);
    }

    // remove reference to touched object when no longer touching
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == this.touching.gameObject)
            this.touching = null;
    }

    // pull held objects toward the hand holding it
    private void FixedUpdate()
    {
        if (this.held == null)
            return;
        Vector3 force = this.transform.position - this.offset - this.held.position;
        force *= force.sqrMagnitude;
        this.held.velocity = Vector3.zero;
        this.held.AddForce(force);
    }

    // handle buttons for grabbing and releasing objects
    private void Update()
    {
        if (this.touching == null)
            return;
        if (Input.GetButtonDown("Grab")) {
            this.held = this.touching;
            this.offset = this.touching.position - this.transform.position;
        }
        else if (Input.GetButtonUp("Grab")) {
            this.held = null;
        }
    }
}

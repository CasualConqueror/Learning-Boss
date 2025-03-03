using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColumnBreakScript : MonoBehaviour
{
    public GameObject unbrokenColumn;
    public GameObject brokenColumn;

    // This determines whether the column will be broken or unbroken at runtime
    public bool isBroken;

    // Audio for column breaking
    public AudioClip breakSound;
    private AudioSource audioSource;

    // Layer mask for detecting enemies
    public LayerMask enemyLayer;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && breakSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        if (isBroken)
        {
            BreakColumn();
        }
        else
        {
            unbrokenColumn.SetActive(true);
            brokenColumn.SetActive(false);
        }
    }

    // Detect collision with enemies and break the column
    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) != 0)
        {
            BreakColumn();
        }
    }

    // Modified to be public so it can be called by BossAttackSystem
    public void BreakColumn()
    {
        if (isBroken) return; // Prevent breaking already broken columns

        isBroken = true;
        unbrokenColumn.SetActive(false);
        brokenColumn.SetActive(true);

        // Add physics components to broken pieces if they don't have them
        SetupBrokenPieces();

        // Play break sound if available
        if (audioSource != null && breakSound != null)
        {
            audioSource.PlayOneShot(breakSound);
        }

        Debug.Log("Column broke! At position: " + transform.position);
    }

    private void SetupBrokenPieces()
    {
        if (brokenColumn != null)
        {
            // Get all child pieces of the broken column
            Transform[] pieces = brokenColumn.GetComponentsInChildren<Transform>();

            foreach (Transform piece in pieces)
            {
                // Skip the parent object
                if (piece == brokenColumn.transform) continue;

                // Add rigidbody if it doesn't exist
                if (piece.GetComponent<Rigidbody>() == null)
                {
                    Rigidbody rb = piece.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 10f; // Set appropriate mass
                }

                // Add collider if it doesn't exist
                if (piece.GetComponent<Collider>() == null)
                {
                    // Add box collider as default
                    piece.gameObject.AddComponent<BoxCollider>();
                }
            }
        }
    }

    // Added for debugging - will show column's collider in the scene view
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}

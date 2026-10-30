using UnityEngine;

public class TestPlayerInput : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerInteraction playerInteraction;
    private PlayerCombat playerCombat;
    private PlayerThrowing playerThrowing;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInteraction = GetComponent<PlayerInteraction>();
        playerCombat = GetComponent<PlayerCombat>();
        playerThrowing = GetComponent<PlayerThrowing>();
    }

    private void Update()
    {
        Vector2 move = Vector2.zero;
        if (Input.GetKey(KeyCode.I)) move.y += 1f;
        if (Input.GetKey(KeyCode.K)) move.y -= 1f;
        if (Input.GetKey(KeyCode.L)) move.x += 1f;
        if (Input.GetKey(KeyCode.J)) move.x -= 1f;

        if (playerMovement.enabled)
        {
            playerMovement.SetMoveInput(move.normalized);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            playerThrowing.StartCharging();
        }

        if (playerThrowing.IsCharging())
        {
            if (Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.H))
            {
                playerThrowing.CancelThrow();
            }
            else if (Input.GetKeyUp(KeyCode.Y))
            {
                playerThrowing.ReleaseThrow();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            playerInteraction.TryInteracting();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            playerCombat.TryAttacking();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            playerInteraction.SendRequest(InteractionType.Eat, null);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            playerInteraction.SendRequest(InteractionType.Drop, null);
        }
    }
}

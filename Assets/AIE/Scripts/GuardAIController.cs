using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class GuardAIController : MonoBehaviour
{
    public NavMeshAgent navAgent;

    private Vector3 initialPosition;

    private BasicNavMeshAIController chasePlayer;

    private void Start()
    {
        initialPosition = transform.position;
        navAgent.updatePosition = false;
    }

    private void Update()
    {
        if(chasePlayer != null)
        {
            navAgent.destination = chasePlayer.transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // when the player enters the trigger, let's start chasing them
        if (other.TryGetComponent<BasicNavMeshAIController>(out var player))
        {
            chasePlayer = player;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // when the player exits the trigger, let's give up
        if (chasePlayer != null &&
            other.TryGetComponent<BasicNavMeshAIController>(out var player))
        {
            if(player == chasePlayer)
            {
                chasePlayer = null;

                navAgent.destination = initialPosition;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class WeaponController : NetworkBehaviour
{
    public GameObject Meele;
    public bool CanAttack = true;
    public float AttackCooldown = 1.0f;
    public bool isAttacking = false;
    public float AttackWindow = 1.0f;

    public Transform weaponOwner;

    public bool CanBlock = true;
    public bool isBlocking = false;
    public float BlockCooldown = 0f;
    
    public float ParryCooldown = 0f;

    public bool CanParry = true;

    public float LastBlockTime = 0f;

    public float BlockDuration = 0f;

    public bool isPlayer;
    
    public CollisionDetection collisionDetection;
    
    // Remove SyncVar attributes
    private bool _syncedIsAttacking = false;
    private bool _syncedIsBlocking = false;

    void Start()
    {
        ParryCooldown = 0f;
        Animator anim = Meele.GetComponent<Animator>();
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == "SwordAttack") 
            {
                AttackCooldown = clip.length;
                AttackWindow = clip.length;
                break;
            }
        }
    }

    void Update()
    {
        if (isPlayer && IsOwner)
        {
            CheckBlocktime();
            
            if (Input.GetMouseButtonDown(0))
            {
                if (CanAttack && !isBlocking)
                {
                    MeeleAttack();
                    AttackServerRpc(true);
                }
            }

            if (Input.GetMouseButton(1))
            {
                if (CanBlock && !isAttacking)
                {
                    LastBlockTime = Time.time;
                    BlockDuration += Time.deltaTime;
                    Block();
                    BlockServerRpc(true);
                }
            }

            if (Input.GetMouseButtonUp(1) && isBlocking)
            {
                StopBlocking();
                BlockServerRpc(false);
            }
        }
    }
    
    private void StopBlocking()
    {
        isBlocking = false;
        CanBlock = false;
        BlockDuration = 0f; 
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetBool("IsBlocking", false);
        StartCoroutine(ResetBlockCooldown());
    }

    public void CheckBlocktime()
    {
        if (Time.time - LastBlockTime > ParryCooldown)
        {
            CanParry = true;
        }
        else
        {
            CanParry = false;
        }
    }   

    public void MeeleAttack()
    {
        if (isAttacking) return;
        
        isAttacking = true;
        if (collisionDetection != null)
            collisionDetection.ResetHit();
            
        CanAttack = false;
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetTrigger("Attack");
        
        StartCoroutine(ResetAttackCooldown());
    }

    public void Block()
    {
        isBlocking = true;
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetBool("IsBlocking", true);
    }

    public bool SuccessfulParry()
    {   
        return BlockDuration <= 0.2f && CanParry;
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    IEnumerator ResetAttackCooldown()
    {
        StartCoroutine(ResetIsAttacking());
        yield return new WaitForSeconds(AttackCooldown);
        CanAttack = true;
    }

    IEnumerator ResetBlockCooldown()
    {
        yield return new WaitForSeconds(BlockCooldown);
        CanBlock = true;
    }

    IEnumerator ResetIsAttacking()
    {
        yield return new WaitForSeconds(AttackWindow);
        isAttacking = false;
        
        // When animation is done, update server
        if (IsOwner)
            AttackServerRpc(false);
    }
    
    // Network RPC methods
    [ServerRpc]
    private void AttackServerRpc(bool attacking)
    {
        _syncedIsAttacking = attacking;
        SynchronizeAttackObserversRpc(attacking);
    }
    
    [ServerRpc]
    private void BlockServerRpc(bool blocking)
    {
        _syncedIsBlocking = blocking;
        SynchronizeBlockObserversRpc(blocking);
    }
    
    [ObserversRpc(ExcludeOwner = true)]
    private void SynchronizeAttackObserversRpc(bool attacking)
    {
        // Handle the attack state change for clients
        if (attacking && !isAttacking)
        {
            MeeleAttack();
        }
        else if (!attacking && isAttacking)
        {
            isAttacking = false;
        }
    }
    
    [ObserversRpc(ExcludeOwner = true)]
    private void SynchronizeBlockObserversRpc(bool blocking)
    {
        // Handle the block state change for clients
        if (blocking && !isBlocking)
        {
            Block();
        }
        else if (!blocking && isBlocking)
        {
            StopBlocking();
        }
    }
}

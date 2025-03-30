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

    // Use SyncVar for isBlocking
    public readonly SyncVar<bool> isBlocking = new SyncVar<bool>();

    public float BlockCooldown = 0f;
    public float ParryCooldown = 0f;
    public bool CanParry = true;
    public float LastBlockTime = 0f;
    public float BlockDuration = 0f;
    public bool isPlayer;
    public bool meeleIsSet = false;
    public bool autoAttack = false;

    public CollisionDetection collisionDetection;
    
    // Remove SyncVar attributes
    private bool _syncedIsAttacking = false;

    private void Awake()
    {
        isBlocking.OnChange += OnBlockingChanged;
    }

    private void OnBlockingChanged(bool previousValue, bool newValue, bool asServer)
    {
        Debug.Log($"isBlocking changed from {previousValue} to {newValue} (asServer: {asServer})");

        // Update animations or other logic based on the new value
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetBool("IsBlocking", newValue);
    }

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
        Debug.Log($" isBlocking.Value = {isBlocking.Value}");
        if (isPlayer && IsOwner)
        {
            CheckBlocktimeServerRpc();

            if(!meeleIsSet){
                if (transform.childCount == 3)
                {
                    Meele = transform.GetChild(1).gameObject; // Get the second child (index 1)
                    Debug.Log($"Meele set to: {Meele.name}");
                    meeleIsSet = true;
                }
                else
                {
                    Debug.LogError("WeaponController does not have a second child to assign as Meele.");
                }

                // Assign collisionDetection to the CollisionDetection script on Meele
                if (Meele != null)
                {
                    collisionDetection = Meele.GetComponent<CollisionDetection>();
                    if (collisionDetection != null)
                    {
                        Debug.Log("CollisionDetection script assigned successfully.");
                    }
                    else
                    {
                        Debug.LogError("Meele does not have a CollisionDetection script attached.");
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (CanAttack && !isBlocking.Value) // Use .Value to read the SyncVar
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
                    UpdateBlockDurationServerRpc(BlockDuration);
                    Block();
                    BlockServerRpc(true);
                }
            }

            if (Input.GetMouseButtonUp(1) && isBlocking.Value) // Use .Value to read the SyncVar
            {
                StopBlocking();
                BlockServerRpc(false);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                autoAttack = !autoAttack;
            }

            if (autoAttack && CanAttack && !isBlocking.Value)
            {
                MeeleAttack();
                AttackServerRpc(true);
            }
        }
    }

    [ServerRpc]
    private void UpdateBlockDurationServerRpc(float duration)
    {
        BlockDuration = duration;
    }
    
    private void StopBlocking()
    {
        // UnityEngine.Debug.Log("StopBlocking");
        SetBlocking(false); // Use .Value to set the SyncVar
        CanBlock = false;
        BlockServerRpc(false);
        BlockDuration = 0f;
        UpdateBlockDurationServerRpc(0f);
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetBool("IsBlocking", false);
        StartCoroutine(ResetBlockCooldown());
    }

  
    [ServerRpc]
    public void CheckBlocktimeServerRpc()
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
        NotifyAnimationObserversRpc("Attack");
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetTrigger("Attack");
        
        
        StartCoroutine(ResetAttackCooldown());
    }

    [ServerRpc] private void SetBlocking(bool value) => isBlocking.Value = value;


    public void Block()
    {
        if (!CanBlock || isAttacking) return;
        print("Blocking");
        SetBlocking(true); // Use .Value to set the SyncVar
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetBool("IsBlocking", true);
    }

    [Server]
    public bool SuccessfulParry()
    {
        bool isParrySuccessful = BlockDuration <= 0.4f && CanParry;
        Debug.Log($"Parry check on server: BlockDuration = {BlockDuration}, CanParry = {CanParry}, Success = {isParrySuccessful}");
        return isParrySuccessful;
    }

    

    // public bool IsBlocking()
    // {
    //     return isBlocking.;
    // }

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
        // _syncedIsBlocking = blocking;
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
        if (blocking && !isBlocking.Value)
        {
            Block();
        }
        else if (!blocking && isBlocking.Value)
        {
            StopBlocking();
        }
    }

    [ObserversRpc]
    private void NotifyAnimationObserversRpc(string animationName)
    {
        Animator anim = Meele.GetComponent<Animator>();
        anim.Play(animationName);
    }
}

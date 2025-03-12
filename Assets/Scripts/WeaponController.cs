using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{

    public GameObject Meele;
    public bool CanAttack = true;
    public float AttackCooldown = 1.0f;
    //public AudioClip WeaponAttackSound;
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

    // Start is called before the first frame update
    void Start()
    {
        // ParryCooldown = BlockCooldown + 0.5f;
        ParryCooldown =  0f;
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

    // Update is called once per frame
    void Update()
    {
        if(isPlayer == true)
        {
            CheckBlocktime();
            if (Input.GetMouseButtonDown(0))
            {
                if (CanAttack && !isBlocking)
                {
                    MeeleAttack();
                }
            }

            if (Input.GetMouseButton(1))
            {
                // UnityEngine.Debug.Log(BlockDuration);
                if (CanBlock && !isAttacking)
                {
                    LastBlockTime = Time.time;
                    BlockDuration += Time.deltaTime;
                    Block();
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                isBlocking = false;
                CanBlock = false;
                BlockDuration = 0f; 
                Animator anim = Meele.GetComponent<Animator>();
                anim.SetBool("IsBlocking", false);
                StartCoroutine(ResetBlockCooldown());

            }
        }

        //Dummy Attacking
        else
        {
            MeeleAttack();
        }
    }

    public void CheckBlocktime()
    {
        if (Time.time - LastBlockTime > ParryCooldown)
        {
            CanParry = true;
        }
        else
        {
            //  UnityEngine.Debug.Log(Time.time - LastBlockTime);
            CanParry = false;
        }
    }   

    public void MeeleAttack()
    {
        if (isAttacking){
            return;
        }
        isAttacking = true;
        collisionDetection.ResetHit();
        CanAttack = false;
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetTrigger("Attack");
        
       // AudioSource ac = GetComponent<AudioSource>();

       // ac.PlayOneShot(WeaponAttackSound);
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
        return BlockDuration <= 1.5f && CanParry;
    }

    public bool IsBlocking()
    {
        return isBlocking;
    }

    IEnumerator ResetAttackCooldown()
    {
        StartCoroutine(ResetIsAttacking());
        yield return new WaitForSeconds(AttackCooldown);
        UnityEngine.Debug.Log("CanAttack is True");
        CanAttack = true;
    }

    IEnumerator ResetBlockCooldown()
    {
        yield return new WaitForSeconds(BlockCooldown);
        CanBlock= true;
    }

    IEnumerator ResetIsAttacking()
    {
        yield return new WaitForSeconds(AttackWindow);
        isAttacking = false;
    }
}

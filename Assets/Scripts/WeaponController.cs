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
    public float BlockCooldown = 1.0f;

    public bool isPlayer;
    
    public CollisionDetection collisionDetection;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(isPlayer == true)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (CanAttack && !isBlocking)
                {
                    MeeleAttack();
                }
            }

            if (Input.GetMouseButton(1))
            {
                if (CanBlock && !isAttacking)
                {
                    Block();
                }
            }

            if (Input.GetMouseButtonUp(1))
            {
                isBlocking = false;
                CanBlock = false;
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

    public void MeeleAttack()
    {
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

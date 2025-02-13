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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (CanAttack)
            {
                MeeleAttack();
            }
        }
    }

    public void MeeleAttack()
    {
        isAttacking = true;
        CanAttack = false;
        Animator anim = Meele.GetComponent<Animator>();
        anim.SetTrigger("Attack");
       // AudioSource ac = GetComponent<AudioSource>();

       // ac.PlayOneShot(WeaponAttackSound);
        StartCoroutine(ResetAttackCooldown());
    }

    IEnumerator ResetAttackCooldown()
    {
        StartCoroutine(ResetIsAttacking());
        yield return new WaitForSeconds(AttackCooldown);
        UnityEngine.Debug.Log("CanAttack is True");
        CanAttack = true;
    }
    IEnumerator ResetIsAttacking()
    {
        yield return new WaitForSeconds(AttackWindow);
        isAttacking = false;
    }
}

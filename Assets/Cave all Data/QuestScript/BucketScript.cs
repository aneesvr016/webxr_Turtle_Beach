using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BucketScript : MonoBehaviour
{
    public Animator WaterBucketAnim;
    public Animator VenigarBucketAnim;

    public GameObject Waterparticle;
    public GameObject Venigarparticle;

    public GameObject WaterInterectable;
    public GameObject VenigarInterectable;


    public GameObject VenigarObj;
    // Start is called before the first frame update
    void Start()
    {
       // OnDrop("Vinegar");
    }

    public void OnDrop(string info)
    {
       if(info == "Water")
        {
            StartCoroutine(DropWater());
        }
        else if (info == "Vinegar")
        {
            StartCoroutine(DropVenigor());
        }
    }
    IEnumerator DropWater()
    {
        WaterInterectable.SetActive(false);
        VenigarInterectable.SetActive(false);
        WaterBucketAnim.Play("Drop");
        yield return new WaitForSeconds(0.8f);
        Waterparticle.SetActive(true);
        yield return new WaitForSeconds(3f);
        WaterBucketAnim.Play("Up");
        Waterparticle.SetActive(false);
        yield return new WaitForSeconds(0.8f);
        WaterInterectable.SetActive(true);
        VenigarInterectable.SetActive(true);
    }
    IEnumerator DropVenigor()
    {
        WaterInterectable.SetActive(false);
        VenigarInterectable.SetActive(false);
        VenigarBucketAnim.Play("Drop");
        yield return new WaitForSeconds(0.8f);
        Venigarparticle.SetActive(true);
        yield return new WaitForSeconds(0.8f);
        VenigarObj.SetActive(true);
        yield return new WaitForSeconds(3f);
        VenigarBucketAnim.Play("Up");
        Venigarparticle.SetActive(false);
        yield return new WaitForSeconds(0.8f);
        VenigarObj.SetActive(false);
        WaterInterectable.SetActive(true);
        VenigarInterectable.SetActive(true);
    }
}

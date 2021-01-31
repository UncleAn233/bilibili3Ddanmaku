using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiftObject : MonoBehaviour
{
    bool isCatched = false;
    string giftName;

    Rigidbody rigidbody;

    private void Awake()
    {
        rigidbody = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (DanmakuManager.Instance.Pause || DanmakuManager.Instance.GiftPause)
        {
            recyleSelf();
        }   
    }

    private void OnDisable()
    {
        isCatched = false;
        StopAllCoroutines();
    }

    private void OnEnable()
    {
        transform.rotation = Random.rotation;
        StartCoroutine(recyleSelf_Cor());
    }
    void recyleSelf()
    {
        DanmakuPool.Instance.recyleGift(this.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        this.rigidbody.AddForce((this.transform.position - collision.gameObject.transform.position) * 0.1f);
    }
    IEnumerator recyleSelf_Cor()
    {
        yield return new WaitForSeconds(DanmakuManager.Instance.GiftLifeTime);
        if (!isCatched)
        {
            recyleSelf();
        }
    }
}

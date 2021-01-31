using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Character : MonoBehaviour
{
    bool yVelocityLock = true;
    string text;
    Rigidbody rigidbody;
    MeshCollider meshCollider;

    private void Awake()
    {
        text = gameObject.name.Replace("(Clone)","");
        if(GetComponent<Rigidbody>() == null)
            rigidbody = gameObject.AddComponent<Rigidbody>();
        if(GetComponent<MeshCollider>() == null)
        {
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        meshCollider.convex = true;
        //meshCollider.material = Resources.Load("Meterial/CharacterPM") as PhysicMaterial;
        transform.rotation = Quaternion.identity;
    }

    private void Start()
    {
        transform.localScale = new Vector3(DanmakuManager.Instance.dmCharacterSize, DanmakuManager.Instance.dmCharacterSize, DanmakuManager.Instance.dmCharacterSize) * 10;
    }
    private void OnEnable()
    {
        resetCharacter();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
    private void FixedUpdate()
    {
        if (yVelocityLock && Mathf.Abs(rigidbody.velocity.y) > DanmakuManager.Instance.dmDownSpeed)
        {
            rigidbody.velocity = new Vector3(rigidbody.velocity.x, -1 * DanmakuManager.Instance.dmDownSpeed, rigidbody.velocity.z);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Ground")
        {
            rigidbody.freezeRotation = false;
            
        }
        if(collision.gameObject.tag == "player")
        {
            
        }
    }
    void recyleSelf()
    {
        DanmakuPool.Instance.recyleCharacter(this.gameObject, text);
    }
    
    IEnumerator recyleSelf_Cor()
    {
        yield return new WaitForSeconds(DanmakuManager.Instance.dmLifeTime);
        Debug.Log("回收");
        recyleSelf();
    }
    
    void resetCharacter()
    {
        rigidbody.velocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
        rigidbody.freezeRotation = true;
        rigidbody.AddForce(0, -10 * DanmakuManager.Instance.dmDownSpeed, 0);
        StartCoroutine(recyleSelf_Cor());
    }
}

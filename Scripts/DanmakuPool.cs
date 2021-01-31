using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DanmakuManager;
using static DanmakuModel;

/// <summary>
/// 弹幕池
/// </summary>
public class DanmakuPool : MonoBehaviour
{
    //礼物
    GameObject giftPreb; //礼物预设 有需要可改为礼物组
    Queue<GameObject> giftPool; //礼物池

    //弹幕
    Dictionary<string, Queue<GameObject>> characterPool = new Dictionary<string, Queue<GameObject>>(); //字符池
    const string path = "3DCharacters/3dText"; //字体路径

    static DanmakuPool instance;
    public static DanmakuPool Instance 
    {
        get{
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        giftPool = new Queue<GameObject>();
    }

    private void Start()
    {
        giftPreb = Resources.Load<GameObject>("Prebs/Cube");
    }

    public List<GameObject> getGift(int num)
    {
        List<GameObject> list = new List<GameObject>();
        for (var i = 0; i < num; i++)
        {
            if (giftPool.Count > 0)
            {
                list.Add(giftPool.Dequeue());
            }
            else
            {
                list.Add(GameObject.Instantiate(giftPreb));
            }
        }
        return list;
    }
    public void recyleGift(GameObject gift)
    {
        giftPool.Enqueue(gift);
        gift.SetActive(false);
    }

    /// <summary>
    /// 字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public GameObject getCharacter(string str)
    {
        if (!characterPool.ContainsKey(str))
        {
            try
            {
                var temp = (Resources.Load<GameObject>("3DCharacters/3dText")).transform.Find(str).gameObject;
                characterPool.Add(str, new Queue<GameObject>());
                var ob = GameObject.Instantiate(temp);
                ob.AddComponent<Character>();
                return ob;
            }
            catch (NullReferenceException e)
            {
                Debug.Log(e.ToString());
                throw new NoSuchCharacterException(str);
            }
        }
        else
        {
            if (characterPool[str].Count > 0)
            {
                var ob =  characterPool[str].Dequeue();
                ob.SetActive(true);
                return ob;
            }
            else
            {
                var ob = GameObject.Instantiate((Resources.Load(path) as GameObject).transform.Find(str).gameObject);
                ob.AddComponent<Character>();
                return ob;
            }
        }
    }
    public void recyleCharacter(GameObject character, string str)
    {
        characterPool[str].Enqueue(character);
        character.SetActive(false);
    }
}

public class NoSuchCharacterException : Exception
{
    public string error;
    public NoSuchCharacterException(string str)
    {
        error = "Not find the model of the character: " + str;
    }
}
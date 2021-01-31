using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DanmakuModel;

/// <summary>
/// 弹幕显示相关
/// </summary>
/// 
public class DanmakuManager : MonoBehaviour
{
    public enum DanmakuType { DM, GIFT, SC };

    [TooltipAttribute("弹幕生成位置")]
    public Vector3 dmStartPos = Vector3.zero;
    [TooltipAttribute("弹幕生成范围")]
    public float dmGenerateLimit = 20f;
    [TooltipAttribute("弹幕下落速度")]
    [Range(0,10)]
    public float dmDownSpeed;
    [TooltipAttribute("弹幕字间距")]
    public float dmCharacterDistance = 0.4f;
    [TooltipAttribute("弹幕字号 =scale/10")]
    public float dmCharacterSize = 10f;
    [TooltipAttribute("礼物生成距离间隔（影响多个礼物的总下落时间）")]
    public float GiftDistance = 1;
    [TooltipAttribute("礼物生成位置（砸礼物用）")]
    public Transform GiftPoint;
    [TooltipAttribute("弹幕存在时间")]
    public int dmLifeTime = 30;
    [TooltipAttribute("礼物存在时间")]
    public int GiftLifeTime = 10;

    //一些开关
    [TooltipAttribute("总开关")]
    public bool Pause = false;
    public bool GiftPause = false;
    public bool DanmakuPause = false;

    private Vector3 dmPos = Vector3.zero;

    //弹幕仓库
    private Dictionary<DanmakuType, List<Danmaku>> danmakuStore = new Dictionary<DanmakuType, List<Danmaku>>();

    private static DanmakuManager instance;
    public static DanmakuManager Instance
    {
        get
        {
            return instance;
        }
    }

    private void Awake()
    { 
        if(instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!Pause)
        {
            if (danmakuStore.ContainsKey(DanmakuType.DM) && danmakuStore[DanmakuType.DM].Count > 0)
            {
                try
                {
                    string danmakuStr = getSingelDanmaku(DanmakuType.DM).content;
                    Debug.Log("生成：" + danmakuStr);
                    var vec = dmStartPos + new Vector3(Random.Range(-1 * dmGenerateLimit, dmGenerateLimit), 0);
                    for (int i = 0; i < danmakuStr.Length; i++)
                    {
                        var dm = DanmakuPool.Instance.getCharacter(danmakuStr[i].ToString());
                        dm.transform.position = vec - new Vector3(i * dmCharacterDistance * dmCharacterSize * 0.1f, 0, 0);
                    }
                }
                catch (NoSuchCharacterException e)
                {
                    Debug.Log(e.error);
                }
            }
            if (danmakuStore.ContainsKey(DanmakuType.GIFT) && danmakuStore[DanmakuType.GIFT].Count > 0)
            {
                Danmaku danmaku = getSingelDanmaku(DanmakuType.GIFT);
                List<GameObject> gifts = DanmakuPool.Instance.getGift(danmaku.num);
                setGift(gifts);
            }
        }
    }

    void setGift(List<GameObject> list)
    {
        for (var i = 0; i < list.Count; i++)
        {
            list[i].transform.position = GiftPoint.position + new Vector3(Random.Range(-3, 3), i * GiftDistance, 0);
            list[i].transform.parent = GiftPoint;
        }
    }

    /// <summary>
    /// 从弹幕列表中获取一条弹幕
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    Danmaku getSingelDanmaku(DanmakuType type)
    {
        var e = danmakuStore[type][0];
        danmakuStore[type].Remove(e);
        return e;
    }
    /// <summary>
    /// 添加弹幕信息
    /// </summary>
    /// <param name="type"></param>
    /// <param name="danmaku"></param>
    public void AddDanmaku(DanmakuType type, Danmaku danmaku)
    {
        if (!Pause)
        {
            if (!danmakuStore.ContainsKey(type))
            {
                danmakuStore.Add(type, new List<Danmaku>());
            }
            switch (type)
            {
                case DanmakuType.DM:
                    if (!DanmakuPause)
                    {
                        danmakuStore[type].Add(danmaku);
                    }
                    break;
                case DanmakuType.GIFT:
                    if (!GiftPause)
                    {
                        danmakuStore[type].Add(danmaku);
                    }
                    break;
            }
        }
    }
}

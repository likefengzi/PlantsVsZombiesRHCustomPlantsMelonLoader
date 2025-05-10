﻿using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SelectCustomPlants.MelonLoader.SelectCustomPlants), "SelectCustomPlants", "1.0.0",
    "likefengzi",
    null)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

namespace SelectCustomPlants.MelonLoader;

public class SelectCustomPlants : MelonMod
{
    /// <summary>
    /// 二创植物Button
    /// </summary>
    public static GameObject? MyShowCustomPlantsButton;

    /// <summary>
    /// 二创植物页面
    /// </summary>
    public static GameObject? MyPageParent;

    /// <summary>
    /// 类似Update
    /// </summary>
    public override void OnUpdate()
    {
        //判断鼠标按下
        if (Input.GetMouseButtonDown(0) && MyShowCustomPlantsButton != null)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            //击中二创植物Button
            if (hit.collider != null && hit.collider.gameObject == MyShowCustomPlantsButton)
            {
                //打开二创植物页面
                OpenCustomPlantCards();
            }
        }

        //设置鼠标特效
        if (MyShowCustomPlantsButton != null)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == MyShowCustomPlantsButton)
            {
                CursorChange.SetClickCursor();
            }
        }
    }

    /// <summary>
    /// 初始化二创植物Button
    /// </summary>
    public static void InitCustomCards()
    {
        //用正常植物Button创建二创植物Button
        MyShowCustomPlantsButton = UnityEngine.Object.Instantiate(
            Resources.Load<GameObject>("ui/prefabs/InGameUI").transform.FindChild("Bottom/SeedLibrary/ShowNormal")
                .gameObject, InGameUI.Instance.SeedBank.transform.parent.FindChild("Bottom/SeedLibrary"));
        //设置位置
        MyShowCustomPlantsButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(580, -230);
        MyShowCustomPlantsButton.GetComponent<RectTransform>().position = new Vector3(
            MyShowCustomPlantsButton.GetComponent<RectTransform>().position.x,
            MyShowCustomPlantsButton.GetComponent<RectTransform>().position.y,
            InGameUI.Instance.SeedBank.transform.parent.FindChild("Bottom/SeedLibrary/ShowNormal").position.z);
        //激活
        MyShowCustomPlantsButton.SetActive(true);

        //摧毁UIButton组件
        if (MyShowCustomPlantsButton.GetComponent<UIButton>() != null)
        {
            UnityEngine.Object.Destroy(MyShowCustomPlantsButton.GetComponent<UIButton>());
            //MyShowCustomPlantsButton.AddComponent<ShowCustomPlantsButton>();
        }

        //修改文字
        for (int i = 0; i < MyShowCustomPlantsButton.transform.childCount; i++)
        {
            MyShowCustomPlantsButton.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().m_text = "二创植物";
            //MyShowCustomPlantsButton.transform.GetChild(i).gameObject.GetComponent<TextMeshProUGUI>().fontSize = 18 - 6;
            MyShowCustomPlantsButton.transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 打开二创植物界面
    /// </summary>
    public static void OpenCustomPlantCards()
    {
        //基础植物和彩色植物界面隐藏
        InGameUI.Instance.SeedBank.transform.parent.FindChild("Bottom/SeedLibrary/Grid/Pages").gameObject
            .SetActive(false);
        InGameUI.Instance.SeedBank.transform.parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards").gameObject
            .SetActive(false);
        //如果二创植物界面已经创建就激活，没有就创建
        if (MyPageParent != null)
        {
            MyPageParent.SetActive(true);
        }
        else
        {
            //使用彩色植物界面创建二创植物界面
            MyPageParent = UnityEngine.Object.Instantiate(
                InGameUI.Instance.SeedBank.transform.parent.FindChild("Bottom/SeedLibrary/Grid/ColorfulCards")
                    .gameObject
                    .gameObject, InGameUI.Instance.SeedBank.transform.parent.FindChild("Bottom/SeedLibrary/Grid"));
            MyPageParent.gameObject.SetActive(true);
            GameObject MyPage = MyPageParent.transform.GetChild(0).gameObject;
            MyPage.gameObject.SetActive(true);
            GameObject MyCard = MyPage.transform.GetChild(0).gameObject;
            MyCard.gameObject.SetActive(false);
            for (int i = 0; i < MyPage.transform.childCount; i++)
            {
                if (i != 0)
                {
                    //销毁Page上的所有Card
                    UnityEngine.Object.Destroy(MyPage.transform.GetChild(i).gameObject);
                }
            }

            //获得自定义植物列表
            List<PlantType> plantTypes = new List<PlantType>();
            foreach (PlantType plantType in CustomCore.CustomPlantTypes)
            {
                //如果不是融合版植物，就加载
                if (!Enum.IsDefined(typeof(PlantType), plantType) &&
                    PlantDataLoader.plantDatas[plantType] != null)
                {
                    plantTypes.Add(plantType);
                }
            }
            //创建卡片
            for (int i = 0; i < plantTypes.Count; i++)
            {
                GameObject TempCard = UnityEngine.Object.Instantiate(MyCard);
                if (TempCard != null)
                {
                    //设置父节点
                    TempCard.transform.SetParent(MyPage.transform);
                    //激活
                    TempCard.SetActive(true);
                    //设置位置
                    TempCard.transform.position = MyCard.transform.position;
                    TempCard.transform.localPosition = MyCard.transform.localPosition;
                    TempCard.transform.localScale = MyCard.transform.localScale;
                    TempCard.transform.localRotation = MyCard.transform.localRotation;
                    //背景图片
                    TempCard.transform.GetChild(0).gameObject.SetActive(false);
                    //卡片
                    CardUI component = TempCard.transform.GetChild(1).GetComponent<CardUI>();
                    component.gameObject.SetActive(true);
                    //修改图片
                    Mouse.Instance.ChangeCardSprite(plantTypes[i], component);
                    //设置数据
                    component.thePlantType = plantTypes[i];
                    component.theSeedType = (int)plantTypes[i];
                    component.theSeedCost = PlantDataLoader.plantDatas[plantTypes[i]].field_Public_Int32_1;
                    component.fullCD = PlantDataLoader.plantDatas[plantTypes[i]].field_Public_Single_2;
                }
            }
        }
    }

    /// <summary>
    /// 隐藏二创植物界面
    /// </summary>
    public static void CloseCustomPlantCards()
    {
        if (MyPageParent != null)
        {
            MyPageParent.SetActive(false);
        }
    }

    /// <summary>
    /// 进入一局游戏，显示二创植物Button
    /// </summary>
    [HarmonyPatch(typeof(Board), nameof(Board.Start))]
    public class ShowCustomPlantCards
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            InitCustomCards();
        }
    }

    /// <summary>
    /// 点击其他Button，隐藏二创植物界面
    /// </summary>
    [HarmonyPatch(typeof(UIButton), nameof(UIButton.OnMouseUpAsButton))]
    public class HideCustomPlantCards
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            CloseCustomPlantCards();
        }
    }
}
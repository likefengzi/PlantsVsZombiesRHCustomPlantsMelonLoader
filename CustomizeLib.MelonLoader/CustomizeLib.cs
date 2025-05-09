using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppTMPro;
using MelonLoader;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

[assembly: MelonInfo(typeof(CustomCore), "PVZRHCustomization", "2.5.1", "likefengzi", null)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

namespace CustomizeLib.MelonLoader;

/// <summary>
/// 自定义植物数据
/// </summary>
public struct CustomPlantData
{
    //植物id
    public int ID { get; set; }

    //游戏内置植物数据
    public PlantDataLoader.PlantData_ PlantData { get; set; }

    //预制体
    public GameObject Prefab { get; set; }

    //预览图
    public GameObject Preview { get; set; }
}

/// <summary>
/// 植物图鉴
/// </summary>
[HarmonyPatch(typeof(AlmanacPlantBank))]
public static class AlmanacMgrPatch
{
    /// <summary>
    /// 从json加载植物信息
    /// </summary>
    /// <param name="__instance"></param>
    /// <returns></returns>
    [HarmonyPatch("InitNameAndInfoFromJson")]
    [HarmonyPrefix]
    public static bool PreInitNameAndInfoFromJson(AlmanacPlantBank __instance)
    {
        //如果自定义植物图鉴信息包含
        if (CustomCore.PlantsAlmanac.ContainsKey((PlantType)__instance.theSeedType))
        {
            //遍历图鉴上的组件
            for (int i = 0; i < __instance.transform.childCount; i++)
            {
                Transform childTransform = __instance.transform.GetChild(i);
                if (childTransform == null)
                    continue;
                //植物姓名
                if (childTransform.name == "Name")
                {
                    childTransform.GetComponent<TextMeshPro>().text =
                        CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item1;
                    childTransform.GetChild(0).GetComponent<TextMeshPro>().text =
                        CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item1;
                }

                //植物信息
                if (childTransform.name == "Info")
                {
                    TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                    info.overflowMode = TextOverflowModes.Page;
                    info.fontSize = 40;
                    info.text = CustomCore.PlantsAlmanac[(PlantType)__instance.theSeedType].Item2;
                    __instance.introduce = info;
                }

                //植物阳光
                if (childTransform.name == "Cost")
                    childTransform.GetComponent<TextMeshPro>().text = "";
            }

            //阻断原始的加载
            return false;
        }

        return true;
    }

    /// <summary>
    /// 图鉴中鼠标按下，用于翻页
    /// </summary>
    /// <param name="__instance"></param>
    /// <returns></returns>
    [HarmonyPatch("OnMouseDown")]
    [HarmonyPrefix]
    public static bool PreOnMouseDown(AlmanacPlantBank __instance)
    {
        //右侧显示
        __instance.introduce =
            __instance.gameObject.transform.FindChild("Info").gameObject.GetComponent<TextMeshPro>();
        //页数
        __instance.pageCount = __instance.introduce.m_pageNumber * 1;
        //下一页
        if (__instance.currentPage <= __instance.introduce.m_pageNumber)
            ++__instance.currentPage;
        else
            __instance.currentPage = 1;
        //翻页
        __instance.introduce.pageToDisplay = __instance.currentPage;

        //阻断原始翻页
        return false;
    }
}

/// <summary>
/// 僵尸图鉴
/// </summary>
[HarmonyPatch(typeof(AlmanacMgrZombie))]
public static class AlmanacMgrZombiePatch
{
    [HarmonyPatch("InitNameAndInfoFromJson")]
    [HarmonyPrefix]
    public static bool PreInitNameAndInfoFromJson(AlmanacMgrZombie __instance)
    {
        if (CustomCore.ZombiesAlmanac.ContainsKey(__instance.theZombieType))
        {
            for (int i = 0; i < __instance.transform.childCount; i++)
            {
                Transform childTransform = __instance.transform.GetChild(i);
                if (childTransform == null)
                    continue;
                if (childTransform.name == "Name")
                {
                    childTransform.GetComponent<TextMeshPro>().text =
                        CustomCore.ZombiesAlmanac[__instance.theZombieType].Item1;
                    childTransform.GetChild(0).GetComponent<TextMeshPro>().text =
                        CustomCore.ZombiesAlmanac[__instance.theZombieType].Item1;
                }

                if (childTransform.name == "Info")
                {
                    TextMeshPro info = childTransform.GetComponent<TextMeshPro>();
                    info.overflowMode = TextOverflowModes.Page;
                    info.fontSize = 40;
                    info.text = CustomCore.ZombiesAlmanac[__instance.theZombieType].Item2;
                    __instance.introduce = info;
                }

                if (childTransform.name == "Cost")
                    childTransform.GetComponent<TextMeshPro>().text = "";
            }

            return false;
        }

        return true;
    }
}

/// <summary>
/// 创造并放置植物
/// </summary>
[HarmonyPatch(typeof(CreatePlant), "SetPlant")]
public static class CreatePlantPatch
{
    public static void Postfix(ref GameObject __result)
    {
        //如果创造的植物不为空，自定义植物列表包含该植物
        if (__result is not null && __result.TryGetComponent<Plant>(out var plant) &&
            CustomCore.CustomPlantTypes.Contains(plant.thePlantType))
        {
            //？应该是判断植物类型并实现对应效果
            TypeMgr.GetPlantTag(plant);
        }
    }
}

/// <summary>
/// 拓展功能
/// </summary>
public static class Extensions
{
    /// <summary>
    /// 禁止拆分，父本母本归零
    /// </summary>
    /// <param name="plant"></param>
    public static void DisableDisMix(this Plant plant)
    {
        plant.firstParent = PlantType.Nothing;
        plant.secondParent = PlantType.Nothing;
    }

    /// <summary>
    /// 加载资源
    /// </summary>
    /// <param name="ab"></param>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static T GetAsset<T>(this AssetBundle ab, string name) where T : UnityEngine.Object
    {
        foreach (var ase in ab.LoadAllAssetsAsync().allAssets)
        {
            if (ase.TryCast<T>()?.name == name)
            {
                return ase.Cast<T>();
            }
        }

        throw new ArgumentException($"Could not find {name} from {ab.name}");
    }

    /// <summary>
    /// 获得全部血量
    /// </summary>
    /// <param name="zombie"></param>
    /// <returns></returns>
    public static int GetTotalHealth(this Zombie zombie)
    {
        return (int)zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth;
    }

    /// <summary>
    /// 判断物体是否存在至少一个
    /// </summary>
    /// <param name="board"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool ObjectExist<T>(this Board board)
    {
        return board.GameObject().transform.GetComponentsInChildren<T>().Length > 0;
    }
}

/// <summary>
/// 
/// </summary>
[HarmonyPatch(typeof(GameAPP))]
public static class GameAPPPatch
{
    /// <summary>
    /// 加载资源，如果后续出现列表过短，可以在这里重写
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch("LoadResources")]
    public static void LoadResources()
    {
        //遍历自定义植物列表
        foreach (var plant in CustomCore.CustomPlants)
        {
            //都可以重写加长
            //植物预制体列表
            GameAPP.resourcesManager.plantPrefabs[plant.Key] = plant.Value.Prefab;
            GameAPP.resourcesManager.plantPrefabs[plant.Key].tag = "Plant";
            if (!GameAPP.resourcesManager.allPlants.Contains(plant.Key))
            {
                GameAPP.resourcesManager.allPlants.Add(plant.Key);
            }

            //植物数据列表
            PlantDataLoader.plantData[(int)plant.Key] = plant.Value.PlantData;
            PlantDataLoader.plantDatas.Add(plant.Key, plant.Value.PlantData);
            //植物预览图列表
            GameAPP.resourcesManager.plantPreviews[plant.Key] = plant.Value.Preview;
            GameAPP.resourcesManager.plantPreviews[plant.Key].tag = "Preview";
        }

        //融合配方
        Il2CppSystem.Array array = MixData.data.Cast<Il2CppSystem.Array>();
        foreach (var f in CustomCore.CustomFusions)
        {
            array.SetValue(f.Item1, f.Item2, f.Item3);
        }

        //自定义僵尸列表
        foreach (var z in CustomCore.CustomZombies)
        {
            if (!GameAPP.resourcesManager.allZombieTypes.Contains(z.Key))
            {
                GameAPP.resourcesManager.allZombieTypes.Add(z.Key);
            }

            GameAPP.resourcesManager.zombiePrefabs[z.Key] = z.Value.Item1;
            GameAPP.resourcesManager.zombiePrefabs[z.Key].tag = "Zombie";
        }

        //自定义子弹列表
        foreach (var bullet in CustomCore.CustomBullets)
        {
            GameAPP.resourcesManager.bulletPrefabs[bullet.Key] = bullet.Value;
            if (!GameAPP.resourcesManager.allBullets.Contains(bullet.Key))
            {
                GameAPP.resourcesManager.allBullets.Add(bullet.Key);
            }
        }

        //自定义特效列表
        foreach (var par in CustomCore.CustomParticles)
        {
            GameAPP.particlePrefab[(int)par.Key] = par.Value;
            GameAPP.resourcesManager.particlePrefabs[par.Key] = par.Value;
            if (!GameAPP.resourcesManager.allParticles.Contains(par.Key))
            {
                GameAPP.resourcesManager.allParticles.Add(par.Key);
            }
        }

        //自定义图片列表
        foreach (var spr in CustomCore.CustomSprites)
        {
            GameAPP.spritePrefab[spr.Key] = spr.Value;
        }
    }
}

/// <summary>
/// 金币
/// </summary>
[HarmonyPatch(typeof(Money))]
public static class MoneyPatch
{
    /// <summary>
    /// 金币大招
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="plant"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("ReinforcePlant")]
    public static bool PreReinforcePlant(Money __instance, ref Plant plant)
    {
        //如果自定义超级技能列表包含该植物
        if (CustomCore.SuperSkills.ContainsKey(plant.thePlantType))
        {
            //大招金币数量
            var cost = CustomCore.SuperSkills[plant.thePlantType].Item1(plant);
            //金币不足，弹出提示
            if (Board.Instance.theMoney < cost)
            {
                InGameText.Instance.ShowText($"需要{cost}金币", 5);
                //阻断大招
                return false;
            }

            //大招
            if (plant.SuperSkill())
            {
                CustomCore.SuperSkills[plant.thePlantType].Item2(plant);
                //大招动画
                plant.AnimSuperShoot();
                //标记使用过大招
                __instance.UsedEvent(plant.thePlantColumn, plant.thePlantRow, cost);
                //触发连锁大招
                __instance.OtherSuperSkill(plant);
            }

            //阻断原版大招
            return false;
        }

        return true;
    }
}

/// <summary>
/// 鼠标
/// </summary>
[HarmonyPatch(typeof(Mouse))]
public static class MousePatch
{
    /// <summary>
    /// 获得鼠标上的植物
    /// </summary>
    /// <param name="__result"></param>
    [HarmonyPostfix]
    [HarmonyPatch("GetPlantsOnMouse")]
    public static void PostGetPlantsOnMouse(ref Il2CppSystem.Collections.Generic.List<Plant> __result)
    {
        for (int i = __result.Count - 1; i >= 0; i--)
        {
            foreach (Plant plant in __result.GetRange(i, 1))
            {
                //判断植物存在并且为大坚果类
                if (plant != null && TypeMgr.BigNut(plant.thePlantType))
                {
                    __result.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// 左键点击
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch("LeftClickWithNothing")]
    public static void PostLeftClickWithNothing()
    {
        // 执行射线检测获取所有碰撞物体
        RaycastHit2D[] raycastHits = Physics2D.RaycastAll(
            Camera.main.ScreenToWorldPoint(Input.mousePosition),
            Vector2.zero
        );

        // 创建列表存储所有碰撞的游戏对象
        List<GameObject> hitGameObjects = new List<GameObject>();
        foreach (RaycastHit2D raycastHit in raycastHits)
        {
            if (raycastHit.collider != null)
            {
                hitGameObjects.Add(raycastHit.collider.gameObject);
            }
        }

        // 遍历所有碰撞的游戏对象
        foreach (GameObject gameObject in hitGameObjects)
        {
            //如果植物存在并且自定义单击列表中存在
            if (gameObject.TryGetComponent<Plant>(out var plant) &&
                CustomCore.CustomPlantClicks.ContainsKey(plant.thePlantType))
            {
                //触发单击
                CustomCore.CustomPlantClicks[plant.thePlantType](plant);
                return;
            }
        }
    }
}

/// <summary>
/// 植物
/// </summary>
[HarmonyPatch(typeof(Plant))]
public static class PlantPatch
{
    /// <summary>
    /// 对植物使用物品
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="type"></param>
    /// <param name="bucket"></param>
    [HarmonyPostfix]
    [HarmonyPatch("UseItem")]
    public static void PostUseItem(Plant __instance, ref BucketType type, ref Bucket bucket)
    {
        if (CustomCore.CustomUseItems.ContainsKey((__instance.thePlantType, type)))
        {
            //触发使用物品
            CustomCore.CustomUseItems[(__instance.thePlantType, type)](__instance);
            //摧毁物品
            UnityEngine.Object.Destroy(bucket.gameObject);
        }
    }
}

/// <summary>
/// 旅行词条
/// </summary>
[HarmonyPatch(typeof(TravelBuff))]
public static class TravelBuffPatch
{
    /// <summary>
    /// 改变图片
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch("ChangeSprite")]
    public static void PreChangeSprite(TravelBuff __instance)
    {
        //高级词条
        if (__instance.theBuffType == 1 && CustomCore.CustomAdvancedBuffs.ContainsKey(__instance.theBuffNumber))
        {
            __instance.thePlantType = CustomCore.CustomAdvancedBuffs[__instance.theBuffNumber].Item1;
        }

        //究极词条
        if (__instance.theBuffType == 2 && CustomCore.CustomUltimateBuffs.ContainsKey(__instance.theBuffNumber))
        {
            __instance.thePlantType = CustomCore.CustomUltimateBuffs[__instance.theBuffNumber].Item1;
        }
    }
}

/*
[HarmonyPatch(typeof(TravelBuffMenu))]
public static class TravelMenuMgrPatch
{
    [HarmonyPostfix]
    [HarmonyPatch("SetText")]
    public static void PostSetText(TravelBuffMenu __instance)
    {
        for (int i = 0; i < 3; i++)
        {
            int type = __instance.options[i].optionType;
            int number = __instance.options[i].optionNumber;
            if (type is 1 && CustomCore.CustomAdvancedBuffs.ContainsKey(number) && CustomCore.CustomAdvancedBuffs[number].Item5 is not null)
            {
                __instance.textMesh[i].text = $"<color={CustomCore.CustomAdvancedBuffs[number].Item5}>{__instance.textMesh[i].text}</color>";
            }
            if (type is 2 && CustomCore.CustomUltimateBuffs.ContainsKey(number) && CustomCore.CustomUltimateBuffs[number].Item4 is not null)
            {
                __instance.textMesh[i].text = $"<color={CustomCore.CustomUltimateBuffs[number].Item4}>{__instance.textMesh[i].text}</color>";
            }
        }
    }
}
*/

/// <summary>
/// 旅行管理器
/// </summary>
[HarmonyPatch(typeof(TravelMgr))]
public static class TravelMgrPatch
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch("Awake")]
    [HarmonyPrefix]
    public static void PostAwake(TravelMgr __instance)
    {
        //自定义高级词条
        if (CustomCore.CustomAdvancedBuffs.Count > 0)
        {
            bool[] newAdv = new bool[__instance.advancedUpgrades.Count + CustomCore.CustomAdvancedBuffs.Count];
            int[] newAdvUnlock = new int[__instance.advancedUnlockRound.Count + CustomCore.CustomAdvancedBuffs.Count];
            Array.Copy(__instance.advancedUpgrades, newAdv, __instance.advancedUpgrades.Length);
            Array.Copy(__instance.advancedUnlockRound, newAdvUnlock, __instance.advancedUnlockRound.Length);
            __instance.advancedUpgrades = newAdv;
            __instance.advancedUnlockRound = newAdvUnlock;
        }

        //自定义究极词条
        if (CustomCore.CustomUltimateBuffs.Count > 0)
        {
            bool[] newUlti = new bool[__instance.ultimateUpgrades.Count + CustomCore.CustomUltimateBuffs.Count];
            Array.Copy(__instance.ultimateUpgrades, newUlti, __instance.ultimateUpgrades.Length);
            __instance.ultimateUpgrades = newUlti;
        }

        //自定义负面词条
        if (CustomCore.CustomDebuffs.Count > 0)
        {
            bool[] newdeb = new bool[__instance.debuff.Count + CustomCore.CustomDebuffs.Count];
            Array.Copy(__instance.debuff, newdeb, __instance.debuff.Length);
            __instance.debuff = newdeb;
        }
    }

    /// <summary>
    /// 高级词条池子
    /// </summary>
    /// <param name="__result"></param>
    [HarmonyPatch("GetAdvancedBuffPool")]
    [HarmonyPostfix]
    public static void PostGetAdvancedBuffPool(ref Il2CppSystem.Collections.Generic.List<int> __result)
    {
        for (int i = __result.Count - 1; i >= 0; i--)
        {
            foreach (int num in __result.GetRange(i, 1))
            {
                //如果Unlock条件不成立，移除
                if (CustomCore.CustomAdvancedBuffs.ContainsKey(num) &&
                    !CustomCore.CustomAdvancedBuffs[num].Item3())
                {
                    __result.Remove(num);
                }
            }
        }
    }
}

/// <summary>
/// 旅行商店
/// </summary>
[HarmonyPatch(typeof(TravelStore))]
public static class TravelStorePatch
{
    /// <summary>
    /// 刷新词条
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch("RefreshBuff")]
    [HarmonyPostfix]
    public static void PostRefreshBuff(TravelStore __instance)
    {
        foreach (var travelBuff in __instance.gameObject.GetComponentsInChildren<TravelBuff>())
        {
            //高级词条
            if (travelBuff.theBuffType is (int)BuffType.AdvancedBuff &&
                CustomCore.CustomAdvancedBuffs.ContainsKey(travelBuff.theBuffNumber))
            {
                travelBuff.cost = CustomCore.CustomAdvancedBuffs[travelBuff.theBuffNumber].Item4;
                travelBuff.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text =
                    $"花费{CustomCore.CustomAdvancedBuffs[travelBuff.theBuffNumber].Item4}";
            }

            //究极词条
            if (travelBuff.theBuffType is (int)BuffType.UltimateBuff &&
                CustomCore.CustomUltimateBuffs.ContainsKey(travelBuff.theBuffNumber))
            {
                travelBuff.cost = CustomCore.CustomUltimateBuffs[travelBuff.theBuffNumber].Item3;
                travelBuff.transform.GetChild(1).gameObject.GetComponent<TextMeshProUGUI>().text =
                    $"花费{CustomCore.CustomUltimateBuffs[travelBuff.theBuffNumber].Item4}";
            }
        }
    }
}

/// <summary>
/// 类型管理
/// </summary>
[HarmonyPatch(typeof(TypeMgr))]
public static class TypeMgrPatch
{
    /// <summary>
    /// 大坚果，滚动
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("BigNut")]
    public static bool PreBigNut(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.BigNut.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 大僵尸
    /// </summary>
    /// <param name="theZombieType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("BigZombie")]
    public static bool PreBigZombie(ref ZombieType theZombieType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.BigZombie.Contains(theZombieType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 双格植物
    /// </summary>
    /// <param name="thePlantType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("DoubleBoxPlants")]
    public static bool PreDoubleBoxPlants(ref PlantType thePlantType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.DoubleBoxPlants.Contains(thePlantType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 精英僵尸，领袖
    /// </summary>
    /// <param name="theZombieType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("EliteZombie")]
    public static bool PreEliteZombie(ref ZombieType theZombieType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.EliteZombie.Contains(theZombieType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 飞行植物，浮空
    /// </summary>
    /// <param name="thePlantType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("FlyingPlants")]
    public static bool PreFlyingPlants(ref PlantType thePlantType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.FlyingPlants.Contains(thePlantType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获得植物标签
    /// </summary>
    /// <param name="plant"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("GetPlantTag")]
    public static bool PreGetPlantTag(ref Plant plant)
    {
        if (CustomCore.CustomPlantTypes.Contains(plant.thePlantType))
        {
            plant.plantTag = new()
            {
                //冰植物，免疫冰冻
                icePlant = TypeMgr.IsIcePlant(plant.thePlantType),
                //蒺藜植物，反伤
                caltropPlant = TypeMgr.IsCaltrop(plant.thePlantType),
                //双格植物
                doubleBoxPlant = TypeMgr.DoubleBoxPlants(plant.thePlantType),
                //火植物，免疫冰冻
                firePlant = TypeMgr.IsFirePlant(plant.thePlantType),
                //飞行植物，浮空
                flyingPlant = TypeMgr.FlyingPlants(plant.thePlantType),
                //灯笼植物，范围光照，温暖
                lanternPlant = TypeMgr.IsPlantern(plant.thePlantType),
                //小灯笼，范围光照，温暖
                smallLanternPlant = TypeMgr.IsSmallRangeLantern(plant.thePlantType),
                //磁力植物，磁力链接
                magnetPlant = TypeMgr.IsMagnetPlants(plant.thePlantType),
                //坚果植物
                nutPlant = TypeMgr.IsNut(plant.thePlantType),
                //高坚果植物
                tallNutPlant = TypeMgr.IsTallNut(plant.thePlantType),
                //土豆植物
                potatoPlant = TypeMgr.IsPotatoMine(plant.thePlantType),
                //花盆植物
                potPlant = TypeMgr.IsPot(plant.thePlantType),
                //小喷菇，三个一个，没有低矮效果
                puffPlant = TypeMgr.IsPuff(plant.thePlantType),
                //南瓜植物
                pumpkinPlant = TypeMgr.IsPumpkin(plant.thePlantType),
                //超时空植物
                spickRockPlant = TypeMgr.IsSpickRock(plant.thePlantType),
                //缠绕海草
                tanglekelpPlant = TypeMgr.IsTangkelp(plant.thePlantType),
                //水生植物
                waterPlant = TypeMgr.IsWaterPlant(plant.thePlantType)
            };

            return false;
        }

        return true;
    }

    /// <summary>
    /// 飞行僵尸
    /// </summary>
    /// <param name="theZombieType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsAirZombie")]
    public static bool PreIsAirZombie(ref ZombieType theZombieType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsAirZombie.Contains(theZombieType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 蒺藜植物，反伤
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsCaltrop")]
    public static bool PreIsCaltrop(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsCaltrop.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 火植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsFirePlant")]
    public static bool PreIsFirePlant(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsFirePlant.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 冰植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsIcePlant")]
    public static bool PreIsIcePlant(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsIcePlant.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 磁力植物
    /// </summary>
    /// <param name="thePlantType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsMagnetPlants")]
    public static bool PreIsMagnetPlants(ref PlantType thePlantType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsMagnetPlants.Contains(thePlantType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 坚果植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsNut")]
    public static bool PreIsNut(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsNut.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 灯笼植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsPlantern")]
    public static bool PreIsPlantern(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsPlantern.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 花盆植物
    /// </summary>
    /// <param name="thePlantType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsPot")]
    public static bool PreIsPot(ref PlantType thePlantType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsPot.Contains(thePlantType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("IsPotatoMine")]
    public static bool PreIsPotatoMine(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsPotatoMine.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 小喷菇，三个一个，没有低矮
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsPuff")]
    public static bool PreIsPuff(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsPuff.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 南瓜植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsPumpkin")]
    public static bool PreIsPumpkin(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsPumpkin.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 小灯笼植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsSmallRangeLantern")]
    public static bool PreIsSmallRangeLantern(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsSmallRangeLantern.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 特殊植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsSpecialPlant")]
    public static bool PreIsSpecialPlant(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsSpecialPlant.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 超时空植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsSpickRock")]
    public static bool PreIsSpickRock(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsSpickRock.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 高坚果植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsTallNut")]
    public static bool PreIsTallNut(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsTallNut.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 缠绕海草
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsTangkelp")]
    public static bool PreIsTangkelp(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsTangkelp.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 水生植物
    /// </summary>
    /// <param name="theSeedType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("IsWaterPlant")]
    public static bool PreIsWaterPlant(ref PlantType theSeedType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.IsWaterPlant.Contains(theSeedType))
        {
            __result = true;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 伞植物
    /// </summary>
    /// <param name="thePlantType"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPrefix]
    [HarmonyPatch("UmbrellaPlants")]
    public static bool PreUmbrellaPlants(ref PlantType thePlantType, ref bool __result)
    {
        if (CustomCore.TypeMgrExtra.UmbrellaPlants.Contains(thePlantType))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

public class CustomCore : MelonMod
{
    public static class TypeMgrExtra
    {
        public static List<PlantType> BigNut { get; set; } = new() { };
        public static List<ZombieType> BigZombie { get; set; } = new() { };
        public static List<PlantType> DoubleBoxPlants { get; set; } = new() { };
        public static List<ZombieType> EliteZombie { get; set; } = new() { };
        public static List<PlantType> FlyingPlants { get; set; } = new() { };
        public static List<ZombieType> IsAirZombie { get; set; } = new() { };
        public static List<PlantType> IsCaltrop { get; set; } = new() { };
        public static List<PlantType> IsCustomPlant { get; set; } = new() { };
        public static List<PlantType> IsFirePlant { get; set; } = new() { };
        public static List<PlantType> IsIcePlant { get; set; } = new() { };
        public static List<PlantType> IsMagnetPlants { get; set; } = new() { };
        public static List<PlantType> IsNut { get; set; } = new() { };
        public static List<PlantType> IsPlantern { get; set; } = new() { };
        public static List<PlantType> IsPot { get; set; } = new() { };
        public static List<PlantType> IsPotatoMine { get; set; } = new() { };
        public static List<PlantType> IsPuff { get; set; } = new() { };
        public static List<PlantType> IsPumpkin { get; set; } = new() { };
        public static List<PlantType> IsSmallRangeLantern { get; set; } = new() { };
        public static List<PlantType> IsSpecialPlant { get; set; } = new() { };
        public static List<PlantType> IsSpickRock { get; set; } = new() { };
        public static List<PlantType> IsTallNut { get; set; } = new() { };
        public static List<PlantType> IsTangkelp { get; set; } = new() { };
        public static List<PlantType> IsWaterPlant { get; set; } = new() { };
        public static List<ZombieType> NotRandomBungiZombie { get; set; } = new() { };
        public static List<ZombieType> NotRandomZombie { get; set; } = new() { };
        public static List<ZombieType> UltimateZombie { get; set; } = new() { };
        public static List<PlantType> UmbrellaPlants { get; set; } = new() { };
        public static List<ZombieType> UselessHypnoZombie { get; set; } = new() { };
        public static List<ZombieType> WaterZombie { get; set; } = new() { };
    }

    /// <summary>
    /// 添加融合配方
    /// </summary>
    /// <param name="target"></param>
    /// <param name="item1"></param>
    /// <param name="item2"></param>
    public static void AddFusion(int target, int item1, int item2)
    {
        CustomFusions.Add((target, item1, item2));
    }

    /// <summary>
    /// 添加植物图鉴描述
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    public static void AddPlantAlmanacStrings(int id, string name, string description)
    {
        PlantsAlmanac.Add((PlantType)id, (name, description));
    }

    /// <summary>
    /// 添加僵尸图鉴描述
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <param name="description"></param>
    public static void AddZombieAlmanacStrings(int id, string name, string description)
    {
        ZombiesAlmanac.Add((ZombieType)id, (name, description));
    }

    /// <summary>
    /// 获得资源文件
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static AssetBundle GetAssetBundle(Assembly assembly, string name)
    {
        try
        {
            using Stream stream = assembly.GetManifestResourceStream(assembly.FullName!.Split(",")[0] + "." + name) ??
                                  assembly.GetManifestResourceStream(name)!;
            using MemoryStream stream1 = new();
            stream.CopyTo(stream1);
            var ab = AssetBundle.LoadFromMemory(stream1.ToArray());
            ArgumentNullException.ThrowIfNull(ab);
            MelonLogger.Msg($"Successfully load AssetBundle {name}.");
            return ab;
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Failed to load {name} \n{e}");
        }
    }

    /// <summary>
    /// 注册自定义词条
    /// </summary>
    /// <param name="text"></param>
    /// <param name="buffType"></param>
    /// <param name="canUnlock"></param>
    /// <param name="cost"></param>
    /// <param name="color"></param>
    /// <param name="plantType"></param>
    /// <returns></returns>
    public static int RegisterCustomBuff(string text, BuffType buffType, Func<bool> canUnlock, int cost,
        string? color = null, PlantType plantType = PlantType.Nothing)
    {
        switch (buffType)
        {
            //高级词条
            case BuffType.AdvancedBuff:
            {
                int i = TravelMgr.advancedBuffs.Count;
                CustomAdvancedBuffs.Add(i, (plantType, text, canUnlock, cost, color));
                TravelMgr.advancedBuffs.Add(i, text);
                return i;
            }
            //究极词条
            case BuffType.UltimateBuff:
            {
                int i = TravelMgr.ultimateBuffs.Count;
                CustomUltimateBuffs.Add(i, (plantType, text, cost, color));
                TravelMgr.ultimateBuffs.Add(i, text);
                return i;
            }
            //负面词条
            case BuffType.Debuff:
            {
                int i = TravelMgr.debuffs.Count;
                CustomDebuffs.Add(i, text);
                TravelMgr.debuffs.Add(i, text);
                return i;
            }
            default:
                return -1;
        }
    }

    /// <summary>
    /// 注册自定义子弹
    /// </summary>
    /// <param name="id"></param>
    /// <param name="bulletPrefab"></param>
    /// <typeparam name="TBullet"></typeparam>
    public static void RegisterCustomBullet<TBullet>(BulletType id, GameObject bulletPrefab) where TBullet : Bullet
    {
        if (!CustomBullets.ContainsKey(id))
        {
            //子弹预制体上挂载子弹脚本
            bulletPrefab.AddComponent<TBullet>().theBulletType = id;
            CustomBullets.Add(id, bulletPrefab);
        }
    }

    /// <summary>
    /// 注册自定义子弹
    /// </summary>
    /// <param name="id"></param>
    /// <param name="bulletPrefab"></param>
    /// <typeparam name="TBase"></typeparam>
    /// <typeparam name="TBullet"></typeparam>
    public static void RegisterCustomBullet<TBase, TBullet>(BulletType id, GameObject bulletPrefab)
        where TBase : Bullet where TBullet : MonoBehaviour
    {
        if (!CustomBullets.ContainsKey(id))
        {
            //子弹预制体上挂载子弹脚本
            bulletPrefab.AddComponent<TBase>().theBulletType = id;
            bulletPrefab.AddComponent<TBullet>();
            CustomBullets.Add(id, bulletPrefab);
        }
    }

    /// <summary>
    /// 注册自定义特效
    /// </summary>
    /// <param name="id"></param>
    /// <param name="particle"></param>
    public static void RegisterCustomParticle(ParticleType id, GameObject particle)
    {
        CustomParticles.Add(id, particle);
    }

    /// <summary>
    /// 注册自定义植物
    /// </summary>
    /// <param name="id"></param>
    /// <param name="prefab"></param>
    /// <param name="preview"></param>
    /// <param name="fusions"></param>
    /// <param name="attackInterval"></param>
    /// <param name="produceInterval"></param>
    /// <param name="attackDamage"></param>
    /// <param name="maxHealth"></param>
    /// <param name="cd"></param>
    /// <param name="sun"></param>
    /// <typeparam name="TBase"></typeparam>
    /// <typeparam name="TClass"></typeparam>
    public static void RegisterCustomPlant<TBase, TClass>([NotNull] int id, [NotNull] GameObject prefab,
        [NotNull] GameObject preview,
        List<(int, int)> fusions, float attackInterval, float produceInterval, int attackDamage, int maxHealth,
        float cd, int sun)
        where TBase : Plant where TClass : MonoBehaviour
    {
        //植物预制体挂载植物脚本
        prefab.AddComponent<TBase>().thePlantType = (PlantType)id;
        prefab.AddComponent<TClass>();
        //植物id不重复才进行注册
        if (!CustomPlantTypes.Contains((PlantType)id))
        {
            CustomPlantTypes.Add((PlantType)id);
            CustomPlants.Add((PlantType)id, new CustomPlantData()
            {
                ID = id,
                Prefab = prefab,
                Preview = preview,
                PlantData = new()
                {
                    attackDamage = attackDamage,
                    field_Public_PlantType_0 = (PlantType)id,
                    //攻击间隔
                    field_Public_Single_0 = attackInterval,
                    //生产间隔
                    field_Public_Single_1 = produceInterval,
                    //最大HP
                    field_Public_Int32_0 = maxHealth,
                    //种植冷却
                    field_Public_Single_2 = cd,
                    //花费阳光
                    field_Public_Int32_1 = sun
                }
            });
            foreach (var f in fusions)
            {
                //添加融合配方
                AddFusion(id, f.Item1, f.Item2);
            }
        }
        else
        {
            MelonLogger.Msg($"Duplicate Plant ID: {id}");
        }
    }

    /// <summary>
    /// 注册自定义植物
    /// </summary>
    /// <param name="id"></param>
    /// <param name="prefab"></param>
    /// <param name="preview"></param>
    /// <param name="fusions"></param>
    /// <param name="attackInterval"></param>
    /// <param name="produceInterval"></param>
    /// <param name="attackDamage"></param>
    /// <param name="maxHealth"></param>
    /// <param name="cd"></param>
    /// <param name="sun"></param>
    /// <typeparam name="TBase"></typeparam>
    public static void RegisterCustomPlant<TBase>([NotNull] int id, [NotNull] GameObject prefab,
        [NotNull] GameObject preview,
        List<(int, int)> fusions, float attackInterval, float produceInterval, int attackDamage, int maxHealth,
        float cd, int sun)
        where TBase : Plant
    {
        //植物预制体挂载植物脚本
        prefab.AddComponent<TBase>().thePlantType = (PlantType)id;
        if (!CustomPlantTypes.Contains((PlantType)id))
        {
            //植物id不重复才进行注册
            CustomPlantTypes.Add((PlantType)id);
            CustomPlants.Add((PlantType)id, new CustomPlantData()
            {
                ID = id,
                Prefab = prefab,
                Preview = preview,
                PlantData = new()
                {
                    attackDamage = attackDamage,
                    //攻击间隔
                    field_Public_Single_0 = attackInterval,
                    //生产间隔
                    field_Public_Single_1 = produceInterval,
                    //最大HP
                    field_Public_Int32_0 = maxHealth,
                    //种植冷却
                    field_Public_Single_2 = cd,
                    //花费阳光
                    field_Public_Int32_1 = sun
                }
            });
            foreach (var f in fusions)
            {
                AddFusion(id, f.Item1, f.Item2);
            }
        }
        else
        {
            //添加融合配方
            MelonLogger.Msg($"Duplicate Plant ID: {id}");
        }
    }

    /// <summary>
    /// 自定义植物点击事件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="action"></param>
    public static void RegisterCustomPlantClickEvent([NotNull] int id, [NotNull] Action<Plant> action)
    {
        CustomPlantClicks.Add((PlantType)id, action);
    }

    /// <summary>
    /// 自定义图片
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sprite"></param>
    public static void RegisterCustomSprite(int id, Sprite sprite)
    {
        CustomSprites.Add(id, sprite);
    }

    /// <summary>
    /// 自定义对植物使用物品事件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="bucketType"></param>
    /// <param name="callback"></param>
    public static void RegisterCustomUseItemOnPlantEvent([NotNull] PlantType id, [NotNull] BucketType bucketType,
        [NotNull] Action<Plant> callback)
    {
        CustomUseItems.Add((id, bucketType), callback);
    }

    /// <summary>
    /// 自定义对植物使用物品事件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="bucketType"></param>
    /// <param name="newPlant"></param>
    public static void RegisterCustomUseItemOnPlantEvent([NotNull] PlantType id, [NotNull] BucketType bucketType,
        [NotNull] PlantType newPlant)
    {
        CustomUseItems.Add((id, bucketType), (p) =>
        {
            p.Die();
            CreatePlant.Instance.SetPlant(p.thePlantColumn, p.thePlantRow, newPlant);
        });
    }

    /// <summary>
    /// 自定义僵尸
    /// </summary>
    /// <param name="id"></param>
    /// <param name="zombie"></param>
    /// <param name="spriteId"></param>
    /// <param name="theAttackDamage"></param>
    /// <param name="theMaxHealth"></param>
    /// <param name="theFirstArmorMaxHealth"></param>
    /// <param name="theSecondArmorMaxHealth"></param>
    /// <typeparam name="TBase"></typeparam>
    /// <typeparam name="TClass"></typeparam>
    public static void RegisterCustomZombie<TBase, TClass>(ZombieType id, GameObject zombie, int spriteId,
        int theAttackDamage, int theMaxHealth, int theFirstArmorMaxHealth, int theSecondArmorMaxHealth)
        where TBase : Zombie where TClass : MonoBehaviour
    {
        //僵尸预制体挂载僵尸脚本
        zombie.AddComponent<TBase>().theZombieType = id;
        zombie.AddComponent<TClass>();

        ZombieData.zombieData[(int)id] = new()
        {
            theAttackDamage = theAttackDamage,
            theFirstArmorMaxHealth = theFirstArmorMaxHealth,
            theMaxHealth = theMaxHealth,
            theSecondArmorMaxHealth = theSecondArmorMaxHealth
        };
        CustomZombieTypes.Add(id);
        CustomZombies.Add(id, (zombie, spriteId));
    }

    /// <summary>
    /// 自定义大招
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cost"></param>
    /// <param name="skill"></param>
    public static void RegisterSuperSkill([NotNull] int id, [NotNull] Func<Plant, int> cost,
        [NotNull] Action<Plant> skill)
    {
        SuperSkills.Add((PlantType)id, (cost, skill));
    }

    /// <summary>
    /// 自定义高级词条列表
    /// </summary>
    public static Dictionary<int, (PlantType, string, Func<bool>, int, string?)> CustomAdvancedBuffs { get; set; } =
        new() { };

    /// <summary>
    /// 自定义子弹列表
    /// </summary>
    public static Dictionary<BulletType, GameObject> CustomBullets { get; set; } = new() { };

    /// <summary>
    /// 自定义负面词条列表
    /// </summary>
    public static Dictionary<int, string> CustomDebuffs { get; set; } = new() { };

    /// <summary>
    /// 自定义融合配方列表
    /// </summary>
    public static List<(int, int, int)> CustomFusions { get; set; } = new() { };

    /// <summary>
    /// 自定义特效列表
    /// </summary>
    public static Dictionary<ParticleType, GameObject> CustomParticles { get; set; } = new() { };

    /// <summary>
    /// 自定义植物点击事件列表
    /// </summary>
    public static Dictionary<PlantType, Action<Plant>> CustomPlantClicks { get; set; } = new() { };

    /// <summary>
    /// 自定义植物列表
    /// </summary>
    public static Dictionary<PlantType, CustomPlantData> CustomPlants { get; set; } = new() { };

    /// <summary>
    /// 自定义植物类型列表
    /// </summary>
    public static List<PlantType> CustomPlantTypes { get; set; } = new() { };

    /// <summary>
    /// 自定义图片列表
    /// </summary>
    public static Dictionary<int, Sprite> CustomSprites { get; set; } = new() { };

    /// <summary>
    /// 自定义究极词条列表
    /// </summary>
    public static Dictionary<int, (PlantType, string, int, string?)> CustomUltimateBuffs { get; set; } = new() { };

    /// <summary>
    /// 自定义使用物品列表
    /// </summary>
    public static Dictionary<(PlantType, BucketType), Action<Plant>> CustomUseItems { get; set; } = new() { };

    /// <summary>
    /// 自定义僵尸列表
    /// </summary>
    public static Dictionary<ZombieType, (GameObject, int)> CustomZombies { get; set; } = new() { };

    /// <summary>
    /// 自定义僵尸类型
    /// </summary>
    public static List<ZombieType> CustomZombieTypes { get; set; } = new() { };

    /// <summary>
    /// Instance
    /// </summary>
    public static Lazy<CustomCore> Instance { get; set; } = new();

    /// <summary>
    /// 自定义植物图鉴列表
    /// </summary>
    public static Dictionary<PlantType, (string, string)> PlantsAlmanac { get; set; } = new() { };

    /// <summary>
    /// 自定义大招列表
    /// </summary>
    public static Dictionary<PlantType, (Func<Plant, int>, Action<Plant>)> SuperSkills { get; set; } = new() { };

    /// <summary>
    /// 自定义僵尸图鉴列表
    /// </summary>
    public static Dictionary<ZombieType, (string, string)> ZombiesAlmanac { get; set; } = new() { };
}
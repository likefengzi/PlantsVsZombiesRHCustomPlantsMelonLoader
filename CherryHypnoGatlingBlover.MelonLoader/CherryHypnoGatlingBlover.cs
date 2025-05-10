using CustomizeLib.MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using Unity.VisualScripting;
using UnityEngine;

namespace CherryHypnoGatlingBlover.MelonLoader;

[RegisterTypeInIl2Cpp]
public class CherryHypnoGatlingBlover : MonoBehaviour
{
    /// <summary>
    /// 植物ID
    /// </summary>
    public static int PlantID = 1500 + 003;
    /// <summary>
    /// 子弹ID
    /// </summary>
    public static int BulletID = PlantID * 10 + 001;
    public Shooter plant => gameObject.GetComponent<Shooter>();
    public static int Buff1 { get; set; } = -1;
    public static int Buff2 { get; set; } = -1;

    public void Awake()
    {
        //炮口
        plant.shoot = plant.gameObject.transform.GetChild(0).FindChild("Shoot");
    }

    /// <summary>
    /// Anim Event
    /// </summary>
    public void MyAnimShoot()
    {
        //生成子弹
        Bullet bullet = Board.Instance.GetComponent<CreateBullet>().SetBullet(
            (float)(plant.shoot.position.x + 0.1f), plant.shoot.position.y,
            plant.thePlantRow,
            (BulletType)CherryHypnoGatlingBlover.BulletID, 0);
        //同步子弹伤害
        bullet.Damage = plant.attackDamage;
        //同步攻速
        this.MyUpdateInterval();
    }

    /// <summary>
    /// 同步攻速
    /// </summary>
    public void MyUpdateInterval()
    {
        //默认攻速1.5f
        plant.thePlantAttackInterval = 1.5f;
        float min = plant.thePlantAttackInterval;
        //下方植物射击间隔，获得最小攻速
        foreach (Plant plant in Lawnf.Get1x1Plants(plant.thePlantColumn, plant.thePlantRow))
        {
            if (plant.thePlantType != (PlantType)CherryHypnoGatlingBlover.PlantID && plant.thePlantAttackInterval < min)
            {
                min = plant.thePlantAttackInterval;
            }
        }

        plant.thePlantAttackCountDown = min;
    }

    /// <summary>
    /// 该植物铲除时掉落卡片
    /// </summary>
    [HarmonyPatch(typeof(Plant), nameof(Plant.Die))]
    public class CherryHypnoGatlingBlover_Die
    {
        [HarmonyPrefix]
        static bool Prefix(Plant __instance, Plant.DieReason reason)
        {
            if ((int)__instance.thePlantType == CherryHypnoGatlingBlover.PlantID)
            {
                //死于铲子
                if (reason == Plant.DieReason.ByShovel)
                {
                    //掉落卡片
                    Lawnf.SetDroppedCard(__instance.transform.position, PlantType.HypnoBlover);
                    Lawnf.SetDroppedCard(__instance.transform.position, PlantType.UltimateGatling);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 种植植物上限
    /// </summary>
    [HarmonyPatch(typeof(CreatePlant), nameof(CreatePlant.SetPlant))]
    public class CherryHypnoGatlingBlover_CreatePlant
    {
        [HarmonyPrefix]
        static bool Prefix(CreatePlant __instance, int newColumn, int newRow, PlantType theSeedType)
        {
            if ((int)theSeedType == CherryHypnoGatlingBlover.PlantID)
            {
                //如果植物数量大于行*2
                if (Board.Instance.GameObject().transform.GetComponentsInChildren<CherryHypnoGatlingBlover>().Length >
                    Board.Instance.rowNum * 2 - 1)
                {
                    InGameText.Instance.ShowText("种植数量达到上限", 5);
                    //阻断种植
                    return false;
                }
            }

            return true;
        }
    }
}
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace GatlingIronPuff.MelonLoader;

[RegisterTypeInIl2Cpp]
public class GatlingIronPuff : MonoBehaviour
{
    /// <summary>
    /// 植物ID
    /// </summary>
    public static int PlantID = 1500 + 001;

    /// <summary>
    /// 子弹ID
    /// </summary>
    public static int BulletID = PlantID * 10 + 001;

    public Shooter plant => gameObject.GetComponent<Shooter>();

    /// <summary>
    /// 词条1 ID
    /// </summary>
    public static int Buff1 { get; set; } = -1;

    /// <summary>
    /// 词条2 ID
    /// </summary>
    public static int Buff2 { get; set; } = -1;

    public void Awake()
    {
        //炮口
        plant.shoot = plant.gameObject.transform.FindChild("Shoot");
        //低矮植物
        plant.isShort = true;
    }

    /// <summary>
    /// Anim Event
    /// </summary>
    public void MyAnimShoot()
    {
        //创造子弹
        Bullet bullet = Board.Instance.GetComponent<CreateBullet>().SetBullet(
            (float)(plant.shoot.position.x + 0.1f), plant.shoot.position.y,
            plant.thePlantRow,
            (BulletType)GatlingIronPuff.BulletID, 0);
        bullet.Damage = plant.attackDamage;
        //词条1存在
        if (Lawnf.TravelAdvanced(GatlingIronPuff.Buff1))
        {
            int num = 0;
            //判断当前格子有几个该植物
            foreach (Plant plant in Lawnf.Get1x1Plants(plant.thePlantColumn, plant.thePlantRow))
            {
                if ((int)plant.thePlantType == GatlingIronPuff.PlantID)
                {
                    num++;
                }
            }

            //如果当前格子存在3个该植物
            if (num >= 3)
            {
                bullet.Damage = plant.attackDamage * 3;
            }
        }
    }
}
using System.Text;
using CustomizeLib.MelonLoader;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using System.Reflection;

[assembly: MelonInfo(typeof(GatlingIronPuff.MelonLoader.Core), nameof(GatlingIronPuff), "1.0.0",
    "likefengzi",
    null)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

namespace GatlingIronPuff.MelonLoader;

public class Core : MelonMod
{
    /// <summary>
    /// 初始化
    /// </summary>
    public override void OnInitializeMelon()
    {
        //控制台支持中文
        Console.OutputEncoding = Encoding.UTF8;
        //注册类
        ClassInjector.RegisterTypeInIl2Cpp<GatlingIronPuff>();
        ClassInjector.RegisterTypeInIl2Cpp<Bullet_PuffIronPeaPierce>();
        //资源文件
        var ab = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "gatlingironpuff");
        //注册植物
        CustomCore.RegisterCustomPlant<Shooter, GatlingIronPuff>(
            GatlingIronPuff.PlantID,
            ab.GetAsset<GameObject>("GatlingIronPuffPrefab"),
            ab.GetAsset<GameObject>("GatlingIronPuffPreview"),
            new List<(int, int)>
            {
                ((int)PlantType.GatlingPuff, (int)PlantType.IronPea),
                ((int)PlantType.IronPea, (int)PlantType.GatlingPuff)
            },
            1,
            0,
            100,
            1200,
            60f,
            350);
        //小喷菇类植物，一格三个，没有低矮效果
        CustomCore.TypeMgrExtra.IsPuff.Add((PlantType)GatlingIronPuff.PlantID);
        //注册子弹
        CustomCore.RegisterCustomBullet<Bullet_PuffIronPeaPierce>(
            (BulletType)GatlingIronPuff.BulletID,
            ab.GetAsset<GameObject>("Bullet_PuffIronPeaPierce"));
        //注册词条
        GatlingIronPuff.Buff1 = CustomCore.RegisterCustomBuff(
            "三人成虎：三个在同一格时，伤害*3",
            BuffType.AdvancedBuff,
            () => Board.Instance.ObjectExist<GatlingIronPuff>(),
            1000,
            null,
            (PlantType)GatlingIronPuff.PlantID
        );
        GatlingIronPuff.Buff2 = CustomCore.RegisterCustomBuff(
            "不得寸进：铁豆可穿透四次",
            BuffType.AdvancedBuff,
            () => Board.Instance.ObjectExist<GatlingIronPuff>(),
            1000,
            null,
            (PlantType)GatlingIronPuff.PlantID
        );
        //注册图鉴
        CustomCore.AddPlantAlmanacStrings(
            GatlingIronPuff.PlantID,
            "铁桶机枪小喷菇" + "(" + (PlantType)GatlingIronPuff.PlantID + ")",
            "<color=#0000FF>坚硬如铁的铁桶机枪小喷菇，发射4颗击退并且对防具特攻小铁豆</color>\n\n" +
            "<color=#3D1400>贴图作者：@事磁瓜罢</color>\n\n" +
            "<color=#905000>韧性：</color><color=#FF0000>1200</color>\n" +
            "<color=#905000>伤害：</color><color=#FF0000>100*4/s</color>\n" +
            "<color=#905000>融合配方：</color><color=#FF0000>机枪小喷菇+铁桶豌豆射手</color>\n" +
            "<color=#905000>特点：</color><color=#FF0000>低矮植物，铁植物，磁力植物</color>\n\n" +
            "<color=#905000>词条1：</color><color=#FF0000>三人成虎：三个在同一格时，伤害*3</color>\n" +
            "<color=#905000>词条2：</color><color=#FF0000>不得寸进：铁豆可穿透四次</color>\n");
    }
}
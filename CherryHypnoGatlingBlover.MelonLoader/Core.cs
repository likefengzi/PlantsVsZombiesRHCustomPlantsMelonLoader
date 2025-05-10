using System.Text;
using CustomizeLib.MelonLoader;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using System.Reflection;

[assembly: MelonInfo(typeof(CherryHypnoGatlingBlover.MelonLoader.Core), nameof(CherryHypnoGatlingBlover),
    "1.0.0", "likefengzi", null)]
[assembly: MelonGame("LanPiaoPiao", "PlantsVsZombiesRH")]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

namespace CherryHypnoGatlingBlover.MelonLoader;

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
        ClassInjector.RegisterTypeInIl2Cpp<CherryHypnoGatlingBlover>();
        ClassInjector.RegisterTypeInIl2Cpp<Bullet_HypnoCherry>();
        //资源文件
        var ab = CustomCore.GetAssetBundle(Assembly.GetExecutingAssembly(), "cherryhypnogatlingblover");
        //注册植物
        CustomCore.RegisterCustomPlant<Shooter, CherryHypnoGatlingBlover>(
            CherryHypnoGatlingBlover.PlantID,
            ab.GetAsset<GameObject>("CherryHypnoGatlingBloverPrefab"),
            ab.GetAsset<GameObject>("CherryHypnoGatlingBloverPreview"),
            new List<(int, int)>
            {
                ((int)PlantType.HypnoBlover, (int)PlantType.UltimateGatling),
                ((int)PlantType.UltimateGatling, (int)PlantType.HypnoBlover)
            },
            1.5f,
            0,
            300,
            300,
            20f,
            1200
        );
        //飞行植物
        CustomCore.TypeMgrExtra.FlyingPlants.Add((PlantType)CherryHypnoGatlingBlover.PlantID);
        //注册子弹
        CustomCore.RegisterCustomBullet<Bullet_HypnoCherry>(
            (BulletType)CherryHypnoGatlingBlover.BulletID,
            ab.GetAsset<GameObject>("Bullet_HypnoCherry"));
        //注册图鉴
        CustomCore.AddPlantAlmanacStrings(
            CherryHypnoGatlingBlover.PlantID,
            "飞天樱魅射手" + "(" + (PlantType)CherryHypnoGatlingBlover.PlantID + ")",
            "<color=#0000FF>拥有魅惑的飞樱，子弹概率会把僵尸魅惑并转化为究极机枪读报</color>\n\n" +
            "<color=#3D1400>贴图作者：@白羽丶最爱究喷了</color>\n\n" +
            "<color=#905000>韧性：</color><color=#FF0000>300</color>\n" +
            "<color=#905000>伤害：</color><color=#FF0000>300</color>\n" +
            "<color=#905000>融合配方：</color><color=#FF0000>究极樱桃射手+魅惑三叶草</color>\n\n" +
            "<color=#905000>特点：</color><color=#FF0000>对低于1.5s攻速的植物，攻速会同步下方植物</color>\n" +
            "<color=#FF0000>对40%以下血量的僵尸有概率魅惑，魅惑后有10%的概率将僵尸转化为魅惑樱机二爷</color>\n" +
            "<color=#FF0000>铲除后将会同时掉落究极樱桃射手和魅惑三叶草两颗卡片</color>\n\n" +
            "<color=#905000>\"装b，我让你飞起来，听到没小b崽子\"飞天樱魅射手每天都在其他植物面前说这句话，但是其他植物一直都没有降低让她飞到自己头上的渴望</color>");
    }
}
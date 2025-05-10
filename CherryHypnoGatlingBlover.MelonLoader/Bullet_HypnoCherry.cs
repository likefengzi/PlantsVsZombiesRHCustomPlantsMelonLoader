using Il2Cpp;
using MelonLoader;

namespace CherryHypnoGatlingBlover.MelonLoader;
[RegisterTypeInIl2Cpp]
public class Bullet_HypnoCherry:Bullet_superCherry
{
    /// <summary>
    /// 重写击中僵尸
    /// </summary>
    /// <param name="zombie"></param>
    public override void HitZombie(Zombie zombie)
    {
        //僵尸受伤
        zombie.TakeDamage(DmgType.Normal, 300);
        //创建粉色泡泡
        CreateParticle.SetParticle((int)ParticleType.Gloom, zombie.ColliderPosition, zombie.theZombieRow).transform
            .SetParent(GameAPP.board.transform);
        //总HP低于40%
        if ((zombie.theHealth + zombie.theSecondArmorHealth + zombie.theFirstArmorHealth) /
            (zombie.theMaxHealth + zombie.theSecondArmorMaxHealth + zombie.theFirstArmorMaxHealth) <= 0.4 &&
            new Random().Next(0, 100) < 10)
        {
            //魅惑
            zombie.SetMindControl();
            if (new Random().Next(0, 100) < 10)
            {
                //精英僵尸登场特效
                CreateParticle
                    .SetParticle((int)ParticleType.HypnoEmperorSkinCloud, zombie.ColliderPosition, zombie.theZombieRow)
                    .transform.SetParent(GameAPP.board.transform);
                //创造樱桃机枪二爷并魅惑
                CreateZombie.Instance
                    .SetZombie(zombie.theZombieRow, ZombieType.CherryPaperZ95, zombie.ColliderPosition.x)
                    .GetComponent<Zombie>().SetMindControl();
                //命中的僵尸死亡
                zombie.Die(0);
            }
        }

        this.Die();
    }
}
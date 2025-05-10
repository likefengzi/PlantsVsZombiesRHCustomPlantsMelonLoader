using Il2Cpp;
using MelonLoader;

namespace GatlingIronPuff.MelonLoader;

[RegisterTypeInIl2Cpp]
public class Bullet_PuffIronPeaPierce : Bullet_firePea_super
{
    /// <summary>
    /// 子弹击中过的僵尸列表
    /// </summary>
    public List<Zombie> Zombies = new List<Zombie>();

    /// <summary>
    /// 重写击中僵尸
    /// </summary>
    /// <param name="zombie"></param>
    public override void HitZombie(Zombie zombie)
    {
        //如何僵尸击中过
        if (Zombies.Contains(zombie))
        {
            //抵消计数
            this.hitTimes--;
            //阻断
            return;
        }
        else
        {
            //添加到击中僵尸列表
            Zombies.Add(zombie);
            //僵尸受伤
            zombie.TakeDamage(DmgType.Normal, this.Damage);
            //僵尸击退，大铁豆小铁豆都是0.2
            zombie.KnockBack(0.2f, Zombie.KnockBackReason.ByIronPea);
            //铁豆击中特效
            CreateParticle.SetParticle((int)ParticleType.IronPeaSplat, this.transform.position, this.theBulletRow);
        }

        //这个不需要，外面有
        /*if (this.hitTimes==this.penetrationTimes)
        {
            this.Die();
        }*/
    }

    /// <summary>
    /// 重写穿透次数
    /// </summary>
    public override void SetPenetrationTime()
    {
        //根据词条设置穿透次数
        if (Lawnf.TravelAdvanced(GatlingIronPuff.Buff2))
        {
            this.penetrationTimes = 5;
        }
        else
        {
            this.penetrationTimes = 1;
        }
    }
}
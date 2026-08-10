using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using SwordMastery.Content.Items.FlyingSword.Glaive_H;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.FlyingSword.AGlobalControl
{
    public class FlyingGunProjectile_190 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4; // 设置动画帧数
        }
        //Player player => Main.player[Projectile.owner];
        public override void SetDefaults()
        {
            Projectile.knockBack = 0.5f; // 击退
            Projectile.width = 20; // 弹幕宽度
            Projectile.height = 20; // 弹幕高度
            Projectile.friendly = true; // 友方弹幕
            //Projectile.tileCollide = true; // 与瓷砖碰撞
            Projectile.DamageType = DamageClass.Summon; // 伤害类型
            Projectile.penetrate = -1; // 穿透
            Projectile.ignoreWater = true; // 无视液体
            Projectile.timeLeft = 180; // 存在时间，单位为帧
            Projectile.alpha = 1; // 透明度
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public bool Sticking
        {
            get { return Projectile.ai[0] != 0; }// 因为默认状态下ai[0]是 = 0，所以这里用 != 0进行判定
            set { Projectile.ai[0] = value ? 1 : 0; }// 三元运算符：当表达式值为true，返回前者，反之为后者
        }
        public int TargetWho
        {
            get { return (int)Projectile.ai[1]; }
            set { Projectile.ai[1] = value; }
        }
        public override void AI()
        {
            // 更新帧动画
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5) // 每5帧切换下一帧
            {
                Projectile.frame++;
                Projectile.frame %= Main.projFrames[Projectile.type]; // 循环动画
                Projectile.frameCounter = 0;
            }
            Projectile.rotation = Projectile.velocity.ToRotation(); // 计算弹幕的旋转角度

            if (Sticking)// 当弹幕粘在NPC时执行
            {
                // 获取粘滞的NPC
                NPC target = Main.npc[TargetWho];

                // 如果目标NPC死了弹幕也一起死亡
                if (!target.active)
                {
                    Projectile.Kill();
                    return;// 结束函数
                }
                // 把目标的速度给弹幕，让弹幕跟着粘滞目标
                // 这就是粘住了（迫真
                Projectile.Center = target.Center - Projectile.velocity * 2f;
                Projectile.gfxOffY = target.gfxOffY;

                Projectile.ai[2]++;// 作为攻击计时器
                // 此处即为每10帧对NPC造成一次伤害，数值为5点
                if (Projectile.ai[2] >= 10)
                {
                    target.SimpleStrikeNPC(Projectile.damage, 0);
                    Projectile.ai[2] = 0;
                }
            }
            else
            {
                if (Projectile.timeLeft > 90)
                    Projectile.velocity.Y += 0.1f; // 逐帧增加下落速度，改变重力效果
                else
                    Projectile.velocity.Y += 0.5f; // 逐帧增加下落速度，改变重力效果
            }
            // 在最后一秒内逐渐缩小弹幕
            if (!Sticking && Projectile.timeLeft <= 60)
            {
                float t = Projectile.timeLeft / 60f;
                Projectile.scale = MathHelper.Lerp(0.2f, 1f, t);
                Projectile.height = Projectile.width = (int)(20 * MathHelper.Lerp(0.2f, 1f, t));
            }
            else if(Sticking)
            {
                Projectile.scale = 1f;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 打到某个目标之后
            // 把粘滞设为true，这样AI就会从正常行动切换到粘滞状态
            Sticking = true;
            // 把被命中目标的身份记录下来
            TargetWho = target.whoAmI;
            // 并重置弹幕的存活时间
            Projectile.timeLeft = 60;
            Projectile.velocity = (target.Center - Projectile.Center) *
               0.85f; // 根据目标中心的差值（实体中心之间的差异）更改速度
            Projectile.netUpdate = true; // 网络更新这个矛
        }
        public override bool? CanDamage()
        {
            return !Sticking;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 这段代码使弹射物非常弹跳。
            if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 1f)
            {
                Projectile.velocity.X = oldVelocity.X * -0.5f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 1f)
            {
                Projectile.velocity.Y = oldVelocity.Y * -0.5f;
            }
            return false;
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            SpriteEffects effects;
            float rotationOffset;
            if (Projectile.velocity.X > 0)
            {
                rotationOffset = MathHelper.ToRadians(0f);
                effects = SpriteEffects.None;
            }
            else
            {
                rotationOffset = MathHelper.ToRadians(180f);
                effects = SpriteEffects.FlipHorizontally;
            }
            Rectangle rectangle = new Rectangle(
               0,
               texture.Height / Main.projFrames[Type] * Projectile.frame,
               texture.Width,
               texture.Height / Main.projFrames[Type]
           );
            Main.EntitySpriteDraw(
               texture,
               Projectile.Center - Main.screenPosition,
               rectangle,
               lightColor,
               Projectile.rotation + rotationOffset,
               new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
               Projectile.scale,
               effects,
               0);
            return false;
        }
    }
    public class FlyingGunProjectile_444 : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.knockBack = 0.5f; // 击退
            Projectile.width = 10; // 弹幕宽度
            Projectile.height = 10; // 弹幕高度
            Projectile.friendly = true; // 友方弹幕
            //Projectile.tileCollide = true; // 与瓷砖碰撞
            Projectile.DamageType = DamageClass.Ranged; // 伤害类型
            Projectile.penetrate = 1; // 穿透
            Projectile.ignoreWater = true; // 无视液体
            Projectile.timeLeft = 180; // 存在时间，单位为帧
        }
        public override void AI()
        {
            Projectile.velocity *= 0.99f;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 根据旧速度反转弹幕的速度
            if (Projectile.velocity.X != 0)
                Projectile.velocity.X = -oldVelocity.X + Projectile.velocity.X / 8; // 水平方向反弹

            if (Projectile.velocity.Y != 0)
                Projectile.velocity.Y = -oldVelocity.Y + Projectile.velocity.Y / 8; // 垂直方向反弹

            return false; // 返回 false 以表示弹幕没有被销毁
        }
    }
}
using Microsoft.Xna.Framework;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using SwordMastery.Content.Items.FlyingSword.Glaive_H;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.FlyingSword.AGlobalControl
{
    public class FlyingswordEffect
    {
        //===========================养蜂人====================================
        public static void BeeKeeperEffect(Player player, NPC target, int damage, Projectile projectile)
        {
            if (Main.rand.NextBool(2) && target.CanBeChasedBy(projectile.owner))
            {
                int beeType = Utils.SelectRandom(Main.rand, ProjectileID.Bee, ProjectileID.GiantBee);
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(player.HeldItem),
                    target.Center,
                    new Vector2(Main.rand.NextFloat(-2, 2), Main.rand.NextFloat(-5, -2)),
                    beeType,
                    beeType== ProjectileID.Bee?damage / 2:damage,
                    0f,
                    player.whoAmI
                );
            }
            // 25%困惑
            if (Main.rand.NextBool(8))
            {
                target.AddBuff(BuffID.Confused, 60);
            }
        }
        //==============================魔光剑==================================
        public static void LightsBaneEffect(Player player, NPC target, int damage, Projectile projectile)
        {
            Vector2 vector2 = new Vector2(0, Main.rand.NextFloat(-400, 400));
            if (target.CanBeChasedBy(projectile.owner))
             Projectile.NewProjectile(
                player.GetSource_ItemUse(player.HeldItem),
                target.Center,
                (target.Center - player.Center + vector2).SafeNormalize(Vector2.UnitY)*0.1f,
                ProjectileID.LightsBane,
                damage,
                0f,
                player.whoAmI,
                Main.rand.NextFloat(0.5f, 2f)
            );
        }
        //============================血腥屠刀===================================
        public static void BloodButchererEffect(Player player, NPC target, int damage)
        {
           
            // 统计目标身上的血腥屠刀弹幕数量
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj_ = Main.projectile[i];
                if (proj_.active && proj_.type == ProjectileID.BloodButcherer && proj_.ai[1] == target.whoAmI)
                {
                    count++;
                }
            }
            // 超过4个就不发射
            if (count >= 4)
                return;
            // 60% 概率发射弹幕
            if (Main.rand.Next(5) >= 3)
                return;

            float radius = Main.rand.Next(0, 20); // 距离目标中心的半径
            int projType = ProjectileID.BloodButcherer;
            int Damage = (int)(damage / 2f); // 可根据需要调整
            float knockback = 0f;

            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0.2f;

            int proj = Projectile.NewProjectile(
                player.GetSource_ItemUse(player.HeldItem),
                spawnPos,
                velocity,
                projType,
                Damage,
                knockback,
                player.whoAmI,
                rand
            );
            Main.projectile[proj].light = 0.32f;

            // 向四周随机扩散的粒子
            int dustCount = 12;
            for (int i = 0; i < dustCount; i++)
            {
                float dustAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dustSpeed = Main.rand.NextFloat(3f, 7f);
                Vector2 dustVelocity = dustAngle.ToRotationVector2() * dustSpeed;
                Dust dust = Dust.NewDustPerfect(target.Center, DustID.Blood, dustVelocity, 100, new Color(204, 0, 0), 1f);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.2f, 2.2f);
                dust.fadeIn = Main.rand.NextFloat(0.8f, 1.5f);
            }
        }
        //============================村正===================================
        public static void MuramasaEffect(Player player, NPC target, int damage, Projectile projectile)
        {
            float radius = 120f; // 距离目标中心的半径
            int projType = ProjectileID.Muramasa;
            int Damage = (int)(damage / Main.rand.NextFloat(0.8f, 1f)); // 可根据需要调整
            float knockback = 0.1f;

            // 以弹幕到敌人的方向为基础，随机偏移±0.5弧度
            float baseAngle = (target.Center - projectile.Center).ToRotation();
            float randomOffset = Main.rand.NextFloat(-0.66f, 0.66f); // 随机偏移
            float angle = baseAngle + randomOffset;

            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 12f; // 朝向目标中心

            if (target.CanBeChasedBy(projectile.owner))
            {
                int proj = Projectile.NewProjectile(
                    player.GetSource_ItemUse(player.HeldItem),
                    spawnPos,
                    velocity,
                    projType,
                    Damage,
                    knockback,
                    player.whoAmI,
                    randomOffset
                );
                Main.projectile[proj].light = 0.1f;
                Main.projectile[proj].penetrate = -1;
                Main.projectile[proj].tileCollide = false;
                Main.projectile[proj].netUpdate = true;
                //Main.projectile[proj].usesLocalNPCImmunity = true;
                //Main.projectile[proj].localNPCHitCooldown = -1;
            }
            //粒子
            for (int i = 0; i < 3; i++)
            {
                float dustAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float dustSpeed = Main.rand.NextFloat(3f, 7f);
                Vector2 dustVelocity = dustAngle.ToRotationVector2() * dustSpeed;
                Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.BlueCrystalShard, dustVelocity, 100, Color.White, 1f);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.8f, 1f);
            }
        }
        //============================火山===================================
        public static void VolcanoEffect(Player player, NPC target, int damage, Projectile projectile)
        {
            float radius = Main.rand.Next(10, 20); // 距离目标中心的半径
            int projType = ProjectileID.Volcano;
            int Damage = (int)(damage / Main.rand.NextFloat(0.8f, 1f)); // 可根据需要调整
            float knockback = 2f;

            // 随机一个角度
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0.2f; // 朝向目标中心

            if (target.CanBeChasedBy(projectile.owner))
            {
                int proj = Projectile.NewProjectile(
                player.GetSource_ItemUse(player.HeldItem),
                spawnPos,
                velocity,
                projType,
                Damage,
                knockback,
                player.whoAmI
            );
                Main.projectile[proj].light = 0.54f;
            }
        }
        //============================瞌睡章鱼===================================
        public static void MonkStaffT1ExplosionEffct(Player player, NPC target, int damage, Projectile projectile)
        {
            if (target.CanBeChasedBy(projectile.owner))
            {
                int newProj_ = Projectile.NewProjectile(
                                    player.GetSource_ItemUse(player.HeldItem),
                                    target.Center + new Vector2(0, -80),
                                    Vector2.Zero,
                                    ProjectileID.MonkStaffT1Explosion,
                                    (int)(damage*Main.rand.NextFloat(1.2f, 2f)),
                                    0f,
                                    player.whoAmI
                                );
            }
        }
        //============================草剑===================================
        public static void BladeOfGrassEffct(Player player, NPC target, int damage, Projectile projectile)
        {
            int projType = ProjectileID.BladeOfGrass;
            int Damage = (int)(damage / Main.rand.NextFloat(1, 4));
            float knockback = 0f;
            if (target.CanBeChasedBy(projectile.owner))
            {
                for (int i = 0; i < 2; i++)
                {
                    float radius = Main.rand.Next(10, 30);
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float rand = Main.rand.NextFloat(0.08f, 0.30f) * (Main.rand.NextBool() ? 1 : -1);
                    Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
                    Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0.2f;

                    int proj = Projectile.NewProjectile(
                        player.GetSource_ItemUse(player.HeldItem),
                        spawnPos,
                        velocity,
                        projType,
                        Damage,
                        knockback,
                        player.whoAmI,
                        rand
                    );
                    Main.projectile[proj].light = 0.34f;
                }
            }
        }
        //============================舌锋剑===================================
        public static void IchorSplashEffct(Player player, NPC target, int damage, Projectile projectile)
        {
            int projType = ProjectileID.IchorSplash;
            int Damage = damage / 2;
            float knockback = 0f;
            if (target.CanBeChasedBy(projectile.owner))
            {
                Vector2 velocity = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitY) * 4f;

                int proj = Projectile.NewProjectile(
                    player.GetSource_ItemUse(player.HeldItem),
                    projectile.Center,
                    velocity,
                    projType,
                    Damage,
                    knockback,
                    player.whoAmI
                );
                Main.projectile[proj].light = 0.34f;
            }
        }
        //==============================真永夜刃光环===================================
        public static void TrueNightsEdge(NPC targetNPC, Projectile projectile)
        {
            //随机值
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 velocity = (targetNPC.Center - projectile.Center).SafeNormalize(Vector2.UnitY) * 14f; // 朝向目标中心
            var proj_1 = Projectile.NewProjectileDirect(
            projectile.GetSource_FromThis(),
            projectile.Center,
            velocity,
            ModContent.ProjectileType<FlyingTrueNightsEdgeProj_P>(),
            (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
            projectile.knockBack,
            projectile.owner,
            rand, 1f);
            if (proj_1.ModProjectile is FlyingTrueNightsEdgeProj_P customProj)
                customProj.Initialize(4f, 40f, 0.7f);
            var proj_2 = Projectile.NewProjectileDirect(
                projectile.GetSource_FromThis(),
                projectile.Center,
                velocity,
                ModContent.ProjectileType<FlyingTrueNightsEdgeProj_P>(),
                (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                projectile.knockBack,
                projectile.owner,
                rand);
            if (proj_2.ModProjectile is FlyingTrueNightsEdgeProj_P customProj_)
                customProj_.Initialize(4f, 40f, 0.7f);
        }
        //==============================（真/不真）永夜刃击中===================================
        public static void TrueNightsEdgeEffct(NPC target, Projectile projectile, float Scale_)
        {
            float radius = 40f; // 距离目标中心的半径
            // 角度为弹幕到敌人转换
            float angle = (float)Math.Atan2(target.Center.X - projectile.Center.X, target.Center.Y - projectile.Center.Y);
            //随机值
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 8f; // 朝向目标中心

            if (target.CanBeChasedBy(projectile.owner))
            {
                var proj__ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingNightsEdgeProj_>(),
                    (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                    projectile.knockBack,
                    projectile.owner,
                    rand, 1f);
                if (proj__.ModProjectile is FlyingNightsEdgeProj_ customProj__)
                    customProj__.Initialize(-2f, 30f, Scale_);
                var proj___ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingNightsEdgeProj_>(),
                    (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                    projectile.knockBack,
                    projectile.owner,
                    rand);
                if (proj___.ModProjectile is FlyingNightsEdgeProj_ customProj___)
                    customProj___.Initialize(2f, 30f, Scale_);
            }
        }
        //==============================断钢剑===================================
        public static void ExcaliburEffct(NPC target, Projectile projectile)
        {
            float radius = 40f; // 距离目标中心的半径
            int projType = ModContent.ProjectileType<FlyingExcaliburProj_P>();
            int damage = (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)); // 可根据需要调整
            // 角度为弹幕到敌人转换
            float angle = (float)Math.Atan2(target.Center.X - projectile.Center.X, target.Center.Y - projectile.Center.Y);
            //随机值
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0.2f; // 朝向目标中心

            if (target.CanBeChasedBy(projectile.owner))
            {
                var proj = Projectile.NewProjectileDirect(
                projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                projType,
                damage / 2,
                projectile.knockBack,
                projectile.owner,
                rand, 1f);
                if (proj.ModProjectile is FlyingExcaliburProj_P customProj)
                    customProj.Initialize(1f, 30f, 0.8f);
                var proj_ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    projType,
                    damage / 2,
                    projectile.knockBack,
                    projectile.owner,
                    rand);
                if (proj_.ModProjectile is FlyingExcaliburProj_P customProj_)
                    customProj_.Initialize(-1f, 30f, 0.8f);
                var proj__ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    projType,
                    (int)(damage / 2 * (1 + projectile.ai[0] / 80)),
                    projectile.knockBack,
                    projectile.owner,
                    rand);
                if (proj__.ModProjectile is FlyingExcaliburProj_P customProj__)
                    customProj__.Initialize(-3f, 30f, 0.2f + projectile.ai[0] / 100);
                var proj___ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    projType,
                    (int)(damage / 2 * (1 + projectile.ai[0] / 80)),
                    projectile.knockBack,
                    projectile.owner,
                    rand);
                if (proj___.ModProjectile is FlyingExcaliburProj_P customProj___)
                    customProj___.Initialize(3f, 30f, 0.2f + projectile.ai[0] / 100);
            }
        }
        //=============================真断钢剑===================================
        public static void TrueExcaliburEffct(NPC target, Projectile projectile)
        {
            float radius = 40f; // 距离目标中心的半径
            int damage = (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)); // 可根据需要调整
            // 角度为弹幕到敌人转换
            float angle = (float)Math.Atan2(target.Center.X - projectile.Center.X, target.Center.Y - projectile.Center.Y);
            //随机值
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0.2f; // 朝向目标中心

            if (target.CanBeChasedBy(projectile.owner))
            {
                var proj = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingTrueExcaliburProj_P>(),
                    (int)(damage / 2 * (1 + projectile.ai[0] / 160)),
                    projectile.knockBack,
                    projectile.owner,
                    rand, 1f);
                if (proj.ModProjectile is FlyingTrueExcaliburProj_P customProj)
                    customProj.Initialize(1f, 30f, 1f);
                var proj_ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingTrueExcaliburProj_P>(),
                    (int)(damage / 2 * (1 + projectile.ai[0] / 160)),
                    projectile.knockBack,
                    projectile.owner,
                    rand);
                if (proj_.ModProjectile is FlyingTrueExcaliburProj_P customProj_)
                    customProj_.Initialize(-1f, 30f, 1f);
                var proj__ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingExcaliburProj_P>(),
                    (int)(damage / 2 * (1 + projectile.ai[0] / 80)),
                    projectile.knockBack,
                    projectile.owner,
                    rand, 1f);
                if (proj__.ModProjectile is FlyingExcaliburProj_P customProj__)
                    customProj__.Initialize(-3f, 30f, 0.6f + projectile.ai[0] / 240);
                var proj___ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingExcaliburProj_P>(),
                    (int)(damage / 2 * (1 + projectile.ai[0] / 80)),
                    projectile.knockBack,
                    projectile.owner,
                    rand);
                if (proj___.ModProjectile is FlyingExcaliburProj_P customProj___)
                    customProj___.Initialize(3f, 30f, 0.6f + projectile.ai[0] / 240);
            }
        }
        //=============================泰拉刃===================================
        public static void TerraBlade(NPC targetNPC, Projectile projectile)
        {
            //随机值
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 velocity = (targetNPC.Center - projectile.Center).SafeNormalize(Vector2.UnitY) * 21f; // 朝向目标中心
            var proj = Projectile.NewProjectileDirect(
            projectile.GetSource_FromThis(),
            projectile.Center - new Vector2((targetNPC.Center - projectile.Center).X,
            (targetNPC.Center - projectile.Center).Y) * 0.5f,
            velocity,
            ModContent.ProjectileType<FlyingTerraBladeProj_P>(),
            (int)(projectile.damage * Main.rand.NextFloat(1f, 2f)),
            projectile.knockBack,
            projectile.owner,
            rand, 1f);
            if (proj.ModProjectile is FlyingTerraBladeProj_P customProj)
                customProj.Initialize(0f, 40f, 1.2f);
        }
        //=============================泰拉刃击中===================================
        public static void TerraBladeEffct(NPC target, Projectile projectile)
        {
            float radius = 20f; // 距离目标中心的半径
            // 角度为弹幕到敌人转换
            float angle = (float)Math.Atan2(target.Center.X - projectile.Center.X, target.Center.Y - projectile.Center.Y);
            //随机值
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0.1f; // 朝向目标中心

            if (target.CanBeChasedBy(projectile.owner))
            {
                var proj__ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingTerraBladeProj_P_>(),
                    (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                    projectile.knockBack,
                    projectile.owner,
                    rand, 1f);
                if (proj__.ModProjectile is FlyingTerraBladeProj_P_ customProj__)
                    customProj__.Initialize(-2f, 30f, 0.8f);
                var proj___ = Projectile.NewProjectileDirect(
                    projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FlyingTerraBladeProj_P_>(),
                    (int)(projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)),
                    projectile.knockBack,
                    projectile.owner,
                    rand);
                if (proj___.ModProjectile is FlyingTerraBladeProj_P_ customProj___)
                    customProj___.Initialize(2f, 30f, 0.8f);
            }
        }
    }
}
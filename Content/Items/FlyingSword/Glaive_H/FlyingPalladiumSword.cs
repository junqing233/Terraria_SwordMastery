using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.FlyingSword.BladeForge;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.Glaive_H
{
    class FlyingPalladiumSword : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingPalladiumSword").Value;
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingPalladiumSword_").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }

        public override void SetDefaults()
        {
            //Item.CloneDefaults(ItemID.EmpressBlade);
            Item.damage = 24;
            Item.mana = 10;
            Item.width = 52;
            Item.height = 52;
            Item.useTime = 22;
            Item.useAnimation = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2.75f;
            Item.value = 20000;
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<FlyingPalladiumSwordProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsFlyingPalladiumSword>();
            Item.DamageType = DamageClass.Summon;
            // 读取配置并调整伤害
            var config = ModContent.GetInstance<SwordMasteryConfig>();
            if (config.StrengthExperience == StrengthMode.Ordinary)
            {
                Item.damage = (int)(Item.damage * 0.6f);
            }
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            spriteBatch.Draw(texture, position, sourceRectangle, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture_, position, sourceRectangle, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, texture.Height / 2);
            Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);
            spriteBatch.Draw(texture, drawPosition, sourceRectangle, lightColor, rotation, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture_, drawPosition, sourceRectangle, Color.White * 0.8f, rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
        public override void PostUpdate()
        {
            float intensity = 0.6f; // 控制光芒强度，越小越淡
            //Color(247, 127, 0)
            //•	R: 247 / 255 ≈ 0.97
            //•	G: 127 / 255 ≈ 0.496
            //•	B: 0 / 255 ≈ 0
            Lighting.AddLight(Item.Center, 0.97f * intensity, 0.496f * intensity, 0f * intensity);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsFlyingPalladiumSword>(), 3600);
            player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);
            return false;
        }
        //public override bool MeleePrefix()
        //{
        //    return true; // 返回 true 以允许武器具有近战前缀（例如：传奇）
        //}
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.PalladiumBar, 10)
                .AddTile(ModContent.TileType<BladeForgeTile>())
                .Register();
        }
    }
    public class FlyingPalladiumSwordProj : ModProjectile
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/FlyingPalladiumSword";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            //ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
            // 标记为宠物召唤物
            Main.projPet[Projectile.type] = true;

            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EmpressBlade);
            AIType = ProjectileID.EmpressBlade;
            Projectile.hide = false;
            Projectile.minion = true;
            //Projectile.minionSlots = 0;
            Projectile.timeLeft = 2;
            Projectile.height = Projectile.width = 10;
            Projectile.minionSlots = 1;
            Projectile.light = 0.2f;
            Projectile.extraUpdates = 0; // 0为正常速度，-1为更慢（tModLoader允许为负数）
        }
        
        public override void OnSpawn(IEntitySource source)
        {
            //SoundEngine.PlaySound(SoundID.Item100); // 播放声音
            base.OnSpawn(source);
        }
        private readonly int MaxDis = 800;
        public override bool MinionContactDamage()
        {
            if(FindNPC(MaxDis) > 0 && FindNPC(MaxDis) < Main.npc.Length || Projectile.ai[0] != 0)
            return true;
            return base.MinionContactDamage();
        }
        private int FindNPC(int dis)
        {
            return Projectile.FindTargetWithLineOfSight(dis);
        }
       
        //private float slowAcc = 0f;
        public override bool PreAI()
        {
            var player = Main.player[Projectile.owner];
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsFlyingPalladiumSword>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsFlyingPalladiumSword>())) Projectile.Kill();

            ////修改此参数以确定攻击范围
            //var MaxDis = 500;
            var n = FindNPC(MaxDis);
            if (n >= 0 && n < Main.npc.Length || Projectile.ai[0] != 0)
            {
                return base.PreAI();
            }
            
            Projectile.ai[2] = 0;
            foreach (var p in Main.projectile)
            {
                if (p != null && p.active && SwordProjectileGroup.AllTypes.Contains(p.type))
                    if (p.whoAmI < Projectile.whoAmI) Projectile.ai[2]++;
            }

            #region 以下：魔改原版的代码
            int num3 = (int)Projectile.ai[2] + 1;
            var idleRotation3 = num3 * ((float)Math.PI * 2f) * (1f / 60f) * player.direction + (float)Math.PI / 2f;
            idleRotation3 = MathHelper.WrapAngle(idleRotation3);
            int num4 = (int)(num3 % idleRotation3);
            Vector2 vector = new Vector2(0f, 0.5f).RotatedBy((player.miscCounterNormalized * (2f + num4) + num4 * 0.5f + player.direction * 1.3f) * ((float)Math.PI * 2f)) * 4f;
            var idleSpot = Projectile.rotation.ToRotationVector2() * 10f + player.MountedCenter + new Vector2(player.direction * (num3 * -6 - 16), player.gravDir * -15f);
            idleSpot += vector + new Vector2(8, 8);
            idleRotation3 += (float)Math.PI / 2f;

            Projectile.rotation = Projectile.rotation.AngleLerp(idleRotation3, 0.45f);
            Projectile.Center = Vector2.SmoothStep(Projectile.Center, idleSpot, 0.45f);
            for (int i = 0; i < Projectile.localNPCImmunity.Length; i++)
            {
                Projectile.localNPCImmunity[i] = 0;
            }
            #endregion

            return false;
        }
        private int clown = 0;// 攻击冷却
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            // 冷却判定
            if (clown > 0)
                return;
            float radius = 10f; // 距离目标中心的半径
            int projType = ModContent.ProjectileType<FlyingPalladiumSwordProj_P>();
            int damage = (int)(Projectile.damage * Main.rand.NextFloat(1.2f, 2f)); // 可根据需要调整
            float knockback = 2f;

            // 随机一个角度
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (Main.rand.NextVector2Unit(2f, 4f) - 
                Main.rand.NextVector2Unit(2f, 4f)).SafeNormalize(Vector2.UnitY) * 3f; // 朝向目标中心

            if (target.CanBeChasedBy(Projectile.owner))
            {
                int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                projType,
                damage,
                knockback,
                Projectile.owner,
                target.whoAmI
            );
                Main.projectile[proj].light = 0.54f;
                clown = 10;
            }

            if(Main.rand.NextBool(2))
            {
                return;
            }
            // 2秒回血（120帧）
            var player = Main.player[Projectile.owner];
            player.AddBuff(58, 120);// 钯金套（近战）奖励buff
        }
        public override void AI()
        {
            // 冷却递减
            if (clown > 0)
                clown--;
        }
        public override bool PreDrawExtras()
        {
            return base.PreDrawExtras();
        }
        private bool flag = false;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value ;
            Rectangle rectangle = new Rectangle(
                0,
                texture_.Height / Main.projFrames[Type] * Projectile.frame,
                texture_.Width, 
                texture_.Height / Main.projFrames[Type]
                );
            SpriteEffects effects; // 贴图效果
            float rotationOffset;
            var player = Main.player[Projectile.owner];
            var n = FindNPC(MaxDis);
            if (!(n >= 0 && n < Main.npc.Length || Projectile.ai[0] != 0))
            {
                if (player.direction == -1)
                {
                    rotationOffset = 0f;
                    effects = SpriteEffects.None; // 贴图不翻转
                }
                else
                {
                    rotationOffset = MathHelper.ToRadians(90f); // 旋转偏移135度
                    effects = SpriteEffects.FlipHorizontally; // 翻转贴图
                }
            }
            else
            {
                if (Projectile.ai[0] > 0 && Projectile.ai[0] < 40 && n >= 0 && n < Main.npc.Length)
                {
                    if (Main.npc[n] != null && Projectile.Center.X <= Main.npc[n].Center.X)
                    {
                        flag = true;
                    }
                    if (Main.npc[n] != null && Projectile.Center.X >= Main.npc[n].Center.X)
                    {
                        flag = false;
                    }
                }
                if (flag)
                {
                    rotationOffset = 0f;
                    effects = SpriteEffects.None; // 贴图不翻转
                }
                else
                {
                    rotationOffset = MathHelper.ToRadians(90f); // 旋转偏移135度
                    effects = SpriteEffects.FlipHorizontally; // 翻转贴图
                }
            }
            //var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
            //var lig = lightColor.ToVector3().Length() / 1.75f;
            //var v3 = Main.rgbToHsl(Color.OrangeRed);
            //v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.1f;
            //var c = Main.hslToRgb(v3) * lig;
            //c.A = 0;
            // 使用自定义颜色
            Color LightsColor = new Color(247, 127, 0);
            var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
            var v3 = Main.rgbToHsl(LightsColor);
            v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.01f;
            var c = Main.hslToRgb(v3) /** lig*/;
            c.A = 0;

            Color MyColor = c * (0.4f / 3f);
            MyColor.A = 0;
            int maxStep = ProjectileID.Sets.TrailCacheLength[Type] - 7;
            if (Projectile.ai[0] != 0) maxStep += 7;
            for (int i = 1; i < maxStep; i++)
            {
                for (float j = 0; j < 1; j += 0.3f)
                {
                    float factor = (1 - (float)i / ProjectileID.Sets.TrailCacheLength[Type]) * 0.7f + 0.4f;
                    Vector2 oldcenter = Vector2.Lerp(Projectile.oldPos[i], Projectile.oldPos[i - 1], j) + Projectile.Size / 2 - Main.screenPosition;
                    var oldRo = MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j) - MathHelper.PiOver2 + MathHelper.PiOver4;
                    Main.EntitySpriteDraw(texture_,
                                          oldcenter,
                                          rectangle,
                                          MyColor * factor * 0.8f,
                                          oldRo + rotationOffset,
                                          new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                                          Projectile.scale * 1.5f * factor,
                                          effects,
                                          0);
                }
            }

            Main.EntitySpriteDraw(
                texture_,
                Projectile.Center - Main.screenPosition,
                rectangle,
                lightColor,
                Projectile.rotation - MathHelper.PiOver2 + MathHelper.PiOver4 + rotationOffset,
                new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                Projectile.scale * 1.5f,
                effects,
                0
                );
            #region 以下：渐变高光

            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(texture_,
                                      Projectile.Center - Main.screenPosition,
                                      rectangle,
                                      c * value * 0.6f,
                                      Projectile.rotation - MathHelper.PiOver2 + MathHelper.PiOver4 + rotationOffset,
                                      new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                                      Projectile.scale * 1.5f,
                                      effects,
                                      0);
            }
            #endregion
            return false; // 阻止默认绘制
        }
    }
    public class FlyingPalladiumSwordProj_P : ModProjectile
    {
        private float TargetNPC => Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            AIType = -1; // 不用原版AI
            Projectile.penetrate = 1;
            Projectile.timeLeft = 230;
            Projectile.tileCollide = false;
            Projectile.width = Projectile.height = 20;
            Projectile.usesLocalNPCImmunity = true; // 独立无敌帧
            Projectile.localNPCHitCooldown = -1;    // 独立无敌帧时间
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Projectile.frame = Main.rand.Next(3);
        }
        public override bool PreAI()
        {
            if (Projectile.localAI[0] == 0)
            {
                Projectile.damage = 0;
                Projectile.localAI[0] = 1; // 标记已初始化
            }
            // 刚产生时，随机一个方向
            if (Projectile.timeLeft == 170)
            {
                // 随机方向，速度很慢
                Vector2 dir = Main.rand.NextVector2Circular(1f, 1f).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = dir * Main.rand.NextFloat(0.5f, 1.2f);
            }

            // 橙色粒子
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(0.2f, 0.2f),
                    150,
                    new Color(247, 127, 0),
                    Main.rand.NextFloat(0.7f, 1.2f)
                );
                dust.noGravity = true;
                dust.fadeIn = Main.rand.NextFloat(0.5f, 1.2f);
            }

            if (Projectile.timeLeft > 140 && Projectile.timeLeft <= 160)
            {
                // 继续慢速漂浮
                // 可选：偶尔微调方向
                if (Main.rand.NextBool(60))
                {
                    Vector2 dir = Main.rand.NextVector2Circular(1f, 1f).SafeNormalize(Vector2.UnitY);
                    Projectile.velocity = dir * Main.rand.NextFloat(0.5f, 1.2f);
                }
            }
            else if(Projectile.timeLeft <= 140)
            {
                Projectile.damage = Projectile.originalDamage;
                int target = -1;
                if (TargetNPC >= 0 && TargetNPC < Main.npc.Length && Main.npc[(int)TargetNPC] != null
                    && Main.npc[(int)TargetNPC].active)
                {
                    target = (int)TargetNPC;

                }else
                {
                    float minDist = 1200f;
                    for (int k = 0; k < Main.npc.Length; k++)
                    {
                        NPC npc = Main.npc[k];
                        if (npc.CanBeChasedBy(this))
                        {
                            float dist = Vector2.Distance(Projectile.Center, npc.Center);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                target = k;
                            }
                        }
                    }
                }
                
                if (target != -1)
                {
                    // 有目标，向目标冲去
                    Vector2 toTarget = (Main.npc[target].Center - Projectile.Center).SafeNormalize(Vector2.Zero);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget * 10f, 0.12f); // 平滑加速
                }
                else
                {
                    // 没有目标，继续漂浮
                    if (Main.rand.NextBool(60))
                    {
                        Vector2 dir = Main.rand.NextVector2Circular(1f, 1f).SafeNormalize(Vector2.UnitY);
                        Projectile.velocity = dir * Main.rand.NextFloat(0.5f, 1.2f);
                    }
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
            return true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);

            int dustCount = Main.rand.Next(2, 4); // 2~3个粒子
            for (int i = 0; i < dustCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi); // 随机方向
                float speed = Main.rand.NextFloat(2f, 5f);           // 随机速度
                Vector2 velocity = angle.ToRotationVector2() * speed;

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    velocity,
                    150,
                    new Color(247, 127, 0),
                    Main.rand.NextFloat(0.8f, 1.4f)
                );
                dust.noGravity = true;
                dust.fadeIn = Main.rand.NextFloat(0.5f, 1.2f);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value;
            Rectangle rectangle = new Rectangle(
               0,
               texture_.Height / Main.projFrames[Type] * Projectile.frame,
               texture_.Width,
               texture_.Height / Main.projFrames[Type]
           );

            // 计算中心点
            Vector2 origin = new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 高光叠加（可选，叠加色调但不覆盖原Alpha）
            //for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(
                    texture_,
                    drawPos,
                    rectangle,
                    lightColor * Projectile.Opacity,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }

            return false; // 阻止默认绘制
        }
    }
    class BuffsFlyingPalladiumSword : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<FlyingPalladiumSwordProj>()] > 0)//检测玩家持有的弹幕数量
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}
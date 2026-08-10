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
using Terraria.Map;
using Terraria.ModLoader;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.Glaive_H
{
    class FlyingCobaltSword : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingCobaltSword").Value;
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingCobaltSword_").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }

        public override void SetDefaults()
        {
            //Item.CloneDefaults(ItemID.EmpressBlade);
            Item.damage = 22;
            Item.mana = 10;
            Item.width = 52;
            Item.height = 52;
            Item.useTime = 19;
            Item.useAnimation = 19;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2.5f;
            Item.value = 20000;
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<FlyingCobaltSwordProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsFlyingCobaltSword>();
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
            float intensity = 0.8f; // 控制光芒强度，越小越淡
            //Color(30, 144, 255)
            //•	R: 30 / 255 ≈ 0.13
            //•	G: 144 / 255 ≈ 0.58
            //•	B: 255 / 255 ≈ 1
            Lighting.AddLight(Item.Center, 0.13f * intensity, 0.58f * intensity, 1f * intensity);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsFlyingCobaltSword>(), 3600);
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
                .AddIngredient(ItemID.CobaltBar, 8)
                .AddTile(ModContent.TileType<BladeForgeTile>())
                .Register();
        }
    }
    public class FlyingCobaltSwordProj : ModProjectile
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/FlyingCobaltSword";
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
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsFlyingCobaltSword>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsFlyingCobaltSword>())) Projectile.Kill();

            ////修改此参数以确定攻击范围
            //var MaxDis = 500;
            var n = FindNPC(MaxDis);
            if (n >= 0 && n < Main.npc.Length || Projectile.ai[0] != 0)
            {
                // 每帧加一个小的随机值，范围可调
                //float x = Main.rand.NextFloat();
                //float slowStep = MathHelper.Lerp(0.88f, 1.2f, x * x * x * x); // x^2分布，越大概率越高
                //slowAcc += slowStep;
                //if (slowAcc < 1f)
                //    return false;
                //slowAcc -= 1f;
                return base.PreAI();
            }
            //return base.PreAI();

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
            int projType = ModContent.ProjectileType<FlyingCobaltSwordProj_P>();
            int damage = (int)(Projectile.damage); // 可根据需要调整
            float knockback = 0f;

            Vector2 spawnPos = target.Center;
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0f;

            float RotationOffset = Main.rand.NextFloat(0.1f, 3.14f);
            float ScaleOffset = Main.rand.NextFloat(0.5f, 1.2f);
            
            int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                projType,
                (int)(damage * (ScaleOffset + 0.3)),
                knockback,
                Projectile.owner,
                RotationOffset,
                ScaleOffset
            );
            Main.projectile[proj].light = 0.32f;
            clown = 30;
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
            Color LightsColor = new Color(30, 144, 255);
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
                                          MyColor * factor,
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
    public class FlyingCobaltSwordProj_P : ModProjectile
    {
        private float RotationOffset => Projectile.ai[0];
        private float ScaleOffset => Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            //ProjectileID.Sets.TrailingMode[Type] = 2;
            //ProjectileID.Sets.TrailCacheLength[Type] = 10;
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            AIType = -1; // 不用原版AI
            Projectile.penetrate = -1;
            Projectile.timeLeft = 20;
            Projectile.tileCollide = false;
            Projectile.width = Projectile.height = 40;
            Projectile.usesLocalNPCImmunity = true; // 独立无敌帧
            Projectile.localNPCHitCooldown = -1;    // 独立无敌帧时间
            Projectile.alpha = 100;
            Projectile.scale = 0.6f;
        }

        public override bool PreAI()
        {
            //Projectile.damage = 1;
            if(Projectile.timeLeft <= 10)
            {
                Projectile.timeLeft = 2;
                Projectile.scale -= 0.05f;
                if(Projectile.scale <= 0)
                {
                    Projectile.Kill();
                }
            }else if(Projectile.timeLeft >= 15)
            {
                Projectile.scale += 0.02f;
            }
            return true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.damage > 0)
                Projectile.damage = (int)(Projectile.damage * 0.8f);
            var player = Main.player[Projectile.owner];
            player.GetModPlayer<FlyingCobaltSwordPlayer>().CobaltattackspeedTimer = 120;
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value;
            Rectangle rectangle = new Rectangle(
                0,
                0, // 修正：应为0而不是texture_.Height
                texture_.Width,
                texture_.Height / Main.projFrames[Type]
            );

            // 计算中心点
            Vector2 origin = new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // 颜色叠加建议用白色或带透明度的色调
            Color blendColor = Color.White * 0.8f; // 0.8f为整体亮度，可调

            // 叠加高光色调（可选，建议用乘法而不是直接覆盖Alpha）
            Color glowColor = new Color(30, 144, 255, 0) * 0.6f;

            // 主体绘制，保留贴图原有透明度
            Main.EntitySpriteDraw(
                texture_,
                drawPos,
                rectangle,
                blendColor * Projectile.Opacity,
                Projectile.rotation + RotationOffset,
                origin,
                Projectile.scale * ScaleOffset,
                SpriteEffects.None,
                0
            );

            // 高光叠加（可选，叠加色调但不覆盖原Alpha）
            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(
                    texture_,
                    drawPos,
                    rectangle,
                    glowColor * 0.5f * Projectile.Opacity,
                    Projectile.rotation + RotationOffset,
                    origin,
                    Projectile.scale * ScaleOffset + 0.12f,
                    SpriteEffects.None,
                    0
                );
            }

            return false; // 阻止默认绘制
        }
    }
    public class FlyingCobaltSwordPlayer : ModPlayer
    {
        public int CobaltattackspeedTimer = 0;

        public override void ResetEffects()
        {
            CobaltattackspeedTimer = Math.Max(0, CobaltattackspeedTimer - 1);
        }

        public override void UpdateDead()
        {
            CobaltattackspeedTimer = 0;
        }

        public override void PostUpdate()
        {
            //if (CobaltattackspeedTimer > 0)
            //{
            //    Player.GetAttackSpeed(DamageClass.Generic) += 1.5f; // 增加150%近战攻速
            //}
        }
    }
    class BuffsFlyingCobaltSword : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.GetModPlayer<FlyingCobaltSwordPlayer>().CobaltattackspeedTimer > 0)
            {
                player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.1f; // 增加攻速
                //player.whipRangeMultiplier += 0.15f; // 增加攻击距离
            }
            
            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<FlyingCobaltSwordProj>()] > 0)//检测玩家持有的弹幕数量
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
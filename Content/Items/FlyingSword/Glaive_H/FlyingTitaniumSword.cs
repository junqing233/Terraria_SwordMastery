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
using static System.Net.WebRequestMethods;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.Glaive_H
{
    class FlyingTitaniumSword : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingTitaniumSword").Value;
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingTitaniumSword_").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }

        public override void SetDefaults()
        {
            //Item.CloneDefaults(ItemID.EmpressBlade);
            Item.damage = 31;
            Item.mana = 10;
            Item.width = 52;
            Item.height = 52;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = 20000;
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<FlyingTitaniumSwordProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsFlyingTitaniumSword>();
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
            float intensity = 0.64f; // 控制光芒强度，越小越淡
            //Color(229, 229, 229)
            //•	R: 229 / 255 ≈ 0.88
            //•	G: 229 / 255 ≈ 0.88
            //•	B: 229 / 255 ≈ 0.88
            Lighting.AddLight(Item.Center, 0.88f * intensity, 0.88f * intensity, 0.88f * intensity);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsFlyingTitaniumSword>(), 3600);
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
                .AddIngredient(ItemID.TitaniumBar, 13)
                .AddTile(ModContent.TileType<BladeForgeTile>())
                .Register();
        }
    }
    public class FlyingTitaniumSwordProj : ModProjectile
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/FlyingTitaniumSword";
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
        
        public override bool PreAI()
        {
            var player = Main.player[Projectile.owner];
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsFlyingTitaniumSword>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsFlyingTitaniumSword>())) Projectile.Kill();

            var n = FindNPC(MaxDis);
            if (n >= 0 && n < Main.npc.Length || Projectile.ai[0] != 0)
            {
                // 检查是否穿戴钛金套装
                bool hasTitaniumSet =
                    (player.armor[0].type == ItemID.TitaniumMask ||
                    player.armor[0].type == ItemID.TitaniumHelmet ||
                    player.armor[0].type == ItemID.TitaniumHeadgear) &&
                    player.armor[1].type == ItemID.TitaniumBreastplate &&
                    player.armor[2].type == ItemID.TitaniumLeggings;

                if (!hasTitaniumSet)
                {
                    // 移除相关设置
                    player.onHitTitaniumStorm = true;
                    player.titaniumStormCooldown = 1;
                    player.hasTitaniumStormBuff = true;
                }
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
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            var player = Main.player[Projectile.owner];
            player.GetModPlayer<FlyingTitaniumSwordPlayer>().TitaniumattackspeedTimer = 120;

            int projType = ProjectileID.TitaniumStormShard;

            // 统计当前玩家拥有的 FlyingTitaniumSwordProj 弹幕数量
            int flyingCount = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == ModContent.ProjectileType<FlyingTitaniumSwordProj>() && proj.owner == Projectile.owner)
                {
                    flyingCount++;
                }
            }

            // 统计当前玩家拥有的 TitaniumStormShard 弹幕数量
            int currentCount = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == projType && proj.owner == Projectile.owner)
                {
                    currentCount++;
                }
            }

            // 每多一个FlyingTitaniumSwordProj弹幕，maxCount就加5
            int maxCount = flyingCount * 5;

            if (target.CanBeChasedBy(Projectile.owner) && currentCount < maxCount)
            {
                Vector2 spawnPos = target.Center;
                Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * Main.rand.Next(-12, 11) * 0.5f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    projType,
                    Projectile.damage,
                    15f,
                    Projectile.owner
                );
            }
        }
        public override void AI()
        {
        }
        public override bool PreDrawExtras()
        {
            return base.PreDrawExtras();
        }
        
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value ;
            Rectangle rectangle = new Rectangle(
                0,
                texture_.Height / Main.projFrames[Type] * Projectile.frame,
                texture_.Width, 
                texture_.Height / Main.projFrames[Type]
                );
            //var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
            //var lig = lightColor.ToVector3().Length() / 1.75f;
            //var v3 = Main.rgbToHsl(Color.OrangeRed);
            //v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.1f;
            //var c = Main.hslToRgb(v3) * lig;
            //c.A = 0;
            // 使用自定义颜色
            Color LightsColor = new Color(229, 229, 229);
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
                                          oldRo,
                                          new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                                          Projectile.scale * 1.5f * factor,
                                          SpriteEffects.None,
                                          0);
                }
            }

            Main.EntitySpriteDraw(
                texture_,
                Projectile.Center - Main.screenPosition,
                rectangle,
                lightColor,
                Projectile.rotation - MathHelper.PiOver2 + MathHelper.PiOver4,
                new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                Projectile.scale * 1.5f,
                SpriteEffects.None,
                0
                );
            #region 以下：渐变高光

            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(texture_,
                                      Projectile.Center - Main.screenPosition,
                                      rectangle,
                                      c * value * 0.6f,
                                      Projectile.rotation - MathHelper.PiOver2 + MathHelper.PiOver4,
                                      new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                                      Projectile.scale * 1.5f,
                                      SpriteEffects.None,
                                      0);
            }
            #endregion
            return false; // 阻止默认绘制
        }
    }
    public class FlyingTitaniumSwordPlayer : ModPlayer
    {
        public int TitaniumattackspeedTimer = 0;

        public override void ResetEffects()
        {
            TitaniumattackspeedTimer = Math.Max(0, TitaniumattackspeedTimer - 1);
        }

        public override void UpdateDead()
        {
            TitaniumattackspeedTimer = 0;
        }
    }
    class BuffsFlyingTitaniumSword : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.GetModPlayer<FlyingTitaniumSwordPlayer>().TitaniumattackspeedTimer > 0)
            {
                player.endurance += 0.1f; // 减少所受伤害10%
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.1f; // 增加10%攻击力
            }

            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<FlyingTitaniumSwordProj>()] > 0)//检测玩家持有的弹幕数量
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
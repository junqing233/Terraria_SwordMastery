using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using SwordMastery.Content.Items.Mterial;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;

namespace SwordMastery.Content.Items.Weapons.Yoyo
{
    public class VoidFirmamentPlayer : ModPlayer
    {
        public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
        {
            // 判断环境
            bool isCondition = Player.ZoneBeach && !attempt.inLava && !attempt.inHoney;

            if (isCondition && Main.rand.NextBool(10))
            {
                itemDrop = ModContent.ItemType<VoidFirmamentYoyo>();
            }
        }
    }
    public class VoidFirmamentYoyo : ModItem
    {
        public override void SetStaticDefaults()
        {
            
            ItemID.Sets.Yoyo[Item.type] = true;
            ItemID.Sets.GamepadExtraRange[Item.type] = 15;
            ItemID.Sets.GamepadSmartQuickReach[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.noMelee = true; 
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.damage = 36;
            Item.DamageType = DamageClass.MeleeNoSpeed; 
            Item.knockBack = 2.5f; 
            Item.crit = 8; 
            Item.channel = true; 
            Item.rare = ItemRarityID.Green; 
            Item.value = Item.buyPrice(gold: 1); 

            Item.shoot = ModContent.ProjectileType<VoidFirmamentYoyoProj>(); 
            Item.shootSpeed = 16f;	
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "VoidFirmamentYoyo", "神兵") { OverrideColor = Color.IndianRed });
        }

        private static readonly int[] unwantedPrefixes = new int[] { PrefixID.Terrible, PrefixID.Dull, PrefixID.Shameful, PrefixID.Annoying, PrefixID.Broken, PrefixID.Damaged, PrefixID.Shoddy };

        public override bool AllowPrefix(int pre)
        {
            if (Array.IndexOf(unwantedPrefixes, pre) > -1)
            {
                return false;
            }
            return true;
        }
        public override void AddRecipes()
        {
            //CreateRecipe()
            //   .AddIngredient(ModContent.ItemType<ThunderCrystal>(), 1) // 雷电晶
            //   .AddIngredient(ModContent.ItemType<PokeBall>(), 1) // 精灵球
            //   .AddIngredient(ItemID.WoodYoyo, 1) // 木悠悠球
            //   .AddTile(TileID.Anvils) // 铁砧
            //   .Register();
        }
    }
    public class VoidFirmamentYoyoProj : ModProjectile
    {
        //private int TimerCounter = 0;//计数器
        private int attractCooldown = 0; // 吸引冷却计数器

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Projectile.type] = 18f;
            ProjectileID.Sets.YoyosMaximumRange[Projectile.type] = 500f;
            ProjectileID.Sets.YoyosTopSpeed[Projectile.type] = 13f;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;//拖尾效果长度
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;//]e拖尾模式
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.aiStyle = ProjAIStyleID.Yoyo;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            float attractRadius = 240f;
            float attractStrength = 2f;
            float lockRadius = 16f; // 距离中心小于此值时锁定

            Vector2 center = Projectile.Center;

            if (attractCooldown > 0)
            {
                attractCooldown--;
                return;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active /*&& !npc.friendly*/ && !npc.dontTakeDamage && !npc.immortal)
                {
                    float dist = Vector2.Distance(npc.Center, center);
                    if (dist < attractRadius)
                    {
                        if (dist < lockRadius)
                        {
                            // 锁定敌怪在中心
                            npc.velocity = Vector2.Zero;
                            npc.position = center - npc.Size / 2f;
                        }
                        else
                        {
                            Vector2 direction = center - npc.Center;
                            if (direction != Vector2.Zero)
                            {
                                direction.Normalize();
                                float force = attractStrength * (1f - dist / attractRadius);
                                npc.velocity += direction * force;
                            }
                        }
                    }
                }
            }

            attractCooldown = 10; // 设置冷却
        }

        //主弹射物绘制残影
        public override bool PreDraw(ref Color lightColor)
        {
            // 让漩涡缓缓转动
            float vortexRotation = (float)(Main.GameUpdateCount * 0.24f);
            Texture2D vortexTexture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Yoyo/VoidFirmamentYoyoProj_").Value;

            // 绘制漩涡拖尾残影（最底层）
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2;
                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length * 0.6f; // 残影更淡
                float scale = Projectile.scale * (0.9f - i * 0.05f);
                float oldRotation = -vortexRotation + i * 1.15f; // 每个残影有旋转偏移

                Main.EntitySpriteDraw(
                    vortexTexture,
                    drawPos,
                    null,
                    lightColor * opacity,
                    oldRotation,
                    new Vector2(vortexTexture.Width, vortexTexture.Height) / 2,
                    scale,
                    SpriteEffects.None,
                    0
                );
            }

            // 绘制漩涡（当前帧）
            Main.EntitySpriteDraw(
                vortexTexture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor * 0.8f,
                -vortexRotation,
                new Vector2(vortexTexture.Width, vortexTexture.Height) / 2,
                Projectile.scale * 1f,
                SpriteEffects.None,
                0
            );

            // 绘制主弹射物拖尾残影
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 drawPos = Projectile.oldPos[i] - Main.screenPosition + Projectile.Size / 2;
                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length * 0.7f;
                Color color = lightColor * opacity;

                Main.EntitySpriteDraw(
                    ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Yoyo/VoidFirmamentYoyoProj").Value,
                    drawPos,
                    null,
                    color,
                    Projectile.rotation,
                    Projectile.Size / 2,
                    Projectile.scale * (0.9f - i * 0.05f),
                    SpriteEffects.None,
                    0
                );
            }
            // 绘制主弹射物（中间层）
            Main.EntitySpriteDraw(
                ModContent.Request<Texture2D>(Texture).Value,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                new Vector2(Projectile.width, Projectile.height) / 2,
                Projectile.scale,
                SpriteEffects.None,
                0
            );
            return false;
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Weapons.Whip
{
    public class StarfuryWhipCrateDrop : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            // 天空匣和天蓝匣
            if (item.type == ItemID.FloatingIslandFishingCrate || item.type == ItemID.FloatingIslandFishingCrateHard)
            {
                // 概率掉落
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<StarfuryWhip>(), 4));
            }
        }
    }
    public class StarfuryWhipChestLootSystem : ModSystem
    {
        public override void PostWorldGen()
        {
            int skywareChestItemType = ModContent.ItemType<StarfuryWhip>();
            for (int chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
            {
                Chest chest = Main.chest[chestIndex];
                if (chest == null) continue;

                // 天域箱的类型是 ChestType.Skyware
                if (Main.tile[chest.x, chest.y].TileType == TileID.Containers &&
                    Main.tile[chest.x, chest.y].TileFrameX == 13 * 36) // 13号帧是天域箱
                {
                    // 1/4 概率
                    if (Main.rand.NextBool(4))
                    {
                        // 找到第一个空槽
                        for (int i = 0; i < chest.item.Length; i++)
                        {
                            if (chest.item[i].type == ItemID.None)
                            {
                                chest.item[i].SetDefaults(skywareChestItemType);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
    public class StarfuryWhip : ModItem
    {
        //private int lastShotTime = -1000;
        // 设置物品的提示文本，使用特定的标签格式化
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(WhipDebuff.TagDamage);

        public override void SetDefaults()
        {
            // 此方法快速设置鞭子的属性。
            // 将鼠标悬停在方法上以查看其参数。
            Item.DefaultToWhip(ModContent.ProjectileType<StarfuryWhipProj>(), 25, 2, 4);
            Item.useTime = 26;
            Item.useAnimation = 26;
           
            Item.rare = ItemRarityID.Green;
            Item.channel = true; // 允许持续使用鞭子
            Item.width = 36;
            Item.height = 36;
        }
        
        private int UpdateCount = 0;

        private bool isIncreasing = true; // 新增方向标记

        public override void UpdateInventory(Player player)
        {
            if (isIncreasing)
            {
                UpdateCount += 1;
                if (UpdateCount >= 120)
                {
                    isIncreasing = false; // 到达上限切换方向
                }
            }
            else
            {
                UpdateCount -= 1;
                if (UpdateCount <= 0)
                {
                    isIncreasing = true; // 到达下限切换方向
                }
            }
        }
        // 在类内添加颜色渐变控制字段
        private float ColorProgress => UpdateCount / 150f; // 获取0-1的渐变进度

        public override void PostDrawTooltipLine(DrawableTooltipLine line)
        {
            if (line.Mod == "Terraria" && line.Name == "ItemName")
            {
                Vector2 position = new Vector2(line.X, line.Y);
                //// 武器名称渐变参数调整
                Color gradientColor2 = Color.Lerp(Color.DeepPink, Color.LightPink, ColorProgress);

                // 绘制渐变主体
                Utils.DrawBorderString(
                    Main.spriteBatch,
                    line.Text,
                    position,
                    gradientColor2
                );
            }
        }

        public override void HoldItem(Player player)
        {
            base.HoldItem(player);
            var modPlayer = player.GetModPlayer<StarfuryWhipPlayer>();
            if (modPlayer.whipDebuffActive)
            {
                // 粒子和音效
                Vector2 gorePos = player.position;
                for (int i = 0; i < 4; i++)
                {
                    int dust = Dust.NewDust(gorePos, player.width, player.height, DustID.ManaRegeneration, 0, 0, 220, default, 2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].alpha = 220;
                    Main.dust[dust].fadeIn = 0.1f;
                    Main.dust[dust].velocity *= 0.2f;
                }
                
                SoundEngine.PlaySound(SoundID.MaxMana);
                modPlayer.whipDebuffActive = false;
            }
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var modPlayer = player.GetModPlayer<StarfuryWhipPlayer>();

            // 额外弹幕冷却判定
            if (modPlayer.starfuryWhipCooldown <= 0)
            {
                modPlayer.starfuryWhipCooldown = (int)(0.67f * 60f);

                Vector2 mouseWorld = Main.MouseWorld;

                // 生成在屏幕最上方
                Vector2 spawnPos = mouseWorld;
                spawnPos.Y = player.Center.Y - 600f;
                float randomOffsetX = Main.rand.NextFloat(-200f, 0) * player.direction;
                spawnPos.X += randomOffsetX;

                // 计算朝向鼠标的速度
                Vector2 toMouse = mouseWorld - spawnPos;
                if (toMouse != Vector2.Zero)
                    toMouse.Normalize();
                Vector2 starVelocity = toMouse * 24f;

                // 发射额外弹幕（星怒）
                Projectile.NewProjectile(
                    source,
                    spawnPos,
                    starVelocity,
                    ProjectileID.Starfury,
                    damage,
                    knockback,
                    player.whoAmI,
                    mouseWorld.Y,
                    mouseWorld.Y,
                    mouseWorld.Y
                );
            }

            // 始终允许本体弹幕发射
            return true;
        }
        // 使鞭子能够接受近战前缀
        public override bool MeleePrefix()
        {
            return true;
        }
    }
    public class StarfuryWhipPlayer : ModPlayer
    {
        public int starfuryWhipCooldown = 0;
        public bool whipDebuffActive = false;
        private int lastCooldown = 0;

        public override void ResetEffects()
        {
            // 只在冷却从正变成0时触发
            whipDebuffActive = false;
            if (starfuryWhipCooldown > 0)
            {
                starfuryWhipCooldown--;
            }
            if (lastCooldown > 0 && starfuryWhipCooldown == 0)
            {
                whipDebuffActive = true;
            }
            lastCooldown = starfuryWhipCooldown;
        }
    }
    public class StarfuryWhipProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // 这使得投射物使用鞭子碰撞检测，并允许药剂瓶应用于它。
            ProjectileID.Sets.IsAWhip[Type] = true;
        }

        public override void SetDefaults()
        {
            // 此方法快速设置鞭子的属性。
            Projectile.DefaultToWhip();

            // 使用这些来更改默认的鞭子属性
            Projectile.WhipSettings.Segments = 12;// 鞭子的段数
            Projectile.WhipSettings.RangeMultiplier = 0.8f;// 鞭子的范围倍率
        }

        private float Timer
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private float ChargeTime
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<WhipDebuff>(), 240);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            Projectile.damage = (int)(Projectile.damage * 0.8f); // 多次击中惩罚。鞭子击中敌人越多，伤害越低。
        }

        // 此方法在鞭子的所有点之间绘制线条，以防精灵图之间有空隙。
        private void DrawLine(List<Vector2> list)
        {
            Texture2D texture = TextureAssets.FishingLine.Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = new Vector2(frame.Width / 2, 2);

            Vector2 pos = list[0];
            for (int i = 0; i < list.Count - 1; i++)
            {
                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;

                float rotation = diff.ToRotation() - MathHelper.PiOver2;
                Color color = Lighting.GetColor(element.ToTileCoordinates(), Color.Yellow);
                Vector2 scale = new Vector2(1, (diff.Length() + 2) / frame.Height);

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

                pos += diff;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            List<Vector2> list = new List<Vector2>();
            Projectile.FillWhipControlPoints(Projectile, list);
            DrawLine(list);
            SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 pos = list[0];

            for (int i = 0; i < list.Count - 1; i++)
            {
                Rectangle frame = new Rectangle(0, 8, 26, 18);
                Vector2 origin = new Vector2(13, 0);
                float scale = 1;

                if (i == list.Count - 2)
                {
                    frame.Y = 68;
                    frame.Height = 16;
                    Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
                    float t = Timer / timeToFlyOut;
                    scale = MathHelper.Lerp(0.5f, 1.2f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));

                    // 只在动画初期生成gore，避免重复生成
                    if (t < 0.8f && !goreSpawned && Main.rand.NextBool(5))
                    {
                        goreSpawned = true;
                        Vector2 gorePos = pos;
                        Vector2 goreVel = Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(2f, 2f);

                        Gore.NewGore(Projectile.GetSource_FromThis(), gorePos, goreVel, 16, 1f);
                        Gore.NewGore(Projectile.GetSource_FromThis(), gorePos, goreVel, 17, 1f);
                        Dust.NewDust(gorePos, 1, 1, DustID.Enchanted_Gold, goreVel.X, goreVel.Y, 1, default, 1f);
                        Dust.NewDust(gorePos, 1, 1, DustID.Enchanted_Pink, goreVel.X, goreVel.Y, 100, default, 1f);
                    }
                    else if (t >= 0.6f)
                    {
                        goreSpawned = false; // 重置，便于下次挥鞭时再次生成
                    }
                }
                else if (i > 10)
                {
                    frame.Y = 54;
                    frame.Height = 14;
                }
                else if (i > 5)
                {
                    frame.Y = 40;
                    frame.Height = 14;
                }
                else if (i > 0)
                {
                    frame.Y = 26;
                    frame.Height = 14;
                }

                Vector2 element = list[i];
                Vector2 diff = list[i + 1] - element;
                float rotation = diff.ToRotation() - MathHelper.PiOver2;
                Color color = Lighting.GetColor(element.ToTileCoordinates());

                Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale * 1f, flip, 0);
                pos += diff;
            }
            return false;
        }

        // 在类内添加字段，防止gore重复生成
        private bool goreSpawned = false;
    }
}

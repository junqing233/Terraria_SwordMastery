using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.Accessories;
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
using Terraria.ModLoader.IO;

namespace SwordMastery.Content.Items.Weapons.Miscellaneous
{
    public class FlameCross : ModItem
    {
        private readonly Texture2D bossHead = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Miscellaneous/NPC_Head_Boss_8").Value;
        // 物品ID与弹幕ID的映射
        private static readonly Dictionary<int, int> itemToProj = new()
        {
            { 321, 43 },
            { 1173, 201 }, { 1174, 202 }, { 1175, 203 }, { 1176, 204 }, { 1177, 205 },
            { 3229, 527 }, { 3230, 528 }, { 3231, 529 }, { 3232, 530 }, { 3233, 531 }
        };
        public override void SetDefaults()
        {
            Item.damage = 50;
            //Item.crit = 1;
            Item.DamageType = DamageClass.Magic;
            Item.width = 30;
            Item.height = 40;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(0, 0, 1, 4);
            Item.rare = ItemRarityID.Green;
            //Item.UseSound = SoundID.Item1;
            Item.autoReuse = true; // 自动使用
            //Item.useTurn = true; // 自动转向
            Item.noUseGraphic = false; // 显示使用图标
            Item.noMelee = true;// 确保物品在挥动动画中不造成伤害
            Item.shoot = ProjectileID.HelFire;
            Item.shootSpeed = 12;
            Item.mana = 8;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string keyText = "击败     后，在聊天框中输入“圣焰裁决”，改变此十字架！";
            tooltips.Add(new TooltipLine(Mod, "FlameCrossKey", keyText) { OverrideColor = Color.White });
        }
        public override void PostDrawTooltip(ReadOnlyCollection<DrawableTooltipLine> lines)
        {
            base.PostDrawTooltip(lines);
            int x = 0;
            int y = 0;
            int width = 34;
            int height = 36;
            Rectangle sourceRectangle = new Rectangle(x, y, width, height);
            Vector2 drawPosition = new Vector2(Main.mouseX + 65, Main.mouseY + 338);
            Main.spriteBatch.Draw(bossHead, drawPosition, sourceRectangle, Color.White * 0.8f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // 统计墓碑数量
            int tombstoneCount = 0;
            int[] tombstoneItemIDs = new int[]
            {
                ItemID.Tombstone, ItemID.GraveMarker, ItemID.CrossGraveMarker, ItemID.Headstone,
                ItemID.Gravestone, ItemID.Obelisk, ItemID.RichGravestone1, ItemID.RichGravestone2,
                ItemID.RichGravestone3, ItemID.RichGravestone4, ItemID.RichGravestone5
            };
            var player = Main.LocalPlayer;
            foreach (var item in player.inventory)
            {
                if (item != null && !item.IsAir && tombstoneItemIDs.Contains(item.type))
                    tombstoneCount += item.stack;
            }

            // 绘制数量（右下角）
            if (tombstoneCount >= 0)
            {
                string text = tombstoneCount.ToString();
                Vector2 textPos = position + new Vector2(frame.Width - 45, frame.Height - 36);
                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.ItemStack.Value,
                    text,
                    textPos.X, textPos.Y,
                    Color.IndianRed,
                    Color.Black,
                    Vector2.Zero,
                    0.9f
                );
            }
        }
        public override bool AltFunctionUse(Player player)// 右键
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                int radius = 240;
                int tileRadius = radius / 16;
                Point playerTile = player.Center.ToTileCoordinates();

                // 墓碑类型列表（原版所有墓碑）
                int[] tombstoneTypes = new int[]
                {
                    TileID.Tombstones
                };

                // 墓碑弹幕与物品映射
                Dictionary<int, int> projToItem = new()
                {
                    { ProjectileID.Tombstone, ItemID.Tombstone },
                    { ProjectileID.GraveMarker, ItemID.GraveMarker },
                    { ProjectileID.CrossGraveMarker, ItemID.CrossGraveMarker },
                    { ProjectileID.Headstone, ItemID.Headstone },
                    { ProjectileID.Gravestone, ItemID.Gravestone },
                    { ProjectileID.Obelisk, ItemID.Obelisk },
                    { ProjectileID.RichGravestone1, ItemID.RichGravestone1 },
                    { ProjectileID.RichGravestone2, ItemID.RichGravestone2 },
                    { ProjectileID.RichGravestone3, ItemID.RichGravestone3 },
                    { ProjectileID.RichGravestone4, ItemID.RichGravestone4 },
                    { ProjectileID.RichGravestone5, ItemID.RichGravestone5 }
                };

                // 1. 消除墓碑瓦片
                for (int x = playerTile.X - tileRadius; x <= playerTile.X + tileRadius; x++)
                {
                    for (int y = playerTile.Y - tileRadius; y <= playerTile.Y + tileRadius; y++)
                    {
                        if (WorldGen.InWorld(x, y, 10))
                        {
                            var tile = Main.tile[x, y];
                            if (tile != null && tile.HasTile && tombstoneTypes.Contains(tile.TileType))
                            {
                                Vector2 worldPos = new Vector2(x * 16 + 8, y * 16 + 8);
                                WorldGen.KillTile(x, y, false, false, true);
                                if (Main.netMode == NetmodeID.MultiplayerClient)
                                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);
                                // 粒子效果
                                for (int d = 0; d < 12; d++)
                                {
                                    Dust dust = Dust.NewDustPerfect(worldPos, DustID.RedTorch, Main.rand.NextVector2Circular(2, 2), 100, Color.Orange, 1.5f);
                                    dust.noGravity = false;
                                }
                            }
                        }
                    }
                }

                // 2. 消除墓碑弹幕
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == player.whoAmI && proj.Center.Distance(player.Center) <= radius)
                    {
                        if (projToItem.TryGetValue(proj.type, out int itemType))
                        {
                            Vector2 projPos = proj.Center;
                            proj.Kill();
                            // 生成墓碑物品
                            Item.NewItem(source, projPos, 16, 16, itemType);
                            // 粒子效果
                            for (int d = 0; d < 12; d++)
                            {
                                Dust dust = Dust.NewDustPerfect(projPos, DustID.RedTorch, Main.rand.NextVector2Circular(2, 2), 100, Color.Orange, 1.5f);
                                dust.noGravity = false;
                            }
                        }
                    }
                }

                // 环形火焰粒子
                int dustCount = 128;
                float circleRadius = 240f;
                for (int i = 0; i < dustCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / dustCount;
                    Vector2 dustPos = player.Center + circleRadius * angle.ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.RedTorch, Vector2.Zero, 10, Color.Orange, 2f);
                    dust.noGravity = false;
                    dust.velocity = (dustPos - player.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 4f);
                }
                SoundEngine.PlaySound(SoundID.Item74, player.Center);
            }
            else
            {
                // 左键：遍历背包，查找可用物品
                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item item = player.inventory[i];
                    if (item != null && !item.IsAir && itemToProj.TryGetValue(item.type, out int projType))
                    {
                        // 发射弹幕
                        // 发射自定义弹幕并传递属性
                        int proj = Projectile.NewProjectile(source, player.MountedCenter + new Vector2(0, -10), velocity, projType, damage, knockback, player.whoAmI);
                        Main.projectile[proj].friendly = true;
                        Main.projectile[proj].hostile = true;

                        player.immune = true;// 玩家无敌
                        player.immuneTime = 10; // 确保无敌时间短于冲刺持续时间

                        SoundEngine.PlaySound(SoundID.Item100, player.Center);
                        // 消耗物品
                        item.stack--;
                        if (item.stack <= 0)
                            item.TurnToAir();
                        // 只发射一个，跳出循环
                        break;
                    }
                }
            }

            return false; // 不发射弹幕
        }
        public override bool MeleePrefix()
        {
            return false;
        }
        public override void Update(ref float gravity, ref float maxFallSpeed)
        {
            gravity = 0f; // 无重力
            Item.velocity.Y = (float)Math.Sin(Main.GameUpdateCount * 0.04f) * 0.3f; // 上下漂浮

            if (Main.GameUpdateCount % 120 == 0)
            {
                // 环形火焰粒子
                int dustCount = 12;
                float circleRadius = 20f;
                for (int i = 0; i < dustCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / dustCount;
                    Vector2 dustPos = Item.Center + circleRadius * angle.ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.RedTorch, Vector2.Zero, 10, Color.Orange, 2f);
                    dust.noGravity = false;
                    dust.velocity = (dustPos - Item.Center).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.2f, 1f);
                }
            }
        }
        public override bool CanPickup(Player player)
        {
            if (player.dead) return false;
            return base.CanPickup(player);
        }
    }
    public class FlameCrossPlayer : ModPlayer
    {
        public bool hasGotFlameCross = false;
        public int flameBlessingTimer = 0; // 特效计时器

        public override void SaveData(TagCompound tag)
        {
            tag["hasGotFlameCross"] = hasGotFlameCross;
        }

        public override void LoadData(TagCompound tag)
        {
            hasGotFlameCross = tag.GetBool("hasGotFlameCross");
        }
        public override void PostUpdate()
        {
            if (flameBlessingTimer > 0)
            {
                flameBlessingTimer--;
                // 生成火焰粒子
                Player player = Player;
                int dustCount = 10;
                for (int i = 0; i < dustCount; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, DustID.Torch);
                    Main.dust[dustIndex].velocity *= 0.2f;
                    Main.dust[dustIndex].scale *= 1.5f;
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity.Y -= 5f;
                    Main.dust[dustIndex].color = Color.Red;
                }
            }
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            // 4%概率，且只生成一次
            if (!hasGotFlameCross && Main.rand.NextFloat() < 0.04f)
            {
                Vector2 spawnPos = Player.Center;
                int itemType = ModContent.ItemType<FlameCross>();
                int itemIndex = Item.NewItem(Player.GetSource_Misc("FlameCrossDeath"), spawnPos, 32, 32, itemType);
                if (itemIndex >= 0 && itemIndex < Main.maxItems)
                {
                    Main.item[itemIndex].velocity = new Vector2(0, -2f); // 让物品初始向上漂浮
                }
                hasGotFlameCross = true; // 标记为已获得
            }
        }
    }
    public class EvilIntentionSystem : ModSystem
    {
        public override void PostUpdateInput()
        {
            if (NPC.downedMoonlord)
            {
                // 检查聊天框内容
                if (Main.LocalPlayer.HeldItem.type == ModContent.ItemType<FlameCross>()
                    && Main.chatText.Trim() == "圣焰裁决")
                {
                    Player player = Main.LocalPlayer; // 获取本地玩家

                    int itemType = ModContent.ItemType<FlameCross_Acc>(); // 指定生成的物品类型
                    player.HeldItem.SetDefaults(itemType);
                    // 显示提示信息
                    Main.NewText("你已获得圣神十字架的祝福！", Color.Orange);
                    // 触发2秒特效
                    player.GetModPlayer<FlameCrossPlayer>().flameBlessingTimer = 120;

                    // 清空聊天框内容
                    Main.chatText = "";
                }
                else if (Main.LocalPlayer.HeldItem.type != ModContent.ItemType<FlameCross>()
                    && Main.chatText.Trim() == "圣焰裁决")
                {
                    // 显示提示信息
                    Main.NewText("你必须拿着十字架已彰显你的虔诚……", Color.OrangeRed);
                    // 清空聊天框内容
                    Main.chatText = "";
                }
            }
        }
    }
}

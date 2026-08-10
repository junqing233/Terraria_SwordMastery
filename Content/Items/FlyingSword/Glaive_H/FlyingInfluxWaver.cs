using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.FlyingSword.BladeForge;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.Glaive_H
{
    class FlyingInfluxWaver : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingInfluxWaver").Value;
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingInfluxWaver_").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }

        public override void SetDefaults()
        {
            //Item.CloneDefaults(ItemID.EmpressBlade);
            Item.damage = 50;
            Item.mana = 10;
            Item.width = 48;
            Item.height = 72;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = 20000;
            Item.rare = ItemRarityID.Yellow;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<FlyingInfluxWaverProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsFlyingInfluxWaver>();
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
            float intensity = 1.2f; // 控制光芒强度，越小越淡
            //Color(172, 232, 242)
            //•	R: 172 / 255 ≈ 0.675
            //•	G: 232 / 255 ≈ 0.9
            //•	B: 242 / 255 ≈ 0.95
            Lighting.AddLight(Item.Center, 0.675f * intensity, 0.9f * intensity, 0.95f * intensity);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsFlyingInfluxWaver>(), 3600);
            player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.InfluxWaver, 1)
                .AddTile(ModContent.TileType<BladeForgeTile>())
                .Register();
        }
    }
    public class FlyingInfluxWaverProj : ModProjectile
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/FlyingInfluxWaver";
        NPC targetNPC = null;
        private int existTimer = 0; // 计时器
        public bool IsDerivedFromZenith = false; // 标记是否为天顶剑衍生
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
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
            Projectile.timeLeft = 2;
            Projectile.height = Projectile.width = 10;
            Projectile.minionSlots = 1;
            Projectile.light = 0.2f;
            Projectile.extraUpdates = 0; // 0为正常速度，-1为更慢（tModLoader允许为负数）
        }

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            existTimer = 0; // 初始化计时器
            IsDerivedFromZenith = false;
        }
        private readonly int MaxDis = 800;
        public override bool MinionContactDamage()
        {
            if (FindNPC(MaxDis) > 0 && FindNPC(MaxDis) < Main.npc.Length || Projectile.ai[0] != 0)
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
            
            int parentWhoAmI = (int)Projectile.ai[2];
            bool parentAlive = false;
            if (parentWhoAmI >= 0 && parentWhoAmI < Main.maxProjectiles)
            {
                Projectile parent = Main.projectile[parentWhoAmI];
                if (parent.active && parent.type == ModContent.ProjectileType<FlyingZenithProj>())
                {
                    parentAlive = true;
                }
            }

            if (parentAlive)
            {
                existTimer++;
                if (existTimer > 420) // 7秒（60帧*7）
                {
                    Projectile.Kill();
                    return false;
                }
                Projectile.timeLeft = 2; // 本体存在，刷新生命周期
            }
            else
            {
                if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsFlyingInfluxWaver>()))
                {
                    Projectile.timeLeft = 2;
                }
                if (!player.HasBuff(ModContent.BuffType<BuffsFlyingInfluxWaver>()) 
                    || IsDerivedFromZenith) Projectile.Kill();
            }
            //修改此参数以确定攻击范围
            var n = FindNPC(MaxDis);
            if (n >= 0 && n < Main.npc.Length || Projectile.ai[0] != 0)
            {
                if (targetNPC != null && targetNPC.active && (Projectile.ai[0] == 68 || Projectile.ai[0] == 1))
                {
                    int projType = ModContent.ProjectileType<FlyingSeedlerProj_P>();
                    int damage = (int)(Projectile.damage * Main.rand.NextFloat(0.7f, 0.9f)); // 可根据需要调整
                    float knockback = 2f;
                    //随机值
                    float rand = Main.rand.NextFloat(0.5f, 2f);
                    Vector2 velocity = (targetNPC.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 21f; // 朝向目标中心
                    int Proj = Projectile.NewProjectile(
                         Projectile.GetSource_FromThis(),
                         Projectile.Center,
                         velocity,
                         ProjectileID.InfluxWaver,
                         damage,
                         knockback,
                         Projectile.owner
                    );
                    Main.projectile[Proj].DamageType = DamageClass.Summon;
                    //Main.projectile[Proj].usesLocalNPCImmunity = true;
                    //Main.projectile[Proj].localNPCHitCooldown = 2;
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
            targetNPC = target;
            var player = Main.player[Projectile.owner];
            player.GetModPlayer<FlyingInfluxWaverPlayer>().InfluxWaverattackspeedTimer = 120;
        }
        public override bool PreDrawExtras()
        {
            return base.PreDrawExtras();
        }
        private bool flag = false;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value;
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
            // 使用自定义颜色
            Color LightsColor = new Color(172, 232, 242);
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
    
    public class FlyingInfluxWaverPlayer : ModPlayer
    {
        public int InfluxWaverattackspeedTimer = 0;

        public override void ResetEffects()
        {
            InfluxWaverattackspeedTimer = Math.Max(0, InfluxWaverattackspeedTimer - 1);
        }

        public override void UpdateDead()
        {
            InfluxWaverattackspeedTimer = 0;
        }
    }
    class BuffsFlyingInfluxWaver : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.GetModPlayer<FlyingInfluxWaverPlayer>().InfluxWaverattackspeedTimer > 0)
            {
                player.GetDamage(DamageClass.Summon) += 0.15f; // 增加15%召唤伤害
                player.GetArmorPenetration(DamageClass.Summon) += 20; // 增加15点近战穿甲
            }

            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<FlyingInfluxWaverProj>()] > 0)//检测玩家持有的弹幕数量
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
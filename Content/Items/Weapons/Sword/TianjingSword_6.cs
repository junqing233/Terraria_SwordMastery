using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Weapons.Sword
{
    public class TianjingSword_6 : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 128; // 基础伤害
            Item.crit = 2; // 爆击率
            Item.DamageType = DamageClass.MeleeNoSpeed; // 伤害类型
            Item.width = 48; // 物品宽度
            Item.height = 60; // 物品高度
            Item.useTime = 25; // 使用时间
            Item.useAnimation = 25; // 使用动画时间
            Item.knockBack = 6; // 击退
            Item.value = Item.buyPrice(99, 0, 0, 0); // 物品价值
            Item.rare = ItemRarityID.LightRed; // 稀有度
            Item.autoReuse = true; // 自动使用
            Item.shoot = ModContent.ProjectileType<TianjingSword_6Proj_>(); // 射击类型
            Item.shootSpeed = 1f; // 射击速度

            /// 修改部分
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.HiddenAnimation;
        }
        public static bool isa = false;
        private bool isExtraExercise = false; // 是否额外增加
        // 修改物品提示信息
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Language.ActiveCulture.Name == "zh-Hans")
            {
                if (isa)
                {
                    tooltips.Add(new TooltipLine(Mod, "", "左键进行四段斩击，第四段斩击释放" + "[c/fd3a4a:神龙魄]"));
                    tooltips.Add(new TooltipLine(Mod, "", "右键蓄力斩击，释放" + "[c/02866f:神龙游]"));
                }
                else
                {
                    tooltips.Add(new TooltipLine(Mod, "", "左键进行四段斩击，第四段斩击(需同时点击右键)释放" + "[c/fcd917:神龙魄]"));
                    tooltips.Add(new TooltipLine(Mod, "", "右键蓄力斩击，释放" + "[c/00b9fb:神龙游]"));
                }

                tooltips.Add(new TooltipLine(Mod, "", "神龙头部可造成基于敌人最大生命值的真实伤害"));
                tooltips.Add(new TooltipLine(Mod, "", "斩击可击退敌人和弹幕"));

                if (isa)
                    tooltips.Add(new TooltipLine(Mod, "", "右键物品关闭" + "[c/fd3a4a:神龙魄]" + "限制"));
                else
                    tooltips.Add(new TooltipLine(Mod, "", "右键物品关闭" + "[c/fcd917:神龙魄]" + "限制"));
            }
            else
            {
                if (isa)
                    tooltips.Add(new TooltipLine(Mod, "", "Left click: Four-stage slash, the fourth slash releases Dragon Soul"));
                else
                    tooltips.Add(new TooltipLine(Mod, "", "Left click: Four-stage slash, the fourth slash (requires right click at the same time) releases Divine Dragon Soul"));
                tooltips.Add(new TooltipLine(Mod, "", "Right click: Charge slash, releases Divine Dragon Swim"));
                tooltips.Add(new TooltipLine(Mod, "", "Divine Dragon head deals true damage based on enemy's max life"));
                tooltips.Add(new TooltipLine(Mod, "", "Slashes can knock back enemies and projectiles"));

                var openTooltip = (new TooltipLine(Mod, "", "Right click the item to toggle Dragon Soul restriction"));
                tooltips.Add(openTooltip);
            }
        }
        // 合成材料
        public override void AddRecipes()
        {
            CreateRecipe()
              .AddIngredient(ModContent.ItemType<TianjingSword>(), 1)
              .AddIngredient(ModContent.ItemType<TianjingSword_0>(), 1)
              .AddIngredient(ModContent.ItemType<TianjingSword_1>(), 1)
              .AddIngredient(ModContent.ItemType<TianjingSword_2>(), 1)
              .AddIngredient(ModContent.ItemType<TianjingSword_3>(), 1)
              .AddIngredient(ModContent.ItemType<TianjingSword_4>(), 1)
              .AddIngredient(ModContent.ItemType<TianjingSword_5>(), 1)
              .AddTile(TileID.MythrilAnvil)//秘银砧
              .Register();
        }
        public override bool CanRightClick()
        {
            if (Main.mouseRight && !isExtraExercise)
            {
                if (Main.mouseRightRelease)
                {
                    isa = !isa;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Grab); // 播放音效
                }
            }
            isExtraExercise = Main.mouseRightRelease;

            return false;
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            var p = Projectile.NewProjectileDirect(source,
                                                    position,
                                                    velocity,
                                                    ModContent.ProjectileType<TianjingSword_6Proj_>(),
                                                    damage,
                                                    knockback,
                                                    Main.myPlayer).ModProjectile as TianjingSword_6Proj_;

            if (player.altFunctionUse == 2)
                p.attackType = AttackType.right;
            else
                p.attackType = AttackType.left;

            return false;
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // 获取贴图
            Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Sword/TianjingSword_6_").Value;

            // 计算旋转角度（每60帧转一圈，可调整速度）
            float rotation = (float)(Main.GameUpdateCount % 360) / 360f * MathHelper.TwoPi;

            // 计算中心点
            Vector2 center = position;

            // 绘制
            spriteBatch.Draw(
                texture,
                center,
                null,
                drawColor * 0.8f,
                rotation,
                new Vector2(texture.Width, texture.Height) * 0.5f,
                scale * 0.32f,
                SpriteEffects.None,
                0f
            );
        }
    }
    public static class NpcUtils
    {
        /// <summary>
        /// 对NPC造成其最大生命值百分比的真实伤害（无击退、无暴击、无无敌帧，最少1点）。
        /// </summary>
        /// <param name="target">目标NPC</param>
        /// <param name="percent">百分比（如0.01表示1%）</param>
        /// <param name="hitDirection">伤害方向</param>
        public static void DealTruePercentDamage(NPC target, float percent, int hitDirection)
        {
            if (target == null || !target.active || target.friendly || target.dontTakeDamage)
                return;

            int extraDamage = Math.Max(1, (int)Math.Ceiling(target.lifeMax * percent));
            target.StrikeNPC(new NPC.HitInfo
            {
                Damage = extraDamage,
                Knockback = 0f,
                HitDirection = hitDirection,
                Crit = false,
                DamageType = DamageClass.Default
            }, fromNet: false);
        }
    }
    internal enum AttackType
    {
        left = -1,
        right = 1
    }
    public class TianjingSword_6Proj_ : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSword_6";
        Player player => Main.player[Projectile.owner];
        Item item => player.HeldItem;

        internal AttackType attackType = AttackType.left;
        float ActualScale = 0.5f;
        float[] Record_ActualScale = new float[6];
        float[] Record_Rotation = new float[6];
        private Stack<NPC> HittedNPC = new Stack<NPC>();

        public override void SetDefaults()
        {
            Projectile.width = 10; // 投射物的碰撞箱宽度
            Projectile.height = 10; // 投射物的碰撞箱高度
            Projectile.friendly = true; // 投射物可以击中敌人
            Projectile.penetrate = -1; // 投射物无限穿透
            Projectile.tileCollide = false; // 投射物不与瓦片碰撞
            Projectile.usesLocalNPCImmunity = true; // 使用局部免疫帧

            Projectile.ownerHitCheck = true; // 确保投射物的拥有者有视线可以瞄准目标（即不能穿越瓦片击中目标）
            Projectile.DamageType = DamageClass.MeleeNoSpeed; // 投射物为近战投射物

            //额外
            Projectile.ignoreWater = true;
            Projectile.localNPCHitCooldown = 1; //手动记录
        }
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HittedNPC.Push(target);
            //target.AddBuff(BuffID., 180);
            // 以挥砍方向为主，带有一定扩散
            Vector2 mainDir = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();

            for (int i = 0; i < 6; i++)
            {
                // 以主方向为基础，叠加一定随机扩散
                Vector2 velocity = mainDir.RotatedByRandom(MathHelper.ToRadians(40)) * Main.rand.NextFloat(2.5f, 5.5f) * 4f;
                var dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(target.width / 3f, target.height / 3f),
                    DustID.Blood,
                    velocity,
                    100,
                    Color.White,
                    Main.rand.NextFloat(1.2f, 2.0f)
                );
                dust.noGravity = true;
                dust.fadeIn = 1.1f;
            }
            if(target.velocity != Vector2.Zero)
            target.velocity += (Projectile.rotation - MathHelper.PiOver2).
                ToRotationVector2() * 6f;
            base.OnHitNPC(target, hit, damageDone);
        }
        public override void OnKill(int timeLeft)
        {
            HittedNPC.Clear();
            base.OnKill(timeLeft);
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;//这一项赋值2可以记录运动轨迹和方向（用于制作拖尾）
            ProjectileID.Sets.TrailCacheLength[Type] = 6;//这一项代表记录的轨迹最多能追溯到多少帧以前(注意最大值取不到)
        }
        public override bool? CanHitNPC(NPC target)
        {
            return !HittedNPC.Contains(target);
        }
        public override void CutTiles()
        {
            Vector2 start = player.MountedCenter;
            Vector2 end = start + (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * ActualScale * 80;
            Utils.PlotTileLine(start, end, 48, DelegateMethods.CutTiles);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = player.MountedCenter;
            Vector2 end = start + (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * ActualScale * 80;
            float collisionPoint = 0f;
            bool coll = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(),
                                                     targetHitbox.Size(),
                                                     start,
                                                     end,
                                                     24,
                                                     ref collisionPoint);
            //测试碰撞点
            //var d = Dust.NewDustPerfect(end, 2);

            if (attackType == AttackType.right)
            {
                if (Projectile.ai[1] > 0 && Projectile.timeLeft > 27) return coll;
            }
            else
            {
                return coll;
            }
            return false;
        }
        private void ReflectHostileProjectiles()
        {
            Vector2 start = player.MountedCenter;
            Vector2 end = start + (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * ActualScale * 80;
            float width = 48f; // 临时加大范围便于测试

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                // 只判断 hostile，避免漏判
                if (!other.active || !other.hostile || other == Projectile)
                    continue;

                Rectangle otherHitbox = other.Hitbox;
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(otherHitbox.TopLeft(), otherHitbox.Size(), start, end, width, ref collisionPoint))
                {
                    //Main.NewText($"反弹弹幕: type={other.type}, owner={other.owner}");
                    other.velocity = -other.velocity;
                    other.friendly = true;
                    other.hostile = false;
                    other.owner = Main.myPlayer;
                    other.netUpdate = true;
                }
            }
        }
        public override void AI()
        {
            Projectile.velocity = new Vector2(0, -2).RotatedBy(Projectile.rotation);
            Projectile.Center = player.MountedCenter;
            Projectile.position.Y += player.gfxOffY;
            player.heldProj = Projectile.whoAmI;
            if (attackType == AttackType.left) Attack_Left();
            else Attack_Right();

            player.itemAnimation = player.itemTime = 3;
            // 检测与敌方弹幕的碰撞并反弹
            ReflectHostileProjectiles();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (attackType == AttackType.left) Draw_Left(lightColor * (Projectile.timeLeft / 20f));
            else Draw_Right(lightColor * (Projectile.timeLeft / 40f));

            return false;
        }
        void Attack_Left()
        {
            var MaxTime = player.GetTotalAttackSpeed(Projectile.DamageType) * item.useAnimation;
            var addValue = 25f / MaxTime;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.Pi);

            // 第一阶段
            if (Projectile.ai[0] == 0)
                Attack_Left_Reset(0, -0.7, 0, 24, 0.8f);
            // 第二阶段
            if (Projectile.ai[0] > 22)
                Attack_Left_Reset(1, -0.6, 28, 20, 1);
            // 第三阶段
            if (Projectile.ai[0] > 40)
                Attack_Left_Reset(2, 3, 48, 18, 0.8f);
            // 新增第四阶段
            if (Projectile.ai[0] > 60)
                Attack_Left_Reset(3, -1.2, 68, 16, 0.5f); // 你可以根据需要调整参数

            // 第一段动画
            if (Projectile.ai[0] < 27)
            {
                var val = Math.Clamp(Projectile.ai[0] / 30f, 0, 1);
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.ai[2] + 4.3f * player.direction, val * addValue);
                ActualScale = E_Postion(0.7f, 1.3, Projectile.rotation, -Projectile.localAI[2]).Length();
                RecordObj(Record_ActualScale, ActualScale);
            }
            // 第二段动画
            else if (Projectile.ai[0] < 47)
            {
                var val = Math.Clamp((Projectile.ai[0] - 27) / 30f, 0, 1);
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.ai[2] + 4.6f * player.direction, val * addValue);
                ActualScale = 1;
                RecordObj(Record_ActualScale, ActualScale);
            }
            // 第三段动画
            else if (Projectile.ai[0] < 67)
            {
                var val = Math.Clamp((Projectile.ai[0] - 47) / 20f, 0, 1);
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.ai[2] - 3.5f * player.direction, val * addValue);
                ActualScale = E_Postion(0.8f, 1.5, Projectile.rotation, -Projectile.localAI[2] + 0.2 * player.direction).Length();
                RecordObj(Record_ActualScale, ActualScale);
            }
            // 第四段动画
            else
            {
                var val = Math.Clamp((Projectile.ai[0] - 67) / 20f, 0, 1);
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.ai[2] + 4.8f * player.direction, val * addValue);
                ActualScale = MathHelper.Lerp(ActualScale, 1.5f, val * 0.5f); // 2.0f为更大范围
                RecordObj(Record_ActualScale, ActualScale);
                
                if(Projectile.ai[0] == 70 &&
                    (TianjingSword_6.isa || Main.mouseRight))
                {
                    var modPlayer = player.GetModPlayer<TianjingSword_6Proj_.TianjingSword6Player>();
                    // 获取未发射过的类型
                    var unused = TianjingSword_6Proj_.TianjingSword6Player.AllHeadTypes
                        .Where(t => !modPlayer.UsedHeadTypes.Contains(t))
                        .ToList();

                    // 如果都发射过了，重置
                    if (unused.Count == 0)
                    {
                        modPlayer.UsedHeadTypes.Clear();
                        unused = TianjingSword_6Proj_.TianjingSword6Player.AllHeadTypes.ToList();
                    }

                    // 随机选一个
                    int typeToShoot = unused[Main.rand.Next(unused.Count)];
                    modPlayer.UsedHeadTypes.Add(typeToShoot);

                    int a = Projectile.NewProjectile(
                        player.GetSource_FromThis(),
                        Projectile.Center,
                        Projectile.localAI[2].ToRotationVector2() * 21,
                        typeToShoot,
                        Projectile.damage,
                        0f,
                        Projectile.owner
                    );
                    Main.projectile[a].ai[0] = 1;
                    Main.projectile[a].ai[1] = 1;
                }
            }
            //Main.NewText(Projectile.ai[0]);
            for (float i = 0; i < 1; i += 0.5f)
            {
                var d = Dust.NewDustDirect(Projectile.Center + new Vector2(0, -ActualScale).RotatedBy(Projectile.rotation) * 80f * Rand_Float(0, 1), 10, 10, DustID.MagnetSphere);
            }

            RecordObj(Record_Rotation, Projectile.rotation);

            Projectile.ai[0] += addValue;
        }
        void Attack_Left_Reset(float CurrentAI, double ToRotation, double ToAI0, double timeleft, double scale)
        {
            if (CurrentAI == Projectile.ai[1])
            {
                if (player.controlUseItem)
                {
                    Projectile.ai[0] = (float)ToAI0;
                    player.direction = Main.MouseWorld.X < player.Center.X ? -1 : 1;
                    Projectile.ai[2] = Projectile.rotation = (float)ToRotation * player.direction;
                    Projectile.timeLeft = (int)timeleft;
                    Projectile.ai[1]++;
                    ResetObj(Record_Rotation, Projectile.rotation);
                    ResetObj(Record_ActualScale, (float)scale);
                    Projectile.localAI[2] = (Main.MouseWorld - Projectile.Center).ToRotation();
                    HittedNPC.Clear();
                    var s = SoundID.Item1;
                    s.PitchVariance = 0.6f;
                    SoundEngine.PlaySound(s, Projectile.Center);

                }
            }
        }
        public class TianjingSword6Player : ModPlayer
        {
            public List<int> UsedHeadTypes = new();

            public static readonly int[] AllHeadTypes = new int[]
            {
                ModContent.ProjectileType<TianjingSwordProj_Head>(),
                ModContent.ProjectileType<TianjingSword_0Proj_Head>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Head>(),
                ModContent.ProjectileType<TianjingSword_2Proj_Head>(),
                ModContent.ProjectileType<TianjingSword_3Proj_Head>(),
                ModContent.ProjectileType<TianjingSword_4Proj_Head>(),
                ModContent.ProjectileType<TianjingSword_5Proj_Head>(),
            };

            public void ResetIfAllUsed()
            {
                if (UsedHeadTypes.Count >= AllHeadTypes.Length)
                    UsedHeadTypes.Clear();
            }
        }
        void Attack_Right()
        {
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.Pi);

            if (Main.mouseRight && Projectile.ai[1] == 0)
            {
                Projectile.timeLeft = 50;
                Projectile.ai[0]++;
                var v = Math.Clamp(Projectile.ai[0] / 50f, 0, 1);
                Projectile.rotation = Projectile.rotation.AngleLerp(-0.94f * player.direction * v + Projectile.localAI[2] + MathHelper.Pi * (player.direction * 0.5f - 0.5f), 0.1f);
                ActualScale = MathHelper.Lerp(ActualScale, 1.3f, v * 0.3f);

                Projectile.localAI[1] = Projectile.rotation;
                Projectile.localAI[2] = (Main.MouseWorld - Projectile.Center).ToRotation();
                player.direction = Main.MouseWorld.X < player.Center.X ? -1 : 1;
                RecordObj(Record_ActualScale, 0.5f);
                if (Projectile.ai[0] >= 50 && Projectile.ai[0] < 57)
                {
                    for (float i = 0; i < 1; i += 0.2f)
                    {
                        var d = Dust.NewDustDirect(Projectile.Center + new Vector2(0, -ActualScale).RotatedBy(Projectile.rotation) * 80f * Rand_Float(0, 1), 10, 10, DustID.MagnetSphere);
                        d.velocity = new Vector2(0, 2).RotatedByRandom(8) / Rand_Float(0.5f, 1.2f);
                        d.fadeIn = 1.3f;
                        d.noGravity = true;
                    }

                }
                if (Projectile.ai[0] == 50)
                {
                    var s = SoundID.Item29;
                    s.Pitch = 0.5f;
                    s.PitchVariance = 0.2f;
                    SoundEngine.PlaySound(s, Projectile.Center);

                }
            }
            else
            {
                if (Projectile.ai[0] > 50)
                {
                    RecordObj(Record_ActualScale, ActualScale);

                    Projectile.ai[1]++;
                    var v = Math.Clamp(Projectile.ai[1] / 70f, 0, 1);
                    Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.localAI[1] + player.direction * 5f, v);
                    //ActualScale = MathHelper.Lerp(ActualScale, E_Postion(0.5f, 1.5, Projectile.rotation, Projectile.localAI[2]).Length(), 0.5f);
                    ActualScale = E_Postion(0.9f, 1.6, Projectile.rotation, -Projectile.localAI[2]).Length();

                    for (float i = 0; i < 1; i += 0.5f)
                    {
                        var d = Dust.NewDustDirect(Projectile.Center + new Vector2(0, -ActualScale).RotatedBy(Projectile.rotation) * 80f * Rand_Float(0, 1), 10, 10, DustID.MagnetSphere);
                    }
                    if (Projectile.ai[1] == 6)
                    {
                        var modPlayer = player.GetModPlayer<TianjingSword6Player>();
                        // 获取未发射过的类型
                        var unused = TianjingSword6Player.AllHeadTypes
                            .Where(t => !modPlayer.UsedHeadTypes.Contains(t))
                            .ToList();

                        // 如果都发射过了，重置
                        if (unused.Count == 0)
                        {
                            modPlayer.UsedHeadTypes.Clear();
                            unused = TianjingSword6Player.AllHeadTypes.ToList();
                        }

                        // 随机选一个
                        int typeToShoot = unused[Main.rand.Next(unused.Count)];
                        modPlayer.UsedHeadTypes.Add(typeToShoot);

                        int a = Projectile.NewProjectile(
                            player.GetSource_FromThis(),
                            Projectile.Center,
                            Projectile.localAI[2].ToRotationVector2() * 14,
                            typeToShoot,
                            Projectile.damage,
                            0f,
                            Projectile.owner
                        );
                        Main.projectile[a].ai[1] = 1;
                        var s = SoundID.Item45;
                        s.PitchVariance = 0.6f;
                        SoundEngine.PlaySound(s, Projectile.Center);
                    }
                }
                else
                {
                    Projectile.Kill();
                    for (float i = 0; i < 1; i += 0.09f)
                    {
                        var d = Dust.NewDustPerfect(Projectile.Center + new Vector2(0, -5).RotatedBy(Projectile.rotation), DustID.MagnetSphere);
                        d.velocity = new Vector2(0, -3).RotatedBy(Projectile.rotation).RotatedByRandom(0.1) * Rand_Float(0.5f, 1.2);
                    }
                }
            }

        }
        void Draw_Left(Color col)
        {
            var co = col;
            co.A = 0;
            co *= 0.6f;

            SpriteEffects? sp = null;
            if (Projectile.ai[1] == 3)
                sp = player.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            for (int i = Projectile.oldRot.Length - 1; i >= 1; i--)
            {
                for (float j = 0; j < 1; j += 0.2f)
                    QuicklyDraw_Proj(MathHelper.Lerp(Record_ActualScale[i], Record_ActualScale[i - 1], j), co * 0.3f, MathHelper.Lerp(Record_Rotation[i], Record_Rotation[i - 1], j), spE: sp);
            }

            QuicklyDraw_Proj(ActualScale, col, spE: sp);

        }
        void Draw_Right(Color col)
        {
            if (Projectile.ai[1] != 0)
            {
                var co = col;
                co.A = 0;
                co *= 0.6f;
                for (int i = Projectile.oldRot.Length - 1; i >= 1; i--)
                {
                    for (float j = 0; j < 1; j += 0.2f)
                        QuicklyDraw_Proj(MathHelper.Lerp(Record_ActualScale[i], Record_ActualScale[i - 1], j), co * 0.3f, MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j));
                }
            }
            QuicklyDraw_Proj(ActualScale, col);
        }
        static Vector2 E_Postion(double a, double b, double Current_Rotation, double Rotate)
        {
            if (Current_Rotation + Rotate == 0) return new Vector2((float)a, 0);
            //用草稿纸推出来的公式
            float y = (float)Math.Pow(a * a / (1 / (float)Math.Tan(Current_Rotation + Rotate)
                / (float)Math.Tan(Current_Rotation + Rotate) + a * a / b / b), 0.5);
            //Main.NewText((long)((rota + next_time + change_roat) * 57.3));
            float x = y / (float)Math.Tan(Current_Rotation + Rotate);

            if (Math.Sin(Current_Rotation + Rotate) > 0)
            {
                return new Vector2(x, y).RotatedBy(-Rotate);
            }
            else
            {
                return -new Vector2(x, y).RotatedBy(-Rotate);
            }
        }
        static float Rand_Float(double a, double b)
        {
            var r = (int)Math.Max(a * 10000, b * 10000);
            var l = (int)Math.Min(a * 10000, b * 10000);
            return Main.rand.Next(l, r) * 0.0001f;
        }
        static void RecordObj<T>(T[] Flo, T ToRecord)
        {
            for (int i = Flo.Length - 1; i >= 1; i--)
                Flo[i] = Flo[i - 1];

            Flo[0] = ToRecord;

        }
        static void ResetObj<T>(T[] Flo, T ToReset)
        {
            for (int i = Flo.Length - 1; i >= 0; i--)
                Flo[i] = ToReset;

        }
        /// <summary>
        /// 快速绘制(通常是手持弹幕)
        /// </summary>
        /// <param name="proj">画的弹幕</param>
        /// <param name="col">颜色</param>
        /// <param name="rotation">null：proj.rotation - MathHelper.PiOver4 * player.direction</param>
        /// <param name="tx">贴图</param>
        /// <param name="scale">缩放</param>
        /// <param name="spE">翻转</param>
        void QuicklyDraw_Proj(float? scale = null, Color? col = null, float? rotation = null, Vector2? Center = null, Texture2D tx = null, SpriteEffects? spE = null, Vector2? Ori = null)
        {
            var proj = Projectile;
            Player player = Main.player[proj.owner];
            Texture2D TX = tx == default ? TextureAssets.Projectile[proj.type].Value : tx;
            Color Col = !col.HasValue ? Lighting.GetColor((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f)) : col.Value;

            if (player != null)
            {
                float sc = !scale.HasValue ? 1 : scale.Value;
                SpriteEffects spe = !spE.HasValue ? player.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None : spE.Value;
                float Ro = !rotation.HasValue ? proj.rotation - MathHelper.PiOver4 * (spe == SpriteEffects.None ? 1 : -1) : rotation.Value - MathHelper.PiOver4 * (spe == SpriteEffects.None ? 1 : -1);

                float Dir = spe == SpriteEffects.None ? 1 : -1;
                Vector2 Cent = !Center.HasValue ? proj.Center : Center.Value;

                var ori = !Ori.HasValue ? new Vector2(TX.Width / 2 - TX.Width / 2 * Dir, TX.Height) : Ori.Value;
                Main.spriteBatch.Draw(TX,
                                      Cent - Main.screenPosition,
                                      null,
                                      Col,
                                      Ro,
                                      ori,
                                      sc,
                                      spe,
                                      0);
            }
        }
    }
}

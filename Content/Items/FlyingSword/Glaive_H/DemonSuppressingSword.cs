using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.FlyingSword.BladeForge;
using SwordMastery.Content.Items.Mterial;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace SwordMastery.Content.Items.FlyingSword.Glaive_H
{
    class DemonSuppressingSword : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword").Value;
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword_").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }

        public override void SetDefaults()
        {
            //Item.CloneDefaults(ItemID.EmpressBlade);
            Item.damage = 32;
            Item.mana = 10;
            Item.width = 70;
            Item.height = 70;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = 20000;
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<DemonSuppressingSwordProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsDemonSuppressingSword>();
            Item.DamageType = DamageClass.Summon;
            Item.noUseGraphic = true;
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
            spriteBatch.Draw(texture, position, sourceRectangle, drawColor, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale * 1.38f, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture_, position, sourceRectangle, drawColor, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale * 1.38f, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, texture.Height / 2-4);
            Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);
            spriteBatch.Draw(texture, drawPosition, sourceRectangle, lightColor, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale * 1.2f, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture_, drawPosition, sourceRectangle, Color.White * 0.8f, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale * 1.2f, SpriteEffects.None, 0f);
            return false;
        }
        public override void PostUpdate()
        {
            float intensity = 0.32f; // 控制光芒强度，越小越淡
            //Color(211, 255, 206)
            //•	R: 211 / 255 ≈ 0.827
            //•	G: 255 / 255 ≈ 1.00
            //•	B: 206 / 255 ≈ 0.807
            Lighting.AddLight(Item.Center, 0.827f * intensity, 1f * intensity, 0.807f * intensity);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsDemonSuppressingSword>(), 3600);
            player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);
            player.SpawnMinionOnCursor(source, player.whoAmI, ModContent.ProjectileType<DemonSuppressingSword_Swing>(), 0, knockback);
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.MeteoriteBar, 28)
                .AddIngredient(ModContent.ItemType<SpringSpirit>(), 3)
                .AddIngredient(ItemID.HallowedBar, 16)
                .AddTile(ModContent.TileType<BladeForgeTile>())
                .Register();
        }
    }
    public class DemonSuppressingSword_Swing : ModProjectile
    {
        private const float SWINGRANGE = (float)Math.PI; // 挥动攻击覆盖的角度（300度）
        private const float FIRSTHALFSWING = 0.5f; // 达到目标角度之前的挥动比例（相对于 swingRange）
        private const float UNWIND = 0.4f; // 剑何时开始消失
        private enum AttackType // 当前进行的攻击类型
        {
            Swing
        }
        private enum AttackStage // 当前执行的攻击阶段，具体见 AI 中的函数描述
        {
            Prepare,
            Execute,
            Unwind
        }
        // 这些属性封装了常规的 ai 和 localAI 数组，以便更简洁易懂
        private AttackType CurrentAttack
        {
            get => (AttackType)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }
        private AttackStage CurrentStage
        {
            get => (AttackStage)Projectile.localAI[0];
            set
            {
                Projectile.localAI[0] = (float)value;
                Timer = 0; // 切换状态时重置计时器
            }
        }
        // 运行时跟踪的变量
        private ref float InitialAngle => ref Projectile.ai[1]; // 瞄准的角度（带有限制）
        private ref float Timer => ref Projectile.ai[2]; // 计时器，用于跟踪每个阶段的进度
        private ref float Progress => ref Projectile.localAI[1]; // 剑相对于初始角度的位置
        private ref float Size => ref Projectile.localAI[2]; // 剑的大小
        // 定义每个阶段的时间函数，考虑到近战攻击速度
        // 注意，你可以根据投射物的需要更改这个
        private float prepTime => 0f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float execTime => 10f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float hideTime => 0f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword"; // 使用物品的纹理作为投射物的纹理
        private Player Owner => Main.player[Projectile.owner];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.width = 86; // 投射物的碰撞箱宽度
            Projectile.height = 86; // 投射物的碰撞箱高度
            Projectile.friendly = true; // 投射物可以击中敌人
            Projectile.timeLeft = 10000; // 投射物失效所需的时间
            Projectile.penetrate = -1; // 投射物无限穿透
            Projectile.tileCollide = false; // 投射物不与瓦片碰撞
            Projectile.usesLocalNPCImmunity = true; // 使用局部免疫帧
            Projectile.localNPCHitCooldown = -1; // 设置为 -1 以确保投射物不会命中两次
            Projectile.ownerHitCheck = true; // 确保投射物的拥有者有视线可以瞄准目标（即不能穿越瓦片击中目标）
            Projectile.DamageType = DamageClass.Summon; // 投射物为近战投射物
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.spriteDirection = Main.MouseWorld.X > Owner.MountedCenter.X ? 1 : -1;
            float targetAngle = (Main.MouseWorld - Owner.MountedCenter).ToRotation();

            if (Projectile.spriteDirection == 1)
            {
                // 不过，我们限制可能方向的范围，以免看起来太过荒谬
                targetAngle = MathHelper.Clamp(targetAngle, (float)-Math.PI * 1 / 3, (float)Math.PI * 1 / 6);
            }
            else
            {
                if (targetAngle < 0)
                {
                    targetAngle += 2 * (float)Math.PI; // 使角度范围连续，以便于操作
                }

                targetAngle = MathHelper.Clamp(targetAngle, (float)Math.PI * 5 / 6, (float)Math.PI * 4 / 3);
            }

            InitialAngle = targetAngle - FIRSTHALFSWING * SWINGRANGE * Projectile.spriteDirection * 1.2f; // 否则我们计算角度
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            // 这个投射物的 Projectile.spriteDirection 在 OnSpawn 中根据拥有者的鼠标位置得出，因此需要同步。spriteDirection 不是自动同步的字段. 由于所有 Projectile.ai 插槽都已使用，因此我们将其手动同步。
            writer.Write((sbyte)Projectile.spriteDirection);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.spriteDirection = reader.ReadSByte();
        }
        public override void AI()
        {
            // 在投射物被杀死之前延长使用动画
            Owner.itemAnimation = 2;
            Owner.itemTime = 2;
            // 如果玩家死去或被控制，杀死投射物
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }
            switch (CurrentStage)
            {
                case AttackStage.Prepare:
                    PrepareStrike();
                    break;
                case AttackStage.Execute:
                    ExecuteStrike();
                    break;
                default:
                    UnwindStrike();
                    break;
            }
            SetSwordPosition();
            Timer++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 origin = new Vector2(Projectile.width / 2, Projectile.height); ;
            float rotationOffset = MathHelper.ToRadians(90f);
            SpriteEffects effects = SpriteEffects.None;

            Main.spriteBatch.Draw(TextureAssets.Projectile[Type].Value,
                Projectile.Center - Main.screenPosition,
                default,
                lightColor * Projectile.Opacity,
                Projectile.rotation + rotationOffset,
                origin,
                Projectile.scale * 0.9f,
                effects,
                0);
            return false;
        }
        // 确保投射物仅在释放阶段和放松阶段造成伤害
        public override bool? CanDamage()
        {
            return false;
        }
        // 方便设置投射物和手臂位置的函数
        public void SetSwordPosition()
        {
            Projectile.rotation = InitialAngle + Projectile.spriteDirection * Progress; // 设置投射物的旋转
            // 设置复合手臂，允许你独立设置手臂的旋转和前后手臂的伸展
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(90f)); // 设置手臂位置（由于手臂起始时低下，所以有 90 度偏移）
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)Math.PI / 2); // 获取手的位置
            armPosition.Y += Owner.gfxOffY; // 添加偏移
            Projectile.Center = armPosition + Projectile.rotation.ToRotationVector2() * -6f; // 设置投射物到手的位置
            Projectile.scale = Size * 1.2f * Owner.GetAdjustedItemScale(Owner.HeldItem); // 稍微放大投射物，也考虑到近战尺寸的修正
            Owner.heldProj = Projectile.whoAmI; // 设置持有的投射物为这个投射物
        }
        // 准备攻击的函数
        private void PrepareStrike()
        {
            Size = 1f; // 使剑在准备攻击时缓慢增加大小，直到达到最大值
            if (Timer >= prepTime)
            {
                SoundEngine.PlaySound(SoundID.Item1); // 播放剑的声音，因为在生成时播放太早
                CurrentStage = AttackStage.Execute; // 如果攻击超过准备时间，进入下一个阶段
            }
        }
        // 实现挥动的首半部分
        private void ExecuteStrike()
        {
            Progress = MathHelper.SmoothStep(0, SWINGRANGE, (1f - UNWIND / 2) * Timer / (execTime * 2));
            if (Timer >= execTime * 2)
            {
                CurrentStage = AttackStage.Unwind; // 完成攻击，进入放松阶段
            }
        }
        // 实现挥动后半部分，剑消失
        private void UnwindStrike()
        {
            Progress = MathHelper.SmoothStep(0, SWINGRANGE, 1f - UNWIND / 10 + UNWIND * Timer / hideTime);
            if (Timer >= hideTime)
            {
                Projectile.Kill(); // 完成隐藏阶段，杀死投射物
            }
        }
    }

    public class DemonSuppressingSwordProj : ModProjectile
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/DemonSuppressingSword";
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword_").Value;
        private readonly Texture2D texture__ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword__").Value;
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
        // 在类内添加字段
        private int markedNpcWhoAmI = -1;
        private bool waitingForNpcDeath = false;

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
            base.OnSpawn(source);
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
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsDemonSuppressingSword>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsDemonSuppressingSword>())) Projectile.Kill();
            // 检查标记的NPC是否死亡
            if (waitingForNpcDeath && markedNpcWhoAmI >= 0 && markedNpcWhoAmI < Main.maxNPCs)
            {
                NPC npc = Main.npc[markedNpcWhoAmI];
                if ((!npc.active || npc.life <= 0) && npc.damage > 0)
                {
                    // 只让第一个召唤物执行
                    bool isFirstSummon = true;
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile proj = Main.projectile[i];
                        if (proj.active && proj.type == Projectile.type && proj.owner == Projectile.owner && proj.whoAmI < Projectile.whoAmI)
                        {
                            isFirstSummon = false;
                            break;
                        }
                    }
                    if (isFirstSummon)
                    {
                        // 统计当前召唤物数量
                        int count = 0;
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            Projectile proj = Main.projectile[i];
                            if (proj.active && proj.type == Projectile.type && proj.owner == Projectile.owner)
                                count++;
                        }
                        int baseDamage = Main.player[Projectile.owner].statLifeMax2;
                        int bonus = (count - 1) * (int)(baseDamage * 0.05f);
                        int totalDamage = baseDamage + bonus;

                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            npc.Center,
                            Vector2.Zero,
                            ModContent.ProjectileType<DemonSuppressingSwordProj_R>(),
                            totalDamage,
                            0f,
                            Projectile.owner
                        );
                    }
                    markedNpcWhoAmI = -1;
                    waitingForNpcDeath = false;
                }
            }
            ////修改此参数以确定攻击范围
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
            if (clown > 0)
                return;

            // 标记被击中的NPC
            if (!target.friendly && !target.immortal && target.life > 0)
            {
                markedNpcWhoAmI = target.whoAmI;
                waitingForNpcDeath = true;
            }
            // 线性插值，血量越低概率越大，最大1%
            float percent = 1f - (float)target.life / target.lifeMax;
            float chance = percent * 0.01f; // 最大1%
            if (Main.rand.NextFloat() < chance && !target.friendly && !target.immortal && target.life > 0)
            {
                Vector2 spawnPos = target.Center + new Vector2(0, -600f);
                Vector2 velocity1 = new Vector2(0, 16f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity1,
                    ModContent.ProjectileType<DemonSuppressingSwordProj_O>(),
                    Projectile.damage,
                    0f,
                    Projectile.owner,
                    target.whoAmI
                );
            }
            // 统计当前环绕弹幕数量
            int orbitCount = 0;
            foreach (var proj in Main.projectile)
            {
                if (proj.active && proj.type == ModContent.ProjectileType<DemonSuppressingSwordProj_>() && proj.ai[0] == Projectile.whoAmI)
                    orbitCount++;
            }

            int maxOrbit = 12;
            if (orbitCount >= maxOrbit)
                return;

            // 概率发射弹幕
            if (Main.rand.NextFloat() >= 0.5f)
                return;

            int newIndex = orbitCount;
            int newTotal = orbitCount + 1;

            // 随机初速度
            Vector2 velocity = Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * 12f;

            int projId = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity,
                ModContent.ProjectileType<DemonSuppressingSwordProj_>(),
                (int)(Projectile.damage * 0.8f),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI, // 父弹幕whoAmI
                newIndex,          // 新弹幕序号
                newTotal           // 总数
            );

            // 同步所有环绕弹幕的总数
            foreach (var proj in Main.projectile)
            {
                if (proj.active && proj.type == ModContent.ProjectileType<DemonSuppressingSwordProj_>() && proj.ai[0] == Projectile.whoAmI)
                    proj.ai[2] = newTotal;
            }

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
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle rectangle = new Rectangle(
                0,
                texture.Height / Main.projFrames[Type] * Projectile.frame,
                texture.Width,
                texture.Height / Main.projFrames[Type]
                );

            // 使用自定义颜色
            Color LightsColor = new Color(211, 255, 206);
            var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
            var v3 = Main.rgbToHsl(LightsColor);
            v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.005f;
            var c = Main.hslToRgb(v3);
            c.A = 0;

            int maxStep = ProjectileID.Sets.TrailCacheLength[Type] - 7;
            if (Projectile.ai[0] != 0) maxStep += 7;
            for (int i = 1; i < maxStep; i++)
            {
                Color color = Color.Lerp(new Color(211, 255, 206) * 0.5f, Color.White, (float)i / maxStep / 1000);
                color.A = 0;
                for (float j = 0; j < 1; j += 0.3f)
                {
                    float factor = (1 - (float)i / ProjectileID.Sets.TrailCacheLength[Type]) * 0.7f + 0.4f;
                    Vector2 oldcenter = Vector2.Lerp(Projectile.oldPos[i], Projectile.oldPos[i - 1], j) + Projectile.Size / 2 - Main.screenPosition;
                    var oldRo = MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j);
                    Main.EntitySpriteDraw(texture__,
                                          oldcenter,
                                          rectangle,
                                          color * factor * 0.2f,
                                          oldRo,
                                          new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                                          Projectile.scale * 1.32f * factor,
                                          SpriteEffects.None,
                                          0);
                }
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                rectangle,
                lightColor,
                Projectile.rotation,
                new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                Projectile.scale * 1.32f,
                SpriteEffects.None,
                0
                );
            Main.EntitySpriteDraw(
                texture_,
                Projectile.Center - Main.screenPosition,
                rectangle,
                c * value,
                Projectile.rotation,
                new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                Projectile.scale * 1.32f,
                SpriteEffects.None,
                0
                );
            #region 以下：渐变高光

            for (int i = 0; i < 1; i++)
            {
                Main.EntitySpriteDraw(texture_,
                                      Projectile.Center - Main.screenPosition,
                                      rectangle,
                                      c * value * 1f,
                                      Projectile.rotation,
                                      new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                                      Projectile.scale * 1.32f,
                                      SpriteEffects.None,
                                      0);
            }
            #endregion
            return false; // 阻止默认绘制
        }
    }
    public class DemonSuppressingSwordProj_O : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword";
        private int groundTimer = 0;
        private bool stuck = false;
        private bool isAtt = false;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;

        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.alpha = 0;
        }

        public override void AI()
        {
            if (!stuck)
            {
                // 指数加速，初速度低，后期极快
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, 64f, 0.18f);

                // 检查是否碰到地面
                if ((Collision.SolidCollision(Projectile.position + new Vector2(0, Projectile.height), Projectile.width, 2) ||
                    Projectile.position.Y > Main.worldSurface * 16) && isAtt)
                {
                    stuck = true;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.tileCollide = false;
                    // 播放爆炸音效
                    SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
                    // 生成烟雾粉尘
                    for (int i = 0; i < 64; i++)
                    {
                        Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                        dust.velocity *= 2f;
                        dust.scale *= 1.12f;
                    }
                    // 屏幕震动
                    if (Main.myPlayer == Projectile.owner)
                    {
                        PunchCameraModifier modifier = new(Projectile.Center, (Main.rand.NextFloat() *
                            ((float)Math.PI * 2f)).ToRotationVector2(), 24f, 12f, 30, 1000f, FullName);// 定义屏幕震动
                        Main.instance.CameraModifiers.Add(modifier);// 屏幕震动
                    }
                }
            }
            else
            {
                groundTimer++;
                if(groundTimer > 30)
                Projectile.alpha += 10;
                if (groundTimer > 60)
                {
                    Projectile.Kill();
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
            isAtt = true;

            // 秒杀 NPC
            target.life = 0;
            target.HitEffect();
            target.checkDead();

            CombatText.NewText(new Rectangle((int)Projectile.position.X, (int)Projectile.position.Y, Projectile.width, Projectile.height),
                        Color.LightGoldenrodYellow, "镇杀！",true); // 显示文本提示
        }
        public override bool? CanDamage() => true;

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            // 绿色粒子
            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 4f);
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.GreenTorch, velocity.X, velocity.Y, 100, new Color(100, 255, 100), 1.6f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1.2f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value;
            //Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade_R").Value;
            Rectangle rectangle = new Rectangle(
                0,
                texture_.Height / Main.projFrames[Type] * Projectile.frame,
                texture_.Width,
                texture_.Height / Main.projFrames[Type]
                );
            // 使用自定义颜色
            Color LightsColor = new Color(211, 255, 206);
            var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.001 + Projectile.ai[2] * 0.07) * 0.3f + 0.7f);
            var v3 = Main.rgbToHsl(LightsColor);
            v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.001 + Projectile.ai[2] * 0.05) * 0.5f + 0.5f) * 0.1f;
            var c = Main.hslToRgb(v3);
            c.A = 100;
            int maxStep = ProjectileID.Sets.TrailCacheLength[Type];
            //if (CurrentState != State.Orbit)
            
            for (int i = 1; i < maxStep; i++)
            {
                Color color = Color.Lerp(new Color(211, 255, 206) * 0.5f, Color.White, (float)i / maxStep / 1000);
                color.A = 0;
                for (float j = 0; j < 1; j += 0.3f)
                {
                    float factor = (1 - (float)i / ProjectileID.Sets.TrailCacheLength[Type]) * 0.7f + 0.4f;
                    Vector2 oldcenter = Vector2.Lerp(Projectile.oldPos[i], Projectile.oldPos[i - 1], j) + Projectile.Size / 2 - Main.screenPosition;
                    var oldRo = MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j);
                    
                    Main.EntitySpriteDraw(texture_,
                                          oldcenter + new Vector2(0, -100),
                                          rectangle,
                                          color * factor * 0.16f * Projectile.Opacity * (!stuck?1f:0.4f),
                                          oldRo,
                                          new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                                          Projectile.scale * 4f * factor,
                                          SpriteEffects.FlipVertically,
                                          0);
                }
            }
            #region 以下：渐变高光

            for (int i = 0; i < 1; i++)
            {
                Main.EntitySpriteDraw(texture_,
                                      Projectile.Center - Main.screenPosition + new Vector2(0, -100),
                                      rectangle,
                                      c * value * 0.8f * Projectile.Opacity,
                                      Projectile.rotation,
                                      texture_.Size() / 2f,
                                      Projectile.scale*4,
                                      SpriteEffects.FlipVertically,
                                      0);
            }
            #endregion
            return false; // 阻止默认绘制
        }
    }
    public class DemonSuppressingSwordProj_ : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword_C";
        public override void SetStaticDefaults()
        {
            
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;

        }
        private enum State
        {
            Scatter,
            MoveToParent,
            Orbit
        }
        private State CurrentState
        {
            get => (State)Projectile.localAI[0];
            set => Projectile.localAI[0] = (float)value;
        }
        private int ParentWhoAmI => (int)Projectile.ai[0];
        private readonly float scatterTime = 30f; // 向外飞行帧数
        private readonly float minSpeed = 2f;     // 进入环绕的最小速度

        public override void SetDefaults()
        {
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.width = Projectile.height = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.friendly = true;
        }

        public override void AI()
        {
            if (CurrentState == State.Scatter)
            {
                Projectile.velocity *= 0.96f;
                Projectile.localAI[1]++;
                if (Projectile.velocity.Length() < minSpeed || Projectile.localAI[1] > scatterTime)
                {
                    CurrentState = State.MoveToParent;
                    Projectile.localAI[1] = 0;
                }
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else if (CurrentState == State.MoveToParent)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                if (ParentWhoAmI < 0 || ParentWhoAmI >= Main.maxProjectiles || !Main.projectile[ParentWhoAmI].active)
                {
                    Projectile.Kill();
                    return;
                }
                var parent = Main.projectile[ParentWhoAmI];
                Vector2 toParent = parent.Center - Projectile.Center;
                float distance = toParent.Length();
                float moveSpeed = 36f;
                float inertia = 20f;
                if (distance > 64f)
                {
                    Projectile.velocity = (Projectile.velocity * (inertia - 1) + toParent.SafeNormalize(Vector2.Zero) * moveSpeed) / inertia;
                }
                else
                {
                    Projectile.Center = parent.Center;
                    Projectile.velocity = Vector2.Zero;
                    CurrentState = State.Orbit;
                    Projectile.localAI[1] = 0;
                }
            }
            else // 环绕
            {
                if (ParentWhoAmI < 0 || ParentWhoAmI >= Main.maxProjectiles || !Main.projectile[ParentWhoAmI].active)
                {
                    Projectile.Kill();
                    return;
                }
                var parent = Main.projectile[ParentWhoAmI];

                // 动态获取所有同父弹幕的环绕弹幕
                List<Projectile> siblings = new List<Projectile>();
                foreach (var proj in Main.projectile)
                {
                    if (proj.active && proj.type == Projectile.type && proj.ai[0] == ParentWhoAmI)
                        siblings.Add(proj);
                }
                // 按 whoAmI 排序，确保序号唯一且稳定
                siblings.Sort((a, b) => a.whoAmI.CompareTo(b.whoAmI));
                int total = siblings.Count;
                int index = siblings.FindIndex(p => p.whoAmI == Projectile.whoAmI);

                // 椭圆参数
                float ellipseA = 38f;
                float ellipseB = 21f;
                float orbitSpeed = 0.08f;
                float phase = index / (float)total * MathHelper.TwoPi;
                float t = Main.GameUpdateCount * orbitSpeed + phase;
                Vector2 offset = new Vector2((float)Math.Cos(t) * ellipseA, (float)Math.Sin(t) * ellipseB);
                Projectile.Center = parent.Center + offset;
                Projectile.rotation = offset.ToRotation() - MathHelper.PiOver2;
                Projectile.velocity = Vector2.Zero;
            }
        }

        public override bool? CanDamage() => CurrentState != State.Scatter;

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            base.Kill(timeLeft);

            // 产生绿色粒子
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * 2f;
                int dust = Dust.NewDust(Projectile.Center, 0, 0, DustID.GreenTorch, velocity.X, velocity.Y, 100, new Color(100, 255, 100), 1.6f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].fadeIn = 1.2f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value;
            //Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade_R").Value;
            Rectangle rectangle = new Rectangle(
                0,
                texture_.Height / Main.projFrames[Type] * Projectile.frame,
                texture_.Width,
                texture_.Height / Main.projFrames[Type]
                );
            // 使用自定义颜色
            Color LightsColor = new Color(211, 255, 206);
            var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.001 + Projectile.ai[2] * 0.07) * 0.3f + 0.7f);
            var v3 = Main.rgbToHsl(LightsColor);
            v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.001 + Projectile.ai[2] * 0.05) * 0.5f + 0.5f) * 0.1f;
            var c = Main.hslToRgb(v3);
            c.A = 100;
            int maxStep = ProjectileID.Sets.TrailCacheLength[Type];
            //if (CurrentState != State.Orbit)
            for (int i = 1; i < maxStep; i++)
            {
                Color color = Color.Lerp(new Color(211, 255, 206) * 0.5f, Color.White, (float)i / maxStep / 1000);
                color.A = 0;
                for (float j = 0; j < 1; j += 0.3f)
                {
                    float factor = (1 - (float)i / ProjectileID.Sets.TrailCacheLength[Type]) * 0.7f + 0.4f;
                    Vector2 oldcenter = Vector2.Lerp(Projectile.oldPos[i], Projectile.oldPos[i - 1], j) + Projectile.Size / 2 - Main.screenPosition;
                    var oldRo = MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j);
                    Main.EntitySpriteDraw(texture_,
                                          oldcenter,
                                          rectangle,
                                          color * factor * 0.2f,
                                          oldRo,
                                          new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[Type]),
                                          Projectile.scale * 1.5f * factor,
                                          SpriteEffects.None,
                                          0);
                }
            }
            #region 以下：渐变高光

            for (int i = 0; i < 1; i++)
            {
                Main.EntitySpriteDraw(texture_,
                                      Projectile.Center - Main.screenPosition,
                                      rectangle,
                                      c * value * 2f,
                                      Projectile.rotation,
                                      texture_.Size() / 2f,
                                      Projectile.scale,
                                      SpriteEffects.FlipVertically,
                                      0);
            }
            #endregion
            return false; // 阻止默认绘制
        }
    }
    public class DemonSuppressingSwordProj_R : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/Glaive_H/DemonSuppressingSword";
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }
        public override void SetDefaults()
        {
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.tileCollide = false;
            Projectile.width = Projectile.height = 10;
            Projectile.usesLocalNPCImmunity = true; // 独立无敌帧
            Projectile.localNPCHitCooldown = -1;    // 独立无敌帧时间
            Projectile.DamageType = DamageClass.Summon;
            Projectile.friendly = true;
            Projectile.hide = true;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 1;
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
        }
        public override void AI()
        {
            Projectile.damage = 0;
            var player = Main.player[Projectile.owner];
            Vector2 toPlayer = (player.Center - Projectile.Center).SafeNormalize(Vector2.Zero);

            // 每帧生成多条光束
            for (int i = 0; i < 8; i++)
            {
                float beamSpeed = Main.rand.NextFloat(12f, 24f); // 光束速度更快
                Vector2 beamDir = toPlayer.RotatedBy(Main.rand.NextFloat(-0.12f, 0.12f)); // 略微抖动
                int dust = Dust.NewDust(Projectile.Center, 0, 0, ModContent.DustType<DemonSuppressingSwordDust>(), 0, 0, 0, default, 2.2f);
                Main.dust[dust].velocity = beamDir * beamSpeed * 0.32f;
                Main.dust[dust].scale = Main.rand.NextFloat(1.8f, 3.6f) * 0.6f; // 拉长
                Main.dust[dust].fadeIn = 0.8f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = false;
            }

            float speed = 16f;
            Projectile.velocity = toPlayer.SafeNormalize(Vector2.UnitY) * speed;

            // 检查是否接触到玩家
            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                int healAmount = Math.Max(1, Projectile.originalDamage / 5);
                if (player.statLife != player.statLifeMax)
                    player.statLife += healAmount;
                player.HealEffect(healAmount, true);
                Projectile.Kill();
            }

            base.AI();
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false; // 阻止默认绘制
        }
    }
    public class DemonSuppressingSwordDust : ModDust
    {
        public override Color? GetAlpha(Dust dust, Color lightColor)
        {
            // 更亮更通透
            return Color.Lerp(Color.White, Color.Transparent, 0.5f) * 0.2f;
        }
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.noLight = false;
            dust.scale = Main.rand.NextFloat(1.8f, 2.8f);
            dust.alpha = 100;
        }
    }
    class BuffsDemonSuppressingSword : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<DemonSuppressingSwordProj>()] > 0)//检测玩家持有的弹幕数量
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.FlyingSword.BladeForge;
using SwordMastery.Content.Items.FlyingSword.Glaive_H;
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
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace SwordMastery.Content.Items.FlyingSword.Glaive
{
    class DemonBlade : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade").Value;
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade_").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }

        public override void SetDefaults()
        {
            //Item.CloneDefaults(ItemID.EmpressBlade);
            Item.damage = 21;
            Item.mana = 10;
            Item.width = 64;
            Item.height = 64;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = 20000;
            Item.rare = ItemRarityID.Orange;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<DemonBladeProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsDemonBlade>();
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
            spriteBatch.Draw(texture, position, sourceRectangle, drawColor, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale*1.38f, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture_, position, sourceRectangle, drawColor, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale * 1.38f, SpriteEffects.None, 0f);
            return false;
        }
        
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, texture.Height / 2);
            Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);
            spriteBatch.Draw(texture, drawPosition, sourceRectangle, lightColor, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale*1.38f, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture_, drawPosition, sourceRectangle, Color.White * 0.8f, -MathHelper.PiOver4 + MathHelper.PiOver2, origin, scale * 1.38f, SpriteEffects.None, 0f);
            return false;
        }
        public override void PostUpdate()
        {
            float intensity = 0.32f; // 控制光芒强度，越小越淡
            //Color(255,255,102)
            //•	R: 255 / 255 ≈ 1.00
            //•	G: 255 / 255 ≈ 1.00
            //•	B: 102 / 255 ≈ 0.40
            Lighting.AddLight(Item.Center, 1f * intensity, 1f * intensity, 0f * intensity);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsDemonBlade>(), 3600);
            player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);
            player.SpawnMinionOnCursor(source, player.whoAmI, ModContent.ProjectileType<DemonBlade_Swing>(), 0, knockback);
            return false;
        }
        public override void AddRecipes()
        {
            Condition DemonBladeCondition = new Condition(
                Language.GetText(Language.ActiveCulture.Name == "zh-Hans"?"需要“龙葵的牺牲”": "Requires “The sacrifice of Longkui”"),
                () => DemonBladeModPlayer.IsDemonBladAddRecipes
            );

            CreateRecipe()
                .AddIngredient(ModContent.ItemType<FlyingLightsBane>(), 1)
                .AddIngredient(ModContent.ItemType<RemoveLightStone>(), 1)
                .AddTile(ModContent.TileType<BladeForgeTile>())
                .AddCondition(DemonBladeCondition)
                .Register();
        }
    }
    class DemonBladeModPlayer : ModPlayer
    {
        public static bool IsDemonBladAddRecipes = false;
        public override void PreUpdateBuffs()
        {
            base.PreUpdateBuffs();
            if (Main.LocalPlayer.HasBuff(ModContent.BuffType<BuffsDemonBladAddRecipes>()))
                IsDemonBladAddRecipes = true;
            else
                IsDemonBladAddRecipes = false;
        }
    }
    public class DemonBlade_Swing : ModProjectile
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
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade"; // 使用物品的纹理作为投射物的纹理
        private Player Owner => Main.player[Projectile.owner];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Projectile.width = 64; // 投射物的碰撞箱宽度
            Projectile.height = 64; // 投射物的碰撞箱高度
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
                Projectile.scale,
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
            Projectile.Center = armPosition + Projectile.rotation.ToRotationVector2() * 0f; // 设置投射物到手的位置
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
            Progress = MathHelper.SmoothStep(0, SWINGRANGE, (1f - UNWIND / 10) + UNWIND * Timer / (hideTime));
            if (Timer >= hideTime)
            {
                Projectile.Kill(); // 完成隐藏阶段，杀死投射物
            }
        }
    }

    public class DemonBladeProj : ModProjectile
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/DemonBlade";
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade_").Value;
        private readonly Texture2D texture__ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade__").Value;
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
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsDemonBlade>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsDemonBlade>())) Projectile.Kill();

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
            // 冷却判定
            if (clown > 0)
                return;

            if (Main.rand.Next(5) >= 4)
                return;

            float radius = 1200f; // 距离目标中心的半径
            int projType = ModContent.ProjectileType<DemonBladeProj_>();
            int damage = (int)(Projectile.damage); // 可根据需要调整
            float knockback = 2f;
            // 随机一个角度
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);

            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 54; // 朝向目标中心
            if(target.CanBeChasedBy(Projectile.owner))
            {
                int proj = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                projType,
                damage,
                knockback,
                Projectile.owner
            );
                Main.projectile[proj].light = 0.54f;
                clown = 30;
            }
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
            Color LightsColor = new Color(122, 72, 199);
            var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
            var v3 = Main.rgbToHsl(LightsColor);
            v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.005f;
            var c = Main.hslToRgb(v3);
            c.A = 0;

            int maxStep = ProjectileID.Sets.TrailCacheLength[Type] - 7;
            if (Projectile.ai[0] != 0) maxStep += 7;
            for (int i = 1; i < maxStep; i++)
            {
                Color color = Color.Lerp(new Color(79, 57, 111), Color.White, (float)i / maxStep / 1000);
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
                                          Projectile.scale * 1.5f * factor,
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
                Projectile.scale * 1.5f,
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
                Projectile.scale * 1.5f,
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
                                      Projectile.scale * 1.5f,
                                      SpriteEffects.None,
                                      0);
            }
            #endregion
            return false; // 阻止默认绘制
        }
    }
    public class DemonBladeProj_ : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade_T";     
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
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
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(-45);

            // 检查是否需要追踪
            NPC target = FindClosestNPC(250f); // 寻找250像素范围内最近的敌人
            if (target != null)
            {
                if ((target.Center.X > Projectile.Center.X && Projectile.velocity.X > 0)
                || (target.Center.X < Projectile.Center.X && Projectile.velocity.X < 0))
                {
                    // 计算方向并调整速度
                    Vector2 direction = target.Center - Projectile.Center;
                    direction.Normalize();

                    // 调整速度并设置最小速度阈值
                    float currentSpeed = Projectile.velocity.Length();
                    float minSpeed = 54f; // 设置最小速度
                    float newSpeed = Math.Max(currentSpeed, minSpeed); // 确保速度不低于最小值

                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * newSpeed, 0.4f); // 调整速度和方向
                }
            }
            // 冷却递减
            if (clown > 0)
                clown--;
            base.AI();
        }

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            base.Kill(timeLeft);
            int particleCount = 10; // 粒子数量
            for (int i = 0; i < particleCount; i++)
            {
                // 随机生成粒子的扩散方向
                Vector2 velocity = Main.rand.NextVector2Circular(12f, 12f); // 粒子速度范围

                // 创建粒子
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Demonite, velocity, 100, Color.LimeGreen, 1.5f);
                dust.noGravity = true; // 禁用重力
                dust.fadeIn = 1f;      // 设置淡入效果
                dust.scale = 1.2f;     // 设置粒子大小
            }
        }
        private int clown = 0;// 攻击冷却
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 冷却判定
            if (clown > 0)
                return;
            if (target.CanBeChasedBy(Projectile.owner))
            {
                Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DemonBladeProj_R>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner
            );
                clown = 10;
            }
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.BlackLightningHit,
                new ParticleOrchestraSettings { PositionInWorld = Main.rand.NextVector2FromRectangle(target.Hitbox) },
                Projectile.owner);
            base.OnHitNPC(target, hit, damageDone);
        }
        // 寻找最近的敌人
        private NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;
            float closestDistance = maxDetectDistance;

            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !npc.friendly && npc.lifeMax > 5 && npc.CanBeChasedBy(Projectile.owner))
                {
                    float distance = Vector2.Distance(Projectile.Center, npc.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestNPC = npc;
                    }
                }
            }

            return closestNPC;
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
            Color LightsColor = new Color(122, 72, 199);
            var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
            var v3 = Main.rgbToHsl(LightsColor);
            v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.1f;
            var c = Main.hslToRgb(v3);
            c.A = 100;

            Color MyColor = c * (0.4f / 3f);
            MyColor.A = 50;
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
                                          texture_.Size() / 1.2f,
                                          Projectile.scale * 1.5f * factor,
                                          SpriteEffects.FlipVertically,
                                          0);
                }
            }

            #region 以下：渐变高光

            for (int i = 0; i < 1; i++)
            {
                Main.EntitySpriteDraw(texture_,
                                      Projectile.Center - Main.screenPosition,
                                      rectangle,
                                      c * value * 0.6f,
                                      Projectile.rotation + MathHelper.ToRadians(-45),
                                      texture_.Size() / 1.2f,
                                      Projectile.scale,
                                      SpriteEffects.FlipVertically,
                                      0);
            }
            #endregion
            return false; // 阻止默认绘制
        }
    }
    public class DemonBladeProj_R : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/Glaive/DemonBlade";
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
                int dust = Dust.NewDust(Projectile.Center, 0, 0, ModContent.DustType<DemonBladeDust>(), 0, 0, 0, default, 2.2f);
                Main.dust[dust].velocity = beamDir * beamSpeed * 0.32f;
                Main.dust[dust].scale = Main.rand.NextFloat(1.8f, 2.8f) * 0.5f; // 拉长
                Main.dust[dust].fadeIn = 0.5f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = false;
            }

            float speed = 18f;
            Projectile.velocity = toPlayer.SafeNormalize(Vector2.UnitY) * speed;

            // 检查是否接触到玩家
            if (Projectile.Hitbox.Intersects(player.Hitbox))
            {
                int healAmount = Math.Max(1, Projectile.originalDamage / 10);
                if(player.statLife != player.statLifeMax)
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
    public class DemonBladeDust : ModDust
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
    class BuffsDemonBlade : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<DemonBladeProj>()] > 0)//检测玩家持有的弹幕数量
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
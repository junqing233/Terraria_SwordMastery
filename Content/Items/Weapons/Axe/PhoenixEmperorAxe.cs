using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.Weapons.Sword;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace SwordMastery.Content.Items.Weapons.Axe
{
    public class PhoenixEmperorAxe : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 32; // 攻击力
            Item.crit = 3; // 暴击率
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.width = 100; // 物品宽度
            Item.height = 68; // 物品高度
            Item.useTime = 60; // 使用时间
            Item.useAnimation = 30; // 使用动画时长
            Item.useStyle = ItemUseStyleID.Swing; // 使用方式; // 您可以选择其他风格，或根据您的需要自行定义
            Item.knockBack = 8; // 击退力量
            Item.value = Item.buyPrice(0, 0, 10, 64); // 价值
            Item.rare = ItemRarityID.Green; // 稀有度
            Item.UseSound = SoundID.Item1; // 使用音效
            Item.autoReuse = true; // 自动使用
            Item.useTurn = false; // 自动转向
            Item.noUseGraphic = true; // 显示使用图标
            Item.tileBoost = 4; // 作为斧头时增强的功能
            Item.noMelee = true;
            // 接口，用于做斧头的逻辑
            Item.axe = 50; // 设置为10，表示该武器可用于砍树和大约会使用的速度
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "PhoenixEmperorAxe", "神兵") { OverrideColor = new Color(135, 206, 250) });
        }
        public override bool? UseItem(Player player)
        {
            if(player.altFunctionUse != 2)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<PhoenixEmperorAxe_Swing>(), Item.damage, Item.knockBack, player.whoAmI);
            return true;
        }
        public override void AddRecipes()
        {
            // 创建一个新的配方组
            RecipeGroup group = new RecipeGroup(() => "任意鸟",
                ItemID.Bird,
                ItemID.BlueJay,
                ItemID.Cardinal,
                ItemID.MallardDuck,
                ItemID.Duck,
                ItemID.GoldBird,
                ItemID.Seagull,
                ItemID.Grebe,
                ItemID.ScarletMacaw,
                ItemID.BlueMacaw,
                ItemID.Toucan,
                ItemID.YellowCockatiel,
                ItemID.GrayCockatiel);
            // 注册配方组
            RecipeGroup.RegisterGroup("SwordMastery:PhoenixEmperorAxeGroup", group);

            CreateRecipe()
              .AddRecipeGroup("SwordMastery:PhoenixEmperorAxeGroup", 9) // 使用配方组
              .AddTile(TileID.Anvils) // 铁砧
              .Register();
        }
    }
    public class PhoenixEmperorAxe_Swing : ModProjectile
    {
        private  float SWINGRANGE = (float)Math.PI*1.6f; // 挥动攻击覆盖的角度（300度）
        private  float FIRSTHALFSWING = 0.5f; // 达到目标角度之前的挥动比例（相对于 swingRange）
        private  float UNWIND = 0.2f; // 剑何时开始消失
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
        private float execTime => 12f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float hideTime => 10f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        public override string Texture => "SwordMastery/Content/Items/Weapons/Axe/PhoenixEmperorAxe"; // 使用物品的纹理作为投射物的纹理
        private Player Owner => Main.player[Projectile.owner];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
            //ProjectileID.Sets.TrailingMode[Type] = 2;//这一项赋值2可以记录运动轨迹和方向（用于制作拖尾）
            //ProjectileID.Sets.TrailCacheLength[Type] = 6;//这一项代表记录的轨迹最多能追溯到多少帧以前(注意最大值取不到)
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
            Projectile.DamageType = DamageClass.MeleeNoSpeed; // 投射物为近战投射物
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
            // 更新投射物的位置和旋转
            Projectile.oldPos[0] = Projectile.position;
            Projectile.oldRot[0] = Projectile.rotation;

            // 更新历史位置和旋转
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                Projectile.oldPos[i] = Projectile.oldPos[i - 1];
                Projectile.oldRot[i] = Projectile.oldRot[i - 1];
            }
            if (Projectile.spriteDirection > 0) Owner.direction = 1;
            else Owner.direction = -1;
            
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
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 120);

            // 计算挥砍方向（与碰撞检测一致）
            Vector2 start = Owner.MountedCenter;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * ((Projectile.Size.Length()) * Projectile.scale * 1.36f);
            Vector2 mainDir = (end - start).SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < 6; i++)
            {
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
            target.velocity += mainDir * 6f;
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 origin; ;
            float rotationOffset;
            Vector2 origin_t; ;
            float rotationOffset_t;
            SpriteEffects effects;
            if (Projectile.spriteDirection > 0)
            {
                origin = new Vector2(Projectile.width / 2, Projectile.height / 2 + 10);
                rotationOffset = MathHelper.ToRadians(40f);
                origin_t = new Vector2(0, Projectile.height);
                rotationOffset_t = MathHelper.ToRadians(45f);
                effects = SpriteEffects.None;
            }
            else
            {
                origin = new Vector2(Projectile.width / 2 + 15, Projectile.height / 2 - 10);
                rotationOffset = MathHelper.ToRadians(140f);
                origin_t = new Vector2(Projectile.width, Projectile.height);
                rotationOffset_t = MathHelper.ToRadians(135f);
                effects = SpriteEffects.FlipHorizontally;
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            // 设置拖尾效果
            Color MyColor = Color.White;
            MyColor.A = 0; // 设置A为255以确保可见
                           // 计算绘制位置和大小
            if (Timer > 10 && CurrentStage == AttackStage.Execute)
                //先绘制拖尾
                for (int i = 0; i < 9; i++) // 循环上限小于轨迹长度
                {
                    float factor = 0.5f - (float)i / 18; // 计算透明度因子
                    Vector2 oldCenter = Projectile.oldPos[i + 1] + Projectile.Size / 2 - Main.screenPosition; // 获取旧位置的中心点
                    // 绘制拖尾
                    Main.EntitySpriteDraw(texture, oldCenter,
                        default,
                        MyColor * factor, // 颜色逐渐变淡
                        Projectile.oldRot[i] + rotationOffset, // 弹幕轨迹上的曾经的方向
                        origin, // 贴图参照原点在左上角
                        Projectile.scale * 1f, // 缩放
                        effects,
                        0); // 层级
                }
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
        // 找到剑的起始和结束位置，并使用线段碰撞检测与敌人检查碰撞
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Owner.MountedCenter;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * ((Projectile.Size.Length()) * Projectile.scale * 1.36f);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 15f * Projectile.scale, ref collisionPoint);
        }

        // 对瓦片进行类似的碰撞检测
        public override void CutTiles()
        {
            Vector2 start = Owner.MountedCenter;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * (Projectile.Size.Length() * Projectile.scale * 1.58f);
            Utils.PlotTileLine(start, end, 20 * Projectile.scale, DelegateMethods.CutTiles);
        }

        // 确保投射物仅在释放阶段和放松阶段造成伤害
        public override bool? CanDamage()
        {
            if (CurrentStage == AttackStage.Prepare)
                return false;
            return base.CanDamage();
        }
        // 方便设置投射物和手臂位置的函数
        public void SetSwordPosition()
        {
            Projectile.rotation = InitialAngle + Projectile.spriteDirection * Progress; // 设置投射物的旋转
            // 设置复合手臂，允许你独立设置手臂的旋转和前后手臂的伸展
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(90f)); // 设置手臂位置（由于手臂起始时低下，所以有 90 度偏移）
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)Math.PI / 2); // 获取手的位置
            armPosition.Y += Owner.gfxOffY; // 添加偏移
            Projectile.Center = armPosition + Projectile.rotation.ToRotationVector2() * (Projectile.spriteDirection > 0 ? 50f : 85f); // 设置投射物到手的位置
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
}

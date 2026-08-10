using Microsoft.Build.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using SwordMastery.Content.Items.Weapons.Miscellaneous;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static SwordMastery.Content.Items.Weapons.Sword.DivineFireBeast;
using static System.Net.Mime.MediaTypeNames;

namespace SwordMastery.Content.Items.Weapons.Sword
{
    public class DivineFireBeastGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 生物类型列表（可根据需要补充）
            int[] snowBiomeNPCs = new int[]
            {
                NPCID.Demon,//恶魔
                NPCID.VoodooDemon,//巫毒恶魔
            };

            if (snowBiomeNPCs.Contains(npc.type))
            {
                // 普通模式掉落规则
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DivineFireBeast>(), 200, 1, 1));
                npcLoot.Add(notExpertRule);

                // 专家模式掉落规则
                LeadingConditionRule expertRule = new LeadingConditionRule(new Conditions.IsExpert());
                expertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<DivineFireBeast>(), 150, 1, 1));
                npcLoot.Add(expertRule);
            }
        }
    }
    public class DivineFireBeast : ModItem
    {
        public static int Attack = 0; // 0=普通，1=右键弹幕存在/强化
        public static bool HasProj = false; // 右键弹幕是否存在

        public override void SetDefaults()
        {
            Item.damage = 36;
            Item.crit = 9;
            Item.DamageType = DamageClass.Melee;
            Item.width = 80;
            Item.height = 72;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4;
            Item.value = Item.buyPrice(1, 0, 0, 64);
            Item.rare = ItemRarityID.Green;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<DivineFireBeastProj_L>();
            Item.shootSpeed = 0f;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "DivineFireBeast", "神兵") { OverrideColor = new Color(251, 153, 2) });
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanShoot(Player player)
        {
            if(player.altFunctionUse == 2)
                return false;
            return true;
        }
        public override bool? UseItem(Player player)
        {
            // 右键：发射DivineFireBeastProj_，并进入特殊状态
            if (player.altFunctionUse == 2 && !HasProj)
            {
                // 临时隐藏武器图标和近战判定
                int proj = Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    player.DirectionTo(Main.MouseWorld) * 36f,
                    ModContent.ProjectileType<DivineFireBeastProj_>(),
                    Item.damage,
                    Item.knockBack,
                    player.whoAmI
                );
                HasProj = true;
                Attack = 1;
            }
            // 强化状态下左键：发射DivineFireBeastProj弹幕
            else if (Attack == 1 && player.altFunctionUse != 2 && !HasProj)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<DivineFireBeastProj>(),
                    Item.damage,
                    Item.knockBack,
                    player.whoAmI
                );
            }
            return true;
        }

        public override void HoldItem(Player player)
        {
            // 检查弹幕是否还存在
            if (HasProj)
            {
                bool found = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.type == ModContent.ProjectileType<DivineFireBeastProj_>() && proj.owner == player.whoAmI)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    HasProj = false;
                    Attack = 0;
                }
            }

            if (HasProj || Attack == 1)
            {
                Item.shoot = ProjectileID.None;
            }
            else
            {
                Item.shoot = ModContent.ProjectileType<DivineFireBeastProj_L>();
            }
        }
    }
    public class DivineFireBeastProj_ : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/DivineFireBeast";
        private int Direction = 0;
        private int Stifr = 0;
        public bool Sticking
        {
            get { return Projectile.ai[0] != 0; }// 因为默认状态下ai[0]是 = 0，所以这里用 != 0进行判定
            set { Projectile.ai[0] = value ? 1 : 0; }// 三元运算符：当表达式值为true，返回前者，反之为后者
        }
        public int TargetWho
        {
            get { return (int)Projectile.ai[1]; }
            set { Projectile.ai[1] = value; }
        }
        public override void SetDefaults()
        {
            Projectile.width = 10; // 弹幕宽度
            Projectile.height = 10; // 弹幕高度
            Projectile.friendly = true; // 友方弹幕
            Projectile.tileCollide = true; // 不与瓷砖碰撞
            Projectile.DamageType = DamageClass.Melee; // 伤害类型
            Projectile.penetrate = -1; // 穿透
            Projectile.ignoreWater = true; // 无视液体
            Projectile.timeLeft = 360;// 弹幕持续时间
            Projectile.alpha = 1; // 透明度
            Projectile.usesLocalNPCImmunity = true; //独立无敌帧
            Projectile.localNPCHitCooldown = 30; //独立无敌帧时间
        }
        public override void OnSpawn(IEntitySource source)
        {
            Direction = (int)Projectile.velocity.X;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // 碰撞后静止
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
            Sticking = true;
            return false; // 不销毁弹幕
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            base.AI();
            // 平台碰撞检测
            Point tilePos = Projectile.Center.ToTileCoordinates();
            Tile tile = Framing.GetTileSafely(tilePos.X, tilePos.Y + 1); // 检查弹幕正下方的Tile

            // 判断是否为平台（平台的TileID见下方注释）
            // 判断平台中心与弹幕中心距离
            if (tile != null && tile.HasTile && Main.tileSolidTop[tile.TileType])
            {
                Vector2 platformCenter = new Vector2(tilePos.X * 16 + 8, (tilePos.Y + 0) * 16 + 8);
                if (Vector2.Distance(Projectile.Center, platformCenter) < 40f)
                {
                    // 触发碰撞逻辑
                    OnTileCollide(Projectile.velocity);
                    // 让弹幕停在平台上
                    Projectile.position.Y = (tilePos.Y + 1) * 16 - Projectile.height;
                    Projectile.velocity.Y = 0;
                    Projectile.netUpdate = true;
                    Sticking = true;
                }
            }
            // 1. 判断回收或传送状态
            if (Projectile.timeLeft <= 10 || Main.mouseLeft)
                Stifr = 1;
            if (Sticking && Main.mouseRight && Projectile.timeLeft < 330)
                Stifr = 2;

            // 2. 回收状态：平滑回到玩家身边
            if (Stifr == 1)
            {
                Projectile.tileCollide = false;
                Projectile.timeLeft = 2;
                Vector2 toPlayer = player.Center - Projectile.Center;
                float distance = toPlayer.Length();
                float speed = MathHelper.Lerp(16f, 36f, 1f - Projectile.timeLeft / 10f); // 回收速度递增
                if (distance < speed)
                {
                    Projectile.Center = player.Center;
                    Projectile.Kill();
                    return;
                }
                Projectile.velocity = toPlayer.SafeNormalize(Vector2.Zero) * speed;
                Projectile.rotation += Projectile.direction * 0.2f;
                return;
            }

            // 3. 传送状态：玩家传送到弹幕，弹幕销毁
            if (Stifr == 2)
            {
                Attack = 1;
                Vector2 center = Projectile.Center;
                Vector2 mouse = Main.MouseWorld;
                Vector2 toMouse = mouse - center;
                float dist = toMouse.Length();
                Vector2 targetPos;
                float maxDist = 200f;
                if (dist <= maxDist)
                {
                    targetPos = mouse;
                }
                else
                {
                    targetPos = center + toMouse.SafeNormalize(Vector2.UnitY) * maxDist;
                }
                player.immune = true;// 玩家无敌
                player.immuneTime = 30; // 确保无敌时间短于冲刺持续时间
                player.Teleport(targetPos, 12);
                for (int i = 0; i < 20; i++)
                    Dust.NewDustPerfect(player.Center, DustID.GoldCoin, Main.rand.NextVector2Unit() * 3f);
                Projectile.Kill();
                return;
            }

            // 4. 粘附状态：弹幕跟随NPC并定时伤害
            if (Sticking)
            {
                NPC target = Main.npc[TargetWho];
                if (TargetWho != 0)
                {
                    if (!target.active)
                    {
                        Stifr = 1;
                        return;
                    }
                    Projectile.velocity *= 0.99999999999f;
                    Projectile.Center = target.Center - Projectile.velocity * 2f;
                    Projectile.gfxOffY = target.gfxOffY;
                }
                return;
            }

            // 5. 普通运动：抛物线下落
            Projectile.velocity.Y += 0.05f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }
        public override bool? CanDamage()
        {
            return true;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 打到某个目标之后
            // 把粘滞设为true，这样AI就会从正常行动切换到粘滞状态
            Sticking = true;
            // 把被命中目标的身份记录下来
            TargetWho = target.whoAmI;
            // 并重置弹幕的存活时间，360 为特定的持续时间
            Projectile.velocity = (target.Center - Projectile.Center) *
                0.75f; // 根据目标中心的差值（实体中心之间的差异）更改速度
            Projectile.netUpdate = true; // 网络更新这个矛
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.OnFire, 60);
            }
            base.OnHitNPC(target, hit, damageDone);
        }
        public override void OnKill(int timeLeft)
        {
            HasProj = false;
        }
        
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 origin;
            float rotationOffset;
            SpriteEffects effects;

            // 统一设置初始origin/rotationOffset/effects
            if (Direction > 0)
            {
                origin = new Vector2(60, 40);
                rotationOffset = MathHelper.ToRadians(45f);
                effects = SpriteEffects.None;
            }
            else
            {
                origin = new Vector2(20, 40);
                rotationOffset = MathHelper.ToRadians(135f);
                effects = SpriteEffects.FlipHorizontally;
            }
            Main.spriteBatch.Draw(
            TextureAssets.Projectile[Type].Value,
            Projectile.Center - Main.screenPosition,
            null,
            lightColor * Projectile.Opacity * lightColor.A,
            Projectile.rotation + rotationOffset,
            origin,
            Projectile.scale,
            effects,
            0);

            return false;
        }
    }
    public class DivineFireBeastProj : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/DivineFireBeast";
        Player player => Main.player[Projectile.owner];
        Item item => player.HeldItem;

        float ActualScale = 0.5f;
        float[] Record_ActualScale = new float[6];
        float[] Record_Rotation = new float[6];
        private Stack<NPC> HittedNPC = new Stack<NPC>();

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
            Projectile.localNPCHitCooldown = 1;
        }
        public override bool ShouldUpdatePosition() => false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HittedNPC.Push(target);
            Vector2 mainDir = (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2();
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
            if (target.velocity != Vector2.Zero)
                target.velocity += (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 16f;
            // 定义倍率区间和对应倍率
            float[] thresholds = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
            float[] multipliers = { 30f, 20f, 16f, 12f, 10f };

            float rand = Main.rand.NextFloat();
            float multiplier = multipliers[Array.FindIndex(thresholds, t => rand < t)];

            if (Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(
                    player.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ProjectileID.Volcano,
                    (int)(Projectile.damage * multiplier),
                    Projectile.knockBack,
                    player.whoAmI
                );
            }
            base.OnHitNPC(target, hit, damageDone);
        }
        public override void OnKill(int timeLeft)
        {
            Attack = 0;
            HittedNPC.Clear();
            base.OnKill(timeLeft);
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
        }
        public override bool? CanHitNPC(NPC target) => !HittedNPC.Contains(target);

        public override void CutTiles()
        {
            Vector2 start = player.MountedCenter;
            Vector2 end = start + (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * ActualScale * 100;
            Utils.PlotTileLine(start, end, 48, DelegateMethods.CutTiles);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = player.MountedCenter;
            Vector2 end = start + (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * Projectile.scale * ActualScale * 100;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 24, ref collisionPoint);
        }
        public override void AI()
        {
            Projectile.velocity = new Vector2(0, -2).RotatedBy(Projectile.rotation);
            Projectile.Center = player.MountedCenter;
            Projectile.position.Y += player.gfxOffY;
            player.heldProj = Projectile.whoAmI;
            Attack_Left();
            player.itemAnimation = player.itemTime = 3;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Draw_Left(lightColor * (Projectile.timeLeft / 20f));
            return false;
        }
        void Attack_Left()
        {
            var MaxTime = player.GetTotalAttackSpeed(Projectile.DamageType) * item.useAnimation;
            var addValue = 25f / MaxTime;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.Pi);

            // 只保留第一阶段
            if (Projectile.ai[0] == 0)
                Attack_Left_Reset(0, -0.7, 0, 24, 0.8f);

            // 第一段动画
            if (Projectile.ai[0] < 27)
            {
                var val = Math.Clamp(Projectile.ai[0] / 60f, 0, 1);
                Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.ai[2] + 4.3f * player.direction, val * addValue);
                ActualScale = E_Postion(1f, 2f, Projectile.rotation, -Projectile.localAI[2]).Length();
                RecordObj(Record_ActualScale, ActualScale);
            }
            else
            {
                Projectile.Kill();
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
        void Draw_Left(Color col)
        {
            var co = col;
            co.A = 0;
            co *= 0.6f;
            SpriteEffects? sp = null;
            for (int i = Projectile.oldRot.Length - 1; i >= 1; i--)
            {
                for (float j = 0; j < 1; j += 0.2f)
                    QuicklyDraw_Proj(MathHelper.Lerp(Record_ActualScale[i], Record_ActualScale[i - 1], j), co * 0.3f, MathHelper.Lerp(Record_Rotation[i], Record_Rotation[i - 1], j), spE: sp);
            }
            QuicklyDraw_Proj(ActualScale, col, spE: sp);
        }
        static Vector2 E_Postion(double a, double b, double Current_Rotation, double Rotate)
        {
            if (Current_Rotation + Rotate == 0) return new Vector2((float)a, 0);
            float y = (float)Math.Pow(a * a / (1 / (float)Math.Tan(Current_Rotation + Rotate)
                / (float)Math.Tan(Current_Rotation + Rotate) + a * a / b / b), 0.5);
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
    public class DivineFireBeastProj_L : ModProjectile
    {
        // 定义一些常量，决定剑的挥动范围
        // 注意，我们在这里使用乘数，因为这简化了这些交互的调整
        // 你可以更改这些值或完全替换它们，但这些值是根据外观调整的
        // 定义一些常量，决定剑的挥动范围
        private const float SWINGRANGE = 1.2f * (float)Math.PI; // 挥动攻击覆盖的角度（300度）
        private const float FIRSTHALFSWING = 0.45f; // 达到目标角度之前的挥动比例（相对于 swingRange）
        private const float WINDUP = 0.15f; // 玩家攻击前手臂向后摆动的程度（相对于 swingRange）
        private const float UNWIND = 0.4f; // 剑何时开始消失

        private enum AttackStage // 当前执行的攻击阶段，具体见 AI 中的函数描述
        {
            Prepare,
            Execute,
            Unwind
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
        private float prepTime => 12f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float execTime => 8f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float hideTime => 12f / Owner.GetTotalAttackSpeed(Projectile.DamageType);

        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/DivineFireBeast"; // 使用物品的纹理作为投射物的纹理
        private Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
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
            Projectile.DamageType = DamageClass.Melee; // 投射物为近战投射物
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.spriteDirection = Main.MouseWorld.X > Owner.MountedCenter.X ? 1 : -1;
            float targetAngle = (Main.MouseWorld - Owner.MountedCenter).ToRotation();
            InitialAngle = targetAngle - FIRSTHALFSWING * SWINGRANGE * Projectile.spriteDirection; // 计算角度
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
            Owner.itemAnimation = 2; // 延长使用动画
            Owner.itemTime = 2;

            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            // 仅保留挥舞的逻辑
            if (CurrentStage == AttackStage.Prepare)
            {
                PrepareStrike();
            }
            else if (CurrentStage == AttackStage.Execute)
            {
                ExecuteStrike();
            }
            else
            {
                UnwindStrike();
            }

            SetSwordPosition();
            Timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 根据方向计算剑的原点（护手）并偏移剑的旋转（因为剑的贴图是倾斜的）
            Vector2 origin;
            float rotationOffset;
            SpriteEffects effects;

            if (Projectile.spriteDirection > 0)
            {
                origin = new Vector2(0, Projectile.height + 5);
                rotationOffset = MathHelper.ToRadians(20f);
                effects = SpriteEffects.None;
            }
            else
            {
                origin = new Vector2(Projectile.width + 15, Projectile.height + 4);
                rotationOffset = MathHelper.ToRadians(160f);
                effects = SpriteEffects.FlipHorizontally;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, default, lightColor * Projectile.Opacity, Projectile.rotation + rotationOffset, origin, Projectile.scale, effects, 0);

            // 由于我们在进行自定义绘制，因此不进行正常绘制
            return false;
        }

        // 找到剑的起始和结束位置，并使用线段碰撞检测与敌人检查碰撞
        public override bool? Colliding(Rectangle projHitbox, Microsoft.Xna.Framework.Rectangle targetHitbox)
        {
            Vector2 start = Owner.MountedCenter;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * ((Projectile.Size.Length()) * Projectile.scale * 1.18f);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 15f * Projectile.scale, ref collisionPoint);
        }

        // 对瓦片进行类似的碰撞检测
        public override void CutTiles()
        {
            Vector2 start = Owner.MountedCenter;
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * (Projectile.Size.Length() * Projectile.scale * 1.35f);
            Utils.PlotTileLine(start, end, 30 * Projectile.scale, DelegateMethods.CutTiles);
        }

        // 确保投射物仅在释放阶段和放松阶段造成伤害
        public override bool? CanDamage()
        {
            if (CurrentStage == AttackStage.Prepare)
                return false;
            return base.CanDamage();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 确保击退方向远离玩家
            modifiers.HitDirectionOverride = target.position.X > Owner.MountedCenter.X ? 1 : -1;
        }
        
        // 方便设置投射物和手臂位置的函数
        public void SetSwordPosition()
        {
            Projectile.rotation = InitialAngle + Projectile.spriteDirection * Progress; // 设置投射物的旋转

            // 设置复合手臂，允许你独立设置手臂的旋转和前后手臂的伸展
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(90f)); // 设置手臂位置（由于手臂起始时低下，所以有 90 度偏移）
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)Math.PI / 2); // 获取手的位置

            armPosition.Y += Owner.gfxOffY; // 添加偏移
            Projectile.Center = armPosition; // 设置投射物到手的位置
            Projectile.scale = Size * 1.2f * Owner.GetAdjustedItemScale(Owner.HeldItem); // 稍微放大投射物，也考虑到近战尺寸的修正

            Owner.heldProj = Projectile.whoAmI; // 设置持有的投射物为这个投射物
        }

        // 准备攻击的函数
        private void PrepareStrike()
        {
            Progress = WINDUP * SWINGRANGE * (1f - Timer / prepTime); // 从初始角度计算旋转
            Size = MathHelper.SmoothStep(0, 1, Timer / prepTime); // 增加大小

            if (Timer >= prepTime)
            {
                SoundEngine.PlaySound(SoundID.Item1); // 播放声音
                CurrentStage = AttackStage.Execute; // 进入执行阶段
            }
        }


        // 执行挥动的函数
        private void ExecuteStrike()
        {
            Progress = MathHelper.SmoothStep(0, SWINGRANGE, (1f - UNWIND) * Timer / execTime);

            if (Timer >= execTime)
            {
                CurrentStage = AttackStage.Unwind; // 完成攻击，进入放松阶段
            }
        }


        // 放松的函数，剑消失
        private void UnwindStrike()
        {
            Progress = MathHelper.SmoothStep(0, SWINGRANGE, (1f - UNWIND) + UNWIND * Timer / hideTime);
            Size = 1f - MathHelper.SmoothStep(0, 1, Timer / hideTime); // 逐渐减小大小

            if (Timer >= hideTime)
            {
                Projectile.Kill(); // 杀死投射物
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 mainDir = Projectile.rotation.ToRotationVector2();
            for (int i = 0; i < 3; i++)
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
            if (Main.rand.NextBool(3))
            {
                target.AddBuff(BuffID.OnFire, 60);
            }
        }
    }
}

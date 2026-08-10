using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.Weapons.Sword;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.WorldBuilding;
using static Terraria.GameContent.Animations.Actions.Sprites;

namespace SwordMastery.Content.Items.Weapons.Sword
{
    public class TianjingSword_1 : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 32;
            Item.crit = 12;
            Item.DamageType = DamageClass.Melee;
            Item.width = 58;
            Item.height = 58;
            Item.useTime = 25;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4;
            Item.value = Item.buyPrice(0,0,60,0);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;// 自动使用
            //Item.useTurn = true;// 自动转向
            Item.noMelee = true;
            Item.noUseGraphic = false;// 取消使用图标//false为显示使用图标
            Item.shoot = ModContent.ProjectileType<TianjingSword_1Proj>();
            Item.shootSpeed = 1f;
            //Item.createTile = ModContent.TileType<TianjingEggTile>(); // 设置物品放置时生成的瓦片
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2) // 右键射击
            {
                Item.noUseGraphic = true;
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<TianjingSword_1Proj_Right>(), 0, knockback, Main.myPlayer);
                return false;
            }
            else
                Item.noUseGraphic = false;
            return base.Shoot(player, source, position, velocity, type, damage, knockback);
        }
        public override bool AltFunctionUse(Player player)// 右键
        {
            return true;
        }
        //public override bool? UseItem(Player player)
        //{
        //    return base.UseItem(player);
        //}
        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 120);
            //var modPlayer = player.GetModPlayer<TianjingSwordUnlockPlayer>();
            //modPlayer.HasGotSword = false;
        }
    }
    public class TianjingSword_1Proj : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj";

        private int frameCounter = 0;
        private int PlayerDirection = 0;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60; // 挥砍动画持续时间（6帧*3=18帧，略微冗余）
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            PlayerDirection = player.direction;
            base.OnSpawn(source);
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.Center = player.Center;

            // 控制动画帧
            frameCounter++;
            if (frameCounter % 4 == 0)
            {
                Projectile.frame++;
                if (Projectile.frame >= 6)
                    Projectile.Kill();
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.Knockback.Base = 2f; // 只设置强度
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);

            // 只在本地玩家执行
            if (Main.myPlayer == Projectile.owner)
            {
                // 检查当前玩家是否已经有 TianjingSwordProj_Head 弹幕
                bool hasHead = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (proj.active && proj.owner == Projectile.owner && proj.type == ModContent.ProjectileType<TianjingSword_1Proj_Head>())
                    {
                        hasHead = true;
                        break;
                    }
                }

                // 如果没有，25%概率发射
                if (!hasHead && Main.rand.NextFloat() < 0.25f)
                {
                    Player player = Main.player[Projectile.owner];
                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(player.HeldItem),
                        player.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<TianjingSword_1Proj_Head>(),
                        Projectile.damage / 2,
                        Projectile.knockBack,
                        player.whoAmI
                    );
                }
                if (Main.rand.NextFloat() < 0.25f)
                    target.AddBuff(BuffID.OnFire, 120);
            }
        }
        public override void CutTiles()
        {
            Player player = Main.player[Projectile.owner];
            Vector2 center = player.Center;

            // 统一挥砍起始方向，并加上60度调整
            float baseAngle = PlayerDirection == 1
                ? MathHelper.ToRadians(-100f)
                : MathHelper.ToRadians(280f);

            float sweepRange = MathHelper.ToRadians(150f);

            float startAngle = baseAngle;
            float endAngle = baseAngle + (PlayerDirection == 1 ? sweepRange : -sweepRange);

            // 动画进度
            int maxFrame = 6;
            float progress = MathHelper.Clamp(Projectile.frame / (float)maxFrame, 0f, 1f);

            // 当前扫到的角度
            float currentAngle = MathHelper.Lerp(startAngle, endAngle, progress);

            float maxDist = 168f;
            float hitWidth = MathHelper.ToRadians(30f);

            // 以挥砍弧线为基准，绘制一条 tile 线，调用 DelegateMethods.CutTiles
            int step = 8;
            for (float angle = currentAngle - hitWidth / 2f; angle <= currentAngle + hitWidth / 2f; angle += hitWidth / step)
            {
                Vector2 dir = angle.ToRotationVector2();
                Vector2 start = center;
                Vector2 end = center + dir * maxDist;
                Utils.PlotTileLine(start, end, 16f, DelegateMethods.CutTiles);
            }

            base.CutTiles();
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 center = player.Center;
            Vector2 npcCenter = targetHitbox.Center.ToVector2();

            // 统一挥砍起始方向
            float baseAngle = PlayerDirection == 1
               ? MathHelper.ToRadians(-100f) // -40 - 60
               : MathHelper.ToRadians(280f); // 220 + 60

            float sweepRange = MathHelper.ToRadians(150f); // 总角度
            float startAngle = baseAngle;
            float endAngle = baseAngle + (PlayerDirection == 1 ? sweepRange : -sweepRange);

            // 动画进度
            int maxFrame = 6;
            float progress = MathHelper.Clamp(Projectile.frame / (float)maxFrame, 0f, 1f);

            // 当前扫到的角度
            float currentAngle = MathHelper.Lerp(startAngle, endAngle, progress);

            Vector2 toNpc = npcCenter - center;
            float dist = toNpc.Length();
            float maxDist = 168f + Math.Max(targetHitbox.Width, targetHitbox.Height) / 2f;

            // 判定：只判定当前扫过的角度±一定宽度
            float hitWidth = MathHelper.ToRadians(30f); // 挥砍宽度
            float angleToNpc = toNpc.SafeNormalize(Vector2.Zero).ToRotation();
            float diff = MathHelper.WrapAngle(angleToNpc - currentAngle);

            return dist < maxDist && Math.Abs(diff) < hitWidth / 2f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / 6;
            int frame = Projectile.frame;
            Rectangle sourceRect = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);

            Player player = Main.player[Projectile.owner];
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            // 旋转方向
            float rotation = PlayerDirection == 1 ? MathHelper.ToRadians(60) : -MathHelper.ToRadians(60);
            SpriteEffects effects = PlayerDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 a = PlayerDirection == -1 ? new Vector2(-52, -52) : new Vector2(52, -52);
            float b = PlayerDirection == -1 ? MathHelper.ToRadians(40) : -MathHelper.ToRadians(40);
            Main.EntitySpriteDraw(
                texture,
                player.Center - Main.screenPosition + a,
                sourceRect,
                lightColor,
                rotation + b,
                origin,
                Projectile.scale,
                effects,
                0
            );
            return false;
        }
    }
    public class TianjingSword_1Proj_Right : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSword_1";

        public override Color? GetAlpha(Color lightColor) { return Color.White; }
        private Player Owner => Main.player[Projectile.owner];

        private int Timer = 0;
        private bool hasSpawnedTowerProj = false;

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 22;
            Projectile.penetrate = -1;
            Projectile.scale = 1.0f;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;//物块碰撞
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.ai[0] == 1)
            {
                Projectile.scale = 0.2f;
            }
            base.OnSpawn(source);
        }
        public override void AI()
        {
            if(Projectile.ai[0] == 1)
            {
                if (Projectile.scale <= 1f)
                    Projectile.scale += 0.01f;
            }
            if (Main.mouseLeft) Projectile.Kill();
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2 * 0.5f;
            if (Main.mouseRight && Owner.HeldItem.type == ModContent.ItemType<TianjingSword_1>())
            {
                Projectile.timeLeft = 10;
            }
            SetSwordPosition();

            // 查找最近的晶塔（frame 6: 雪地，frame 5: 沙漠）
            bool foundTower = false;
            Vector2? towerPos = null;
            int foundFrame = -1;
            FindNearestTower(Owner.Center.ToTileCoordinates(), 20, ref foundTower, ref towerPos, ref foundFrame);

            if (!hasSpawnedTowerProj && foundTower && towerPos.HasValue)
            {
                int projType = ModContent.ProjectileType<TianjingSwordProj_Right_>();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    towerPos.Value + new Vector2(-15, 0),
                    Vector2.Zero,
                    projType,
                    0,
                    0,
                    Projectile.owner,
                    Projectile.whoAmI,
                    foundFrame
                );
                hasSpawnedTowerProj = true;
            }

            // 只有有晶塔时才递增Timer和缩小
            if (foundTower)
            {
                Timer++;
                if (Timer % 30 == 0)
                    hasSpawnedTowerProj = false;
                if (Timer > 60 && Projectile.ai[0] != 1)
                {
                    Projectile.scale -= 0.01f;
                    if (Projectile.scale < 0.2f)
                    {
                        if (Owner.HeldItem.type == ModContent.ItemType<TianjingSword_1>())
                        {
                            if (FrameToSwordMap.TryGetValue(foundFrame, out var mapping))
                            {
                                Owner.inventory[Owner.selectedItem].SetDefaults(mapping.itemType);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromAI(),
                                    Owner.position,
                                    Vector2.Zero,
                                    mapping.projType,
                                    0,
                                    0,
                                    Projectile.owner,
                                    1
                                );
                            }
                        }
                        Projectile.Kill();
                    }
                }
            }
        }
        // 定义frame与物品/弹幕的映射表
        private static readonly Dictionary<int, (int itemType, int projType)> FrameToSwordMap = new()
        {
            { 6, (ModContent.ItemType<TianjingSword_0>(), ModContent.ProjectileType<TianjingSword_0Proj_Right>()) }, // 雪地
            { 5, (ModContent.ItemType<TianjingSword>(), ModContent.ProjectileType<TianjingSwordProj_Right>()) }, // 沙漠
            { 3, (ModContent.ItemType<TianjingSword_2>(), ModContent.ProjectileType<TianjingSword_2Proj_Right>()) }, // 洞穴
            { 1, (ModContent.ItemType<TianjingSword_3>(), ModContent.ProjectileType<TianjingSword_3Proj_Right>()) }, // 丛林
            { 2, (ModContent.ItemType<TianjingSword_5>(), ModContent.ProjectileType<TianjingSword_5Proj_Right>()) }, // 神圣
            
            // 以后有新的晶塔只需加一行
            // { 4, (ModContent.ItemType<TianjingSword_2>(), ModContent.ProjectileType<TianjingSword_2Proj_Right>()) },
        };
        // 查找最近的晶塔（frame 6: 雪地，frame 5: 沙漠）
        private void FindNearestTower(Point center, int radius, ref bool found, ref Vector2? pos, ref int frame)
        {
            float minDist = float.MaxValue;
            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) continue;
                    Tile tile = Main.tile[x, y];
                    int tileFrame = tile.TileFrameX / 54;
                    if (tile != null && tile.TileType == TileID.TeleportationPylon && (tileFrame == 6 || tileFrame == 5 || tileFrame == 3 || tileFrame == 1 || tileFrame == 2))
                    {
                        Vector2 candidatePos = new Vector2(x * 16 + 24, y * 16 + 12);
                        float dist = Vector2.Distance(candidatePos, center.ToVector2() * 16);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            found = true;
                            pos = candidatePos;
                            frame = tileFrame;
                        }
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D textureToDraw = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Sword/TianjingSword_1").Value;
            Player player = Main.player[Projectile.owner];

            // 以底部中心为缩放锚点
            Vector2 origin = player.direction == 1 ? new Vector2(0f, textureToDraw.Height) : new Vector2(0f, 0f);
            Vector2 drawPos = Projectile.position - Main.screenPosition;

            Main.spriteBatch.Draw(
                textureToDraw,
                drawPos,
                null,
                lightColor,
                Projectile.rotation + (player.direction == 1 ? MathHelper.ToRadians(0) : MathHelper.ToRadians(-90)),
                origin,
                Projectile.scale,
                player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically,
                0f
            );
            return false;
        }
        // 方便设置投射物和手臂位置的函数
        public void SetSwordPosition()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2 * 0.5f;

            // 设置复合手臂，允许你独立设置手臂的旋转和前后手臂的伸展
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(140f)); // 设置手臂位置（由于手臂起始时低下，所以有 90 度偏移）
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)Math.PI / 2); // 获取手的位置

            armPosition.Y += Owner.gfxOffY; // 添加偏移
            Projectile.Center = armPosition + (Owner.direction == 1 ? new Vector2(5, 10) : new Vector2(5, 15)); ; // 设置投射物到手的位置

            Owner.heldProj = Projectile.whoAmI; // 设置持有的投射物为这个投射物
        }
        [Obsolete]
        public override void Kill(int timeLeft)
        {

        }
    }
    public class TianjingSword_1Proj_Head : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj_Head";

        private float lemniscateT = 0f; // 8字形参数
        private readonly float lemniscateSpeed = 0.05f; // 控制运动快慢
        private Vector2 centerPos; // 8字形中心（目标点）
        private float playerCircleT = 0f; // 玩家绕圈参数

        private Vector2 lastDesiredPos;
        private Vector2 playerCircleTarget = Vector2.Zero;
        private bool isTracking = true; // 追踪状态
        private NPC trackedTarget = null; // 当前追踪目标
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void AI()
        {
            // 只在首次生成时创建体节
            if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] == 0f)
            {
                int[] bodyTypes = new int[]
                {
                ModContent.ProjectileType<TianjingSword_1Proj_Body_1>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_0>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_1>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_1>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_1>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_1>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_0>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_1>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_1>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_2>(),
                ModContent.ProjectileType<TianjingSword_1Proj_Body_3>()
                };
                int prev = Projectile.whoAmI;
                for (int i = 0; i < bodyTypes.Length; i++)
                {
                    int bodyIndex = Projectile.NewProjectile(
                        Projectile.GetSource_FromAI(),
                        Projectile.Center,
                        Vector2.Zero,
                        bodyTypes[i],
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        prev,
                        i
                    );
                    prev = bodyIndex;
                }
                int tailType = ModContent.ProjectileType<TianjingSword_1Proj_Tail>();
                Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center,
                    Vector2.Zero,
                    tailType,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    prev
                );
                Projectile.localAI[0] = 1f;
            }
            if (Projectile.ai[0] == 1)
            {
                if (isTracking)
                {
                    // 如果没有目标或目标已失效，重新锁定最近敌人
                    if (trackedTarget == null || !trackedTarget.active || trackedTarget.friendly || trackedTarget.dontTakeDamage || !trackedTarget.CanBeChasedBy())
                    {
                        float minDist_ = 200f;
                        NPC nearest = null;
                        Player player_ = Main.player[Projectile.owner];
                        foreach (var npc in Main.npc)
                        {
                            if (!npc.active || npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy()) continue;
                            float dist = Vector2.Distance(Projectile.Center, npc.Center);
                            if (dist < minDist_)
                            {
                                minDist_ = dist;
                                nearest = npc;
                            }
                        }
                        trackedTarget = nearest;
                    }

                    // 有目标则追踪
                    if (trackedTarget != null)
                    {
                        Vector2 toTarget = trackedTarget.Center - Projectile.Center;
                        float speed_ = 18f;
                        Vector2 desiredVelocity = toTarget.SafeNormalize(Vector2.Zero) * speed_;
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.18f);
                    }
                }
                return;
            }
            // 1. 寻找敌人
            Player player = Main.player[Projectile.owner];
            NPC target = null;
            float minDist = 1200f;
            foreach (var npc in Main.npc)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy()) continue;
                float dist = Vector2.Distance(player.Center, npc.Center);
                if (dist < minDist)
                {
                    minDist = dist;
                    target = npc;
                }
            }

            Vector2 desiredPos;
            if (target == null)
            {
                
                Vector2 center = player.Center;
                playerCircleT += 0.06f;
                if (playerCircleT > MathHelper.TwoPi)
                    playerCircleT -= MathHelper.TwoPi;
                Vector2 circlePos = center + new Vector2(
                    310f * (float)Math.Cos(playerCircleT),
                    310f * (float)Math.Sin(playerCircleT)
                );
                // 1. 目标点缓动
                if (playerCircleTarget == Vector2.Zero)
                    playerCircleTarget = Projectile.Center;
                playerCircleTarget = Vector2.Lerp(playerCircleTarget, circlePos, 0.10f);
                desiredPos = playerCircleTarget;
            }
            else
            {
                centerPos = target.Center;
                lemniscateT += lemniscateSpeed;
                if (lemniscateT > MathHelper.TwoPi)
                    lemniscateT -= MathHelper.TwoPi;
                float x = 400 * (float)Math.Sin(lemniscateT);
                float y = 400 * (float)Math.Sin(lemniscateT) * (float)Math.Cos(lemniscateT);
                desiredPos = centerPos + new Vector2(x, y);
                // 重置playerCircleTarget，避免切回玩家时突变
                playerCircleTarget = desiredPos;
            }

            // 直接用desiredPos，不用lastDesiredPos
            Vector2 toDesired = desiredPos - Projectile.Center;
            float speed = 12f;
            if (target != null)
                speed = MathHelper.Lerp(12f, 16f, Math.Abs((float)Math.Cos(lemniscateT)));
            Vector2 targetVelocity = toDesired.SafeNormalize(Vector2.Zero) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetVelocity, 0.15f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() < 0.25f)
                target.AddBuff(BuffID.OnFire, 60);
            isTracking = false;
            if (Projectile.ai[1] == 1)
                NpcUtils.DealTruePercentDamage(target, 0.01f, hit.HitDirection);
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // 加载头部贴图
            Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj_Head").Value;
            int frameHeight = texture.Height / 3;

            // 计算运动方向
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 headPos = Projectile.Center;

            // 检查前方100像素内有无敌人
            bool enemyAhead = false;
            foreach (NPC npc in Main.npc)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy()) continue;
                Vector2 toNpc = npc.Center - headPos;
                float dist = toNpc.Length();
                if (dist < 200f && Vector2.Dot(toNpc.SafeNormalize(Vector2.Zero), dir) > 0.7f)
                {
                    enemyAhead = true;
                    break;
                }
            }
            int frame = enemyAhead ? 2 : 0;
           
            // 绘制
            SpriteBatch spriteBatch = Main.spriteBatch;
            Rectangle sourceRect = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);
            float rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            SpriteEffects effects = Projectile.direction != 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // 计算淡出透明度
            float alpha = 1f;
            if (Projectile.timeLeft < 80) // 2秒 = 120帧
            {
                alpha = Projectile.timeLeft / 80f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
            }
            Color fadeColor = lightColor * alpha;

            spriteBatch.Draw(
                texture,
                Projectile.Center - Main.screenPosition,
                sourceRect,
                fadeColor,
                rotation,
                origin,
                1f,
                effects,
                0f
            );
            return false;
        }
    }
    // 以Body_0为例
    public class TianjingSword_1Proj_Body_0 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj_Body_0";

        private int Direction = 0;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void AI()
        {
            int prevWhoAmI = (int)Projectile.ai[0];
            if (prevWhoAmI < 0 || prevWhoAmI >= Main.maxProjectiles) return;
            Projectile prevProj = Main.projectile[prevWhoAmI];
            if (!prevProj.active)
            {
                Projectile.Kill();
                return;
            }
            Vector2 toPrev = prevProj.Center - Projectile.Center;
            float desiredDist = 36f; // 可根据贴图调整
            if (toPrev.Length() > desiredDist)
            {
                Projectile.Center += toPrev.SafeNormalize(Vector2.Zero) * (toPrev.Length() - desiredDist);
            }
            Projectile.rotation = toPrev.ToRotation() + MathHelper.PiOver2;
            // 关键：独立判断朝向
            Direction = (toPrev.X < 0) ? -1 : 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() < 0.25f)
                target.AddBuff(BuffID.OnFire, 60);
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // 计算淡出透明度
            float alpha = 1f;
            if (Projectile.timeLeft < 80) // 2秒 = 120帧
            {
                alpha = Projectile.timeLeft / 80f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
            }
            Color fadeColor = lightColor * alpha;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                fadeColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );
            //Main.NewText(toPrev.X);
            return false;
        }
    }

    // 以Body_1为例
    public class TianjingSword_1Proj_Body_1 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj_Body_1";

        private int Direction = 0;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void AI()
        {
            int prevWhoAmI = (int)Projectile.ai[0];
            if (prevWhoAmI < 0 || prevWhoAmI >= Main.maxProjectiles) return;
            Projectile prevProj = Main.projectile[prevWhoAmI];
            if (!prevProj.active)
            {
                Projectile.Kill();
                return;
            }
            Vector2 toPrev = prevProj.Center - Projectile.Center;
            float desiredDist = 36f;
            if (toPrev.Length() > desiredDist)
            {
                Projectile.Center += toPrev.SafeNormalize(Vector2.Zero) * (toPrev.Length() - desiredDist);
            }
            Projectile.rotation = toPrev.ToRotation() + MathHelper.PiOver2;
            // 关键：独立判断朝向
            Direction = (toPrev.X < 0) ? -1 : 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() < 0.25f)
                target.AddBuff(BuffID.OnFire, 60);
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // 计算淡出透明度
            float alpha = 1f;
            if (Projectile.timeLeft < 80) // 2秒 = 120帧
            {
                alpha = Projectile.timeLeft / 80f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
            }
            Color fadeColor = lightColor * alpha;
            // 绘制本体
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                fadeColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );
            return false;
        }
    }

    // 以Body_2为例
    public class TianjingSword_1Proj_Body_2 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj_Body_2";

        private int Direction = 0;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void AI()
        {
            int prevWhoAmI = (int)Projectile.ai[0];
            if (prevWhoAmI < 0 || prevWhoAmI >= Main.maxProjectiles) return;
            Projectile prevProj = Main.projectile[prevWhoAmI];
            if (!prevProj.active)
            {
                Projectile.Kill();
                return;
            }
            Vector2 toPrev = prevProj.Center - Projectile.Center;
            float desiredDist = 36f;
            if (toPrev.Length() > desiredDist)
            {
                Projectile.Center += toPrev.SafeNormalize(Vector2.Zero) * (toPrev.Length() - desiredDist);
            }
            Projectile.rotation = toPrev.ToRotation() + MathHelper.PiOver2;
            // 关键：独立判断朝向
            Direction = (toPrev.X < 0) ? -1 : 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() < 0.25f)
                target.AddBuff(BuffID.OnFire, 60);
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // 计算淡出透明度
            float alpha = 1f;
            if (Projectile.timeLeft < 80) // 2秒 = 120帧
            {
                alpha = Projectile.timeLeft / 80f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
            }
            Color fadeColor = lightColor * alpha;
            // 绘制本体
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                fadeColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );
            return false;
        }
    }

    // 以Body_3为例
    public class TianjingSword_1Proj_Body_3 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj_Body_3";

        private int Direction = 0;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void AI()
        {
            int prevWhoAmI = (int)Projectile.ai[0];
            if (prevWhoAmI < 0 || prevWhoAmI >= Main.maxProjectiles) return;
            Projectile prevProj = Main.projectile[prevWhoAmI];
            if (!prevProj.active)
            {
                Projectile.Kill();
                return;
            }
            Vector2 toPrev = prevProj.Center - Projectile.Center;
            float desiredDist = 36f;
            if (toPrev.Length() > desiredDist)
            {
                Projectile.Center += toPrev.SafeNormalize(Vector2.Zero) * (toPrev.Length() - desiredDist);
            }
            Projectile.rotation = toPrev.ToRotation() + MathHelper.PiOver2;
            // 关键：独立判断朝向
            Direction = (toPrev.X < 0) ? -1 : 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() < 0.25f)
                target.AddBuff(BuffID.OnFire, 60);
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // 计算淡出透明度
            float alpha = 1f;
            if (Projectile.timeLeft < 80) // 2秒 = 120帧
            {
                alpha = Projectile.timeLeft / 80f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
            }
            Color fadeColor = lightColor * alpha;
            // 绘制本体
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                fadeColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );
            return false;
        }
    }
    public class TianjingSword_1Proj_Tail : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSword_1Proj_Tail";

        private int Direction = 0;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.alpha = 100;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }
        public override void AI()
        {
            // 跟随最后一节身体弹幕
            int prevWhoAmI = (int)Projectile.ai[0];
            Projectile prevProj = Main.projectile[prevWhoAmI];
            Vector2 toPrev = prevProj.Center - Projectile.Center;
            float desiredDist = 36f;
            if (toPrev.Length() > desiredDist)
            {
                Projectile.Center += toPrev.SafeNormalize(Vector2.Zero) * (toPrev.Length() - desiredDist);
            }
            Projectile.rotation = toPrev.ToRotation() + MathHelper.PiOver2;
            // 关键：独立判断朝向
            Direction = (toPrev.X < 0) ? -1 : 1;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.rand.NextFloat() < 0.25f)
                target.AddBuff(BuffID.OnFire, 60);
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            var texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            SpriteEffects effects = Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // 计算淡出透明度
            float alpha = 1f;
            if (Projectile.timeLeft < 80) // 2秒 = 120帧
            {
                alpha = Projectile.timeLeft / 80f;
                alpha = MathHelper.Clamp(alpha, 0f, 1f);
            }
            Color fadeColor = lightColor * alpha;
            // 绘制本体
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                fadeColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );
            return false;
        }
    }
}

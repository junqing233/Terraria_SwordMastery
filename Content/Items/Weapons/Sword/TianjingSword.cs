using Microsoft.CodeAnalysis.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.Weapons.Sword;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.WorldBuilding;

namespace SwordMastery.Content.Items.Weapons.Sword
{
    public class TianjingSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 16;
            Item.crit = 4;
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
            Item.shoot = ModContent.ProjectileType<TianjingSwordProj>();
            Item.shootSpeed = 1f;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
           
        }
        public override bool? UseItem(Player player)
        {
            //if (player.altFunctionUse == 2) // 右键射击
                return true;
            //else return false;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2) // 右键射击
            {
                Item.noUseGraphic = true;
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<TianjingSwordProj_Right>(), 0, knockback, Main.myPlayer);
                return false;
            }
            else Item.noUseGraphic = false;
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

    }
    public class TianjingEggTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Width = 3; // 设置宽度为4
            TileObjectData.newTile.Height = 3; // 设置高度为4
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16}; // 设置每一行的高度
            TileObjectData.newTile.CoordinateWidth = 16; // 设置每一列的宽度
            TileObjectData.newTile.CoordinatePadding = 2; // 读取贴图时的间隔
            TileObjectData.addTile(Type); // 添加瓦片数据
            // 设置地图条目和颜色
            AddMapEntry(new Color(200, 200, 200), Language.GetText(Language.ActiveCulture.Name == "zh-Hans" ? "天晶蛋" : "Tianjing Egg"));
        }
        public override void MouseOver(int i, int j) // 鼠标悬停
        {
            Player player = Main.LocalPlayer; // 获取本地玩家
            player.cursorItemIconText = (string)Language.GetText(Language.ActiveCulture.Name == "zh-Hans" ? "一颗孕育着神兵的蛋" : "An egg pregnant with a godly weapon");
        }
        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(new Terraria.DataStructures.EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 16, ModContent.ItemType<TianjingSword>());
            var source = new Terraria.DataStructures.EntitySource_TileBreak(i, j);
            Vector2 pos = new Vector2(i * 16, j * 16);
            // 随机速度
            Vector2 vel0 = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 2f;
            Vector2 vel1 = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 2f;
            Gore.NewGore(source, pos, vel0, Mod.Find<ModGore>("TianjingEgg_Gore_0").Type, 1f);
            Gore.NewGore(source, pos, vel1, Mod.Find<ModGore>("TianjingEgg_Gore_1").Type, 1f);
        }
    }
    
    public class TianjingEggWorldGen : ModSystem
    {
        // 本示例展示了在世界生成过程中放置瓦砾方块。
        public class RubbleWorldGen : ModSystem
        {
            public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
            {
                // 在 "Piles" 通过之后立即添加一个 GenPass。ExampleOreSystem 详细解释了这种方法。
                int PilesIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Piles"));
                if (PilesIndex != -1)
                {
                    tasks.Insert(PilesIndex + 1, new TianjingEggPilesPass("Tianjing Egg", 100f));
                }
            }
        }

        // ExamplePilesPass 是一个自定义的 GenPass，用于生成瓦砾方块。
        public class TianjingEggPilesPass : GenPass
        {
            public TianjingEggPilesPass(string name, float loadWeight) : base(name, loadWeight)
            {
            }

            protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
            {
                progress.Message = "Tianjing Egg";
                int[] tileTypes = [
                    ModContent.TileType<TianjingEggTile>(),
                ];

                // 为了不显得过于频繁，我们只在生成点附近放置15个 Example 瓦砾。
                // 本示例使用“尝试直到成功”的方法：https://github.com/tModLoader/tModLoader/wiki/World-Generation#try-until-success
                for (int k = 0; k < 1; k++)
                {
                    bool success = false;
                    int attempts = 0;
                    while (!success)
                    {
                        attempts++;
                        if (attempts > 1000)
                        {
                            break;
                        }
                        int x = WorldGen.genRand.Next(Main.maxTilesX / 2 - 200, Main.maxTilesX / 2 + 200);
                        int y = WorldGen.genRand.Next((int)GenVars.worldSurfaceLow, (int)GenVars.worldSurfaceHigh);
                        int tileType = WorldGen.genRand.Next(tileTypes);
                        
                        if (Main.tile[x, y].TileType == tileType)
                        {
                            continue;
                        }

                        WorldGen.PlaceTile(x, y, tileType, mute: true);
                        success = Main.tile[x, y].TileType == tileType;
                    }
                }
            }
        }
    }
    public class TianjingSwordProj : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj";

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
                    if (proj.active && proj.owner == Projectile.owner && proj.type == ModContent.ProjectileType<TianjingSwordProj_Head>())
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
                        ModContent.ProjectileType<TianjingSwordProj_Head>(),
                        Projectile.damage / 2,
                        Projectile.knockBack,
                        player.whoAmI
                    );
                }
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

            int step = 8; // 角度步进，越小越精细
            //for (float angle = currentAngle - hitWidth / 2f; angle <= currentAngle + hitWidth / 2f; angle += hitWidth / step)
            //{
            //    Vector2 dir = angle.ToRotationVector2();
            //    for (float d = 16f; d <= maxDist; d += 8f)
            //    {
            //        Vector2 checkPos = center + dir * d;
            //        int tileX = (int)(checkPos.X / 16f);
            //        int tileY = (int)(checkPos.Y / 16f);

            //        if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY)
            //            continue;

            //        // 尝试切割该 tile
            //        WorldGen.KillTile(tileX, tileY, false, false, false);
            //        if (Main.netMode == NetmodeID.MultiplayerClient)
            //        {
            //            NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, tileX, tileY);
            //        }
            //    }
            //}
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

            // 统一挥砍起始方向，并加上60度调整
            float baseAngle = PlayerDirection == 1
                ? MathHelper.ToRadians(-100f) // -40 - 60
                : MathHelper.ToRadians(280f); // 220 + 60

            float sweepRange = MathHelper.ToRadians(150f); //总角度

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
    public class TianjingSwordProj_Right : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSword";

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
            if (Projectile.ai[0] == 1)
            {
                if (Projectile.scale < 1f)
                    Projectile.scale += 0.01f;
            }
            if(Main.mouseLeft) Projectile.Kill();
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2 * 0.5f;
            if (Main.mouseRight && Owner.HeldItem.type == ModContent.ItemType<TianjingSword>())
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
                        if (Owner.HeldItem.type == ModContent.ItemType<TianjingSword>())
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
            { 6, (ModContent.ItemType<TianjingSword_0>(), ModContent.ProjectileType<TianjingSword_0Proj_Right>()) }, // 雪原
            { 5, (ModContent.ItemType<TianjingSword_1>(), ModContent.ProjectileType<TianjingSword_1Proj_Right>()) }, // 沙漠
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
            Texture2D textureToDraw = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Sword/TianjingSword").Value;
            Player player = Main.player[Projectile.owner];

            // 以底部中心为缩放锚点
            Vector2 origin = player.direction == 1 ? new Vector2(0f, textureToDraw.Height): new Vector2(0f, 0f);
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
    public class TianjingSwordProj_Right_ : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSword";

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ownerHitCheck = true;
            Projectile.timeLeft = 60;
            Projectile.width = 10;
            Projectile.height = 10;
        }

        public override void AI()
        {
            int mainProjWhoAmI = (int)Projectile.ai[0];
            if (mainProjWhoAmI >= 0 && mainProjWhoAmI < Main.maxProjectiles)
            {
                Projectile mainProj = Main.projectile[mainProjWhoAmI];
                if (mainProj != null && mainProj.active && 
                    (mainProj.type == ModContent.ProjectileType<TianjingSwordProj_Right>()
                    || mainProj.type == ModContent.ProjectileType<TianjingSword_0Proj_Right>()
                    || mainProj.type == ModContent.ProjectileType<TianjingSword_1Proj_Right>()
                    || mainProj.type == ModContent.ProjectileType<TianjingSword_2Proj_Right>()
                    || mainProj.type == ModContent.ProjectileType<TianjingSword_3Proj_Right>()
                    || mainProj.type == ModContent.ProjectileType<TianjingSword_4Proj_Right>()
                    || mainProj.type == ModContent.ProjectileType<TianjingSword_5Proj_Right>()

                    ))
                {
                    Vector2 toMain = mainProj.position - Projectile.position + new Vector2(0, -30);
                    float speed = 12f;
                    if (toMain.Length() < speed)
                    {
                        Projectile.position = mainProj.position;
                        Projectile.Kill();
                        return;
                    }
                    Projectile.velocity = toMain.SafeNormalize(Vector2.Zero) * speed;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
                else
                {
                    Projectile.Kill();
                }
            }
            else
            {
                Projectile.Kill();
            }
            int dust = DustID.IceTorch;
            if (Projectile.ai[1] == 6)
            {
                dust = DustID.IceTorch;
            }
            else if(Projectile.ai[1] == 5)
            {
                dust = DustID.RedTorch;
            }
            else if (Projectile.ai[1] == 3)
            {
                dust = DustID.PurpleTorch;
            }
            else if (Projectile.ai[1] == 1)
            {
                dust = DustID.GreenTorch;
            }
            else if (Projectile.ai[1] == 2)
            {
                dust = DustID.GoldFlame;
            }
            // 粒子效果
            int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dust, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.0f);
            Main.dust[dustIndex].noGravity = true;
            Main.dust[dustIndex].velocity *= 0.2f;
            Main.dust[dustIndex].scale *= 2.5f;
            Main.dust[dustIndex].noLight = true;
            Main.dust[dustIndex].fadeIn = 1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
    public class TianjingSwordProj_Head : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj_Head";

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
            isTracking = true; // 每次生成时重置为追踪
        }

        public override void AI()
        {
            // 只在首次生成时创建体节
            if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] == 0f)
            {
                int[] bodyTypes = new int[]
                {
                ModContent.ProjectileType<TianjingSwordProj_Body_1>(),
                ModContent.ProjectileType<TianjingSwordProj_Body_0>(),
                ModContent.ProjectileType<TianjingSwordProj_Body_1>(),
                ModContent.ProjectileType<TianjingSwordProj_Body_1>(),
                ModContent.ProjectileType<TianjingSwordProj_Body_0>(),
                ModContent.ProjectileType<TianjingSwordProj_Body_2>(),
                ModContent.ProjectileType<TianjingSwordProj_Body_3>()
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
                int tailType = ModContent.ProjectileType<TianjingSwordProj_Tail>();
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
            //Main.NewText(Projectile.ai[1]);
            // 新的追踪运动方式
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
            
            // 1. 优先锁定距离玩家最近的敌人
            Player player = Main.player[Projectile.owner];
            NPC target = null;
            float minDist = 1200f;
            foreach (var npc in Main.npc)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy()) continue;
                float dist = Vector2.Distance(player.Center, npc.Center); // 以玩家为中心
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
                    250f * (float)Math.Cos(playerCircleT),
                    250f * (float)Math.Sin(playerCircleT)
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
                float x = 300 * (float)Math.Sin(lemniscateT);
                float y = 300 * (float)Math.Sin(lemniscateT) * (float)Math.Cos(lemniscateT);
                desiredPos = centerPos + new Vector2(x, y);
                // 重置playerCircleTarget，避免切回玩家时突变
                playerCircleTarget = desiredPos;
            }

            // 切换目标时，lastDesiredPos 不突变，平滑过渡
            if (lastDesiredPos == Vector2.Zero)
                lastDesiredPos = Projectile.Center;

            // 平滑靠近目标点
            lastDesiredPos = Vector2.Lerp(lastDesiredPos, desiredPos, 0.12f);

            // 速度控制
            float speed = 12f;
            if (target != null)
                speed = MathHelper.Lerp(12f, 16f, Math.Abs((float)Math.Cos(lemniscateT)));

            Vector2 toDesired = lastDesiredPos - Projectile.Center;
            Vector2 targetVelocity = toDesired.SafeNormalize(Vector2.Zero) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetVelocity, 0.15f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            isTracking = false; // 击中后不再追踪

            //// 额外造成敌人最大血量1%的真实伤害（至少1点，取整）
            //int extraDamage = Math.Max(1, (int)Math.Ceiling(target.lifeMax * 0.01f));
            //// 造成额外伤害，不触发击退和无敌帧
            //target.StrikeNPC(new NPC.HitInfo()
            //{
            //    Damage = extraDamage,
            //    Knockback = 0f,
            //    HitDirection = hit.HitDirection,
            //    Crit = false,
            //    DamageType = DamageClass.Default
            //}, fromNet: false);
            if (Projectile.ai[1] == 1)
            NpcUtils.DealTruePercentDamage(target, 0.01f, hit.HitDirection);
            base.OnHitNPC(target, hit, damageDone);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // 加载头部贴图
            Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj_Head").Value;
            int frameHeight = texture.Height / 3;
            //int frame = 0;

            // 计算运动方向
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 headPos = Projectile.Center;

            // 检查前方200像素内有无敌人
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
    public class TianjingSwordProj_Body_0 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj_Body_0";

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
    public class TianjingSwordProj_Body_1 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj_Body_1";

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
    public class TianjingSwordProj_Body_2 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj_Body_2";

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
    public class TianjingSwordProj_Body_3 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj_Body_3";

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
    public class TianjingSwordProj_Tail : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TianjingSwordProj/TianjingSwordProj_Tail";

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
            if (!prevProj.active)
            {
                Projectile.Kill();
                return;
            }
            if (toPrev.Length() > desiredDist)
            {
                Projectile.Center += toPrev.SafeNormalize(Vector2.Zero) * (toPrev.Length() - desiredDist);
            }
            Projectile.rotation = toPrev.ToRotation() + MathHelper.PiOver2;
            // 关键：独立判断朝向
            Direction = (toPrev.X < 0) ? -1 : 1;
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

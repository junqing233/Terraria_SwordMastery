using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.Accessories;
using SwordMastery.Content.Items.FlyingSword.AGlobalControl;
using SwordMastery.Content.Items.Weapons.Miscellaneous;
using SwordMastery.Content.Items.Weapons.Sword;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static System.Net.WebRequestMethods;

namespace SwordMastery.Content.Items.Weapons.Magic
{
    public class AllCreationFallsNPC : GlobalNPC
    {
        public class AllCreationFallsConditions
        {
            public class Hardmode : IItemDropRuleCondition
            {
                public bool CanDrop(DropAttemptInfo info) =>
                    Main.dayTime;
                public bool CanShowItemDropInUI() => true;
                public string GetConditionDescription() => "在光之女皇白天受到攻击时掉落喵~";
            }
        }
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.HallowBoss)
            {
                // 困难模式
                npcLoot.Add(ItemDropRule.ByCondition(
                    new AllCreationFallsConditions.Hardmode(),
                    ModContent.ItemType<AllCreationFalls>(), 1, 1, 1));
            }
        }
    }
    public class AllCreationFallsTile : ModTile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Magic/AllCreationFallsTile";
        public static bool AllCreationFallsTileEnabled = false;
        
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileLavaDeath[Type] = true;

            TileID.Sets.FramesOnKillWall[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 4;
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16, 16, 16};
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 0;

            //AnimationFrameHeight = 54;
            TileObjectData.addTile(Type);
            AddMapEntry(Color.Gold, Language.GetText(Language.ActiveCulture.Name == "zh-Hans" ? "虚无魔镜" : "Void Mirror"));
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // 获取瓦片贴图
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // 计算瓦片左上角的屏幕坐标
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPos = new Vector2(i * 16, j * 16) - Main.screenPosition + zero;

            // 计算当前瓦片在大贴图中的偏移
            Tile tile = Main.tile[i, j];
            int frameX = tile.TileFrameX;
            int frameY = tile.TileFrameY;

            // 每一格16x16
            Rectangle sourceRect = new Rectangle(frameX, frameY, 16, 16);
            if (VoidMirrorUI.isClone)
                // 叠加高亮色
                spriteBatch.Draw(
                    texture,
                    drawPos,
                    sourceRect,
                    Color.White * 0.6f, // 可自定义颜色和透明度
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0f
                );
        }
        public override void NearbyEffects(int i, int j, bool closer)
        {
            Tile tile = Main.tile[i, j];
            
        }
        public override void PlaceInWorld(int i, int j, Item item)
        {
            AllCreationFallsTileEnabled = false;
            base.PlaceInWorld(i, j, item);
        }
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            AllCreationFallsTileEnabled = false;
            base.KillTile(i, j, ref fail, ref effectOnly, ref noItem);
        }
        
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemID.None;
            player.mouseInterface = true;

            //Player player = Main.LocalPlayer; // 获取本地玩家
            player.noThrow = 2; // 禁止投掷
            player.cursorItemIconEnabled = true;// 显示物品图标
            player.cursorItemIconID = ItemID.None; // 物品图标ID
            player.mouseInterface = true; // 鼠标接口开启

            // 我们可以通过获取方块样式并查找对应的物品掉落来确定光标上显示的物品。
            int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
            player.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
        }
        
        public override bool RightClick(int i, int j)
        {
            AllCreationFallsTileEnabled = !AllCreationFallsTileEnabled;
            SoundEngine.PlaySound(SoundID.Grab);

            Tile tile = Main.tile[i, j];
            int width = TileObjectData.GetTileData(Type, 0, 0).Width;
            int height = TileObjectData.GetTileData(Type, 0, 0).Height;

            // 反推左上角格子坐标
            int left = i - tile.TileFrameX / 16;
            int top = j - tile.TileFrameY / 16;

            // 计算瓦片中心
            Vector2 center = new Vector2(
                left * 16 + width * 16 / 2f,
                top * 16 + height * 16 / 2f - 50
            );
            int laserCount = 10;
            float shootSpeed = 12f;
            int shootDamage = 128;
            int owner = Main.myPlayer;
            float startTime = Main.GameUpdateCount;

            if(AllCreationFallsTileEnabled)
            {
                for (int k = 0; k < laserCount; k++)
                {
                    float angle = k * MathHelper.TwoPi / laserCount;
                    Vector2 velocity = angle.ToRotationVector2() * shootSpeed;
                    Vector2 spawnPos = center + velocity.SafeNormalize(Vector2.Zero) * 8f;
                    var proj = Projectile.NewProjectileDirect(
                        Terraria.Entity.GetSource_NaturalSpawn(),
                        spawnPos,
                        velocity,
                        ModContent.ProjectileType<AllCreationFallsProj>(),
                        shootDamage,
                        0f,
                        owner,
                        ai0: center.X, // 传递中心X
                        ai1: center.Y, // 传递中心Y
                        ai2: angle     // 传递初始角度
                    ).ModProjectile as AllCreationFallsProj;
                    proj.shootType = AllCreationFallsProj.ShootType.Tile;
                    proj.Projectile.localAI[0] = startTime; // 如需同步旋转起点
                }
            }
            
            return true;
        }
    }
    public class AllCreationFalls : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 128;
            Item.crit = 7;
            Item.DamageType = DamageClass.Magic;
            Item.width = 52;
            Item.height = 60;
            Item.maxStack = 1;
            Item.value = 1;
            Item.useAnimation = 30;//使用动画持续时间
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            //Item.UseSound = SoundID.Item100;
            Item.consumable = true;// 物品是否可消耗
            Item.noUseGraphic = true; // 确保图形显示
            Item.noMelee = true;
            Item.rare = ItemRarityID.Green; // 物品稀有度
            //价值
            Item.value = Item.sellPrice(4, 0, 50);
            Item.mana = 2;
            Item.shoot = ModContent.ProjectileType<AllCreationFallsProj_>();
            Item.createTile = ModContent.TileType<AllCreationFallsTile>();
        }
        public override bool AllowPrefix(int pre)
        {
            return false;
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "AllCreationFalls", "神兵") { OverrideColor = new Color(227, 188, 255) });
        }
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.shoot = ProjectileID.None;
                Item.createTile = ModContent.TileType<AllCreationFallsTile>();
                Item.consumable = true;

            }
            else
            {
                Item.shoot = ModContent.ProjectileType<AllCreationFallsProj_>();
                //Item.createTile = -1;
                Item.consumable = false;
            }
            return base.CanUseItem(player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<AllCreationFallsProj_>())
                {
                    return false;
                }
            }
            return true;
        }
    }
    public class AllCreationFallsProj_ : ModProjectile
    {
        private Player Owner => Main.player[Projectile.owner];
        public override string Texture => "SwordMastery/Content/Items/Weapons/Magic/AllCreationFalls"; // 使用物品的纹理作为投射物的纹理

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;// 使投射物不使用玩家的 gfxOffY 偏移
        }

        public override void SetDefaults()
        {
            Projectile.width = 40; // 投射物的碰撞箱宽度
            Projectile.height = 40; // 投射物的碰撞箱高度
            Projectile.friendly = true; // 投射物可以击中敌人
            Projectile.timeLeft = 60; // 投射物失效所需的时间
            Projectile.penetrate = -1; // 投射物无限穿透
            Projectile.tileCollide = false; // 投射物不与瓦片碰撞
            Projectile.usesLocalNPCImmunity = true; // 使用局部免疫帧
            Projectile.localNPCHitCooldown = -1; // 设置为 -1 以确保投射物不会命中两次
            Projectile.ownerHitCheck = true; // 确保投射物的拥有者有视线可以瞄准目标（即不能穿越瓦片击中目标）
            Projectile.DamageType = DamageClass.Magic; // 投射物为近战投射物
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Projectile.damage = 0;
            var player = Main.player[Projectile.owner];
            int shootDamage = Projectile.originalDamage;
            int laserCount = 10;
            float shootSpeed = 12f;
            for (int i = 0; i < laserCount; i++)
            {
                float angle = i * MathHelper.TwoPi / laserCount;
                Vector2 velocity = angle.ToRotationVector2() * shootSpeed;
                Vector2 spawnPos = Projectile.Center + velocity.SafeNormalize(Vector2.Zero) * 8f;
                int newProj_ = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<AllCreationFallsProj>(),
                    shootDamage,
                    0f,
                    player.whoAmI,
                    ai0: Projectile.whoAmI,
                    ai1: angle
                );
                if (newProj_ >= 0 && newProj_ < Main.maxProjectiles)
                {
                    Main.projectile[newProj_].ai[0] = Projectile.whoAmI;
                    Main.projectile[newProj_].ai[1] = angle;
                }
            }
        }

        public override void AI()
        {
            var player = Main.player[Projectile.owner];
            if (player.HeldItem.type != ModContent.ItemType<AllCreationFalls>())
            {
                Projectile.Kill();
                return;
            }
            if (Main.MouseWorld.X < player.Center.X)
                player.direction = -1;
            else
                player.direction = 1;

            Projectile.frameCounter++;
            if (Main.mouseLeft)
            {
                Projectile.spriteDirection = Main.MouseWorld.X > Owner.MountedCenter.X ? 1 : -1;
                Projectile.timeLeft = 10;
                SetSwordPosition();
                return;
            }
            else
            {
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            //Texture2D texture_ = ModContent.Request<Texture2D>("Pokemon/Content/Weapons/Magic/Sphere/PichuElectricSphereProj_F").Value;
            //获取玩家
            var player = Main.player[Projectile.owner];
            float rotationOffset = MathHelper.ToRadians(90f);
            SpriteEffects effects = SpriteEffects.None; ;
            // 计算每一帧的宽度和高度
            int frameWidth = texture.Width; // 假设整个纹理的宽度是固定的
            int frameHeight = texture.Height / Main.projFrames[Type]; // 总高度根据帧数分配

            // 计算当前帧的绘制区域
            Rectangle frameRectangle = new Rectangle(0, frameHeight * Projectile.frame, frameWidth, frameHeight);  // 使用当前帧的Y坐标位置

            // 计算绘制位置
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // 绘制当前帧
            Main.spriteBatch.Draw(texture, drawPosition, frameRectangle, lightColor * Projectile.Opacity, Projectile.rotation + rotationOffset,
                                  new Vector2(frameWidth * 0.5f, frameHeight * 0.5f), Projectile.scale * 0.8f, effects, 0);
            //Main.spriteBatch.Draw(texture_, drawPosition, frameRectangle, Color.White * Projectile.Opacity, Projectile.rotation + rotationOffset,
            //                     new Vector2(frameWidth * 0.5f, frameHeight * 0.5f), Projectile.scale, effects, 0);
            // 由于我们在进行自定义绘制，因此不进行正常绘制
            return false;
        }


        public void SetSwordPosition() // 可以传入偏移距离参数，默认为10
        {
            var owner = Main.player[Projectile.owner];

            // 获取鼠标在世界中的位置
            Vector2 mousePosition = Main.MouseWorld;

            // 计算从玩家到鼠标的方向，获取旋转角度
            Vector2 directionToMouse = mousePosition - owner.Center;
            directionToMouse.Normalize(); // 标准化方向向量
            Projectile.rotation = directionToMouse.ToRotation(); // 设置投射物的旋转

            // 设置复合手臂，允许独立设置手臂的旋转
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - (owner.direction != -1 ? MathHelper.ToRadians(70f) : MathHelper.ToRadians(110f))); // 设置手臂位置（因为手臂起始时低下，所以有90度偏移）

            // 获取手的位置
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)Math.PI / 2f); // 获取手的位置

            Projectile.Center = armPosition + directionToMouse * 20f; // 将投射物的中心向鼠标方向偏移指定的距离

            Owner.heldProj = Projectile.whoAmI; // 设置持有的投射物为当前投射物
        }
    }
    public class AllCreationFallsProj : ModProjectile
    {
        private const float MaxDamageMultiplier = 1.5f;
        private const float BeamHitboxCollisionWidth = 22f;
        private const float VisualEffectThreshold = 0.1f;
        private const float OuterBeamOpacityMultiplier = 0.75f;
        private const float InnerBeamOpacityMultiplier = 0.1f;
        private const float BeamLightBrightness = 0.75f;

        internal enum ShootType
        {
            Item = -1,
            Tile = 1
        }
        internal ShootType shootType = ShootType.Item;

        private float BeamLength
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private float ChargeRatio
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private const float ChargeSpeed = 1f / 60f;
        private const float BeamLengthGrowSpeed = 6f; // 每帧增长速度

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.timeLeft = 120;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(BeamLength);
            writer.Write(ChargeRatio);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            BeamLength = reader.ReadSingle();
            ChargeRatio = reader.ReadSingle();
        }

        public override void AI()
        {
            Vector2 center;
            float angle;
            float rotateSpeed = 0.002f;
            float length = 800f;

            if (shootType == ShootType.Tile)
            {
                // 通过ai0/ai1传递的中心点
                center = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                float startTime = Projectile.localAI[0];
                float time = Main.GameUpdateCount - startTime;
                angle = Projectile.ai[2] + time * rotateSpeed;
                if(AllCreationFallsTile.AllCreationFallsTileEnabled)
                Projectile.timeLeft = 10;
            }
            else
            {
                // 原有逻辑
                int parentWhoAmI = (int)Projectile.ai[0];
                if (parentWhoAmI < 0 || parentWhoAmI >= Main.maxProjectiles ||
                    !Main.projectile[parentWhoAmI].active ||
                    Main.projectile[parentWhoAmI].type != ModContent.ProjectileType<AllCreationFallsProj_>() ||
                    Main.player[Projectile.owner].statMana <= 2)
                {
                    Projectile.Kill();
                    return;
                }
                if (Main.projectile[parentWhoAmI].active)
                {
                    Projectile.timeLeft = 10;
                }
                center = Main.projectile[parentWhoAmI].Center;
                angle = Projectile.ai[1] + Main.GameUpdateCount * rotateSpeed;
            }

            Projectile.Center = center;
            Vector2 tail = center + angle.ToRotationVector2() * length;
            Projectile.velocity = (tail - center).SafeNormalize(Vector2.UnitY);
            Projectile.rotation = angle;


            // 顺时针旋转速度（弧度/帧），可自行调整
            //float rotateSpeed = 0.002f;

            //// 当前角度 = 初始角度 + 时间 * 速度
            //float angle = Projectile.ai[1] + Main.GameUpdateCount * rotateSpeed;

            //// 激光长度
            //float length = 800f;

            // 起点固定
            //Projectile.Center = center;

            // 尾部坐标
            //Vector2 tail = center + angle.ToRotationVector2() * length;
            // 让velocity指向当前角度
            //Projectile.velocity = (tail - center).SafeNormalize(Vector2.UnitY);


            // 旋转角度用于绘制
            //Projectile.rotation = angle;

            // 激光长度从10递增到100（总长度20~200）
            if (BeamLength < 1000f)
            {
                BeamLength = Math.Min(BeamLength + BeamLengthGrowSpeed, 1000f);
            }
            
            // 自动充能
            ChargeRatio += ChargeSpeed;
            if (ChargeRatio > 1f)
                ChargeRatio = 1f;

            //Vector2 beamDir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);

            float damageMultiplier = MathHelper.Lerp(1f, MaxDamageMultiplier, ChargeRatio * ChargeRatio * ChargeRatio);
            Projectile.damage = (int)(Projectile.originalDamage * damageMultiplier);
            Projectile.friendly = true;

            Projectile.scale = MathHelper.Lerp(0.5f, 1.2f, ChargeRatio);
            Projectile.Opacity = MathHelper.Lerp(0.5f, 1f, ChargeRatio);

            //Projectile.rotation = beamDir.ToRotation();

            //Vector2 beamDims = new Vector2(Projectile.velocity.Length() * BeamLength * 2f, Projectile.width * Projectile.scale);

            //Color beamColor = GetOuterBeamColor();
            //if (ChargeRatio >= VisualEffectThreshold)
            //{
            //    if (Main.netMode != NetmodeID.Server)
            //        ProduceWaterRipples(beamDims);
            //}

            //DelegateMethods.v3_1 = beamColor.ToVector3() * BeamLightBrightness * ChargeRatio;
            //Utils.PlotTileLine(Projectile.Center - beamDir * BeamLength, Projectile.Center + beamDir * BeamLength, beamDims.Y, new Utils.TileActionAttempt(DelegateMethods.CastLight));
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;
            float _ = float.NaN;
            Vector2 beamDir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 beamStart = Projectile.Center - beamDir * BeamLength;
            Vector2 beamEnd = Projectile.Center + beamDir * BeamLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), beamStart, beamEnd, BeamHitboxCollisionWidth * Projectile.scale, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 beamDir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 startPosition = (Projectile.Center - beamDir * BeamLength).Floor() - Main.screenPosition;
            Vector2 endPosition = (Projectile.Center + beamDir * BeamLength).Floor() - Main.screenPosition;
            Vector2 drawScale = new Vector2(Projectile.scale);

            DrawBeam(Main.spriteBatch, texture, startPosition, endPosition, drawScale, Color.White * 0.5f);
            drawScale *= 0.5f;
            DrawBeam(Main.spriteBatch, texture, startPosition, endPosition, drawScale, Color.White);

            return false;
        }

        private void DrawBeam(SpriteBatch spriteBatch, Texture2D texture, Vector2 startPosition, Vector2 endPosition, Vector2 drawScale, Color beamColor)
        {
            Utils.LaserLineFraming lineFraming = new Utils.LaserLineFraming(DelegateMethods.RainbowLaserDraw);
            DelegateMethods.c_1 = beamColor;
            Utils.DrawLaser(spriteBatch, texture, startPosition, endPosition, drawScale, lineFraming);
        }
        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.TileActionAttempt cut = new Utils.TileActionAttempt(DelegateMethods.CutTiles);
            Vector2 beamDir = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
            Vector2 beamStart = Projectile.Center - beamDir * BeamLength;
            Vector2 beamEnd = Projectile.Center + beamDir * BeamLength;
            Utils.PlotTileLine(beamStart, beamEnd, Projectile.width * Projectile.scale, cut);
        }
    }
}

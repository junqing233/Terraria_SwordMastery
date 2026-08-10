using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.FlyingSword.BladeForge;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.Glaive_H
{
    class FlyingExcalibur : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingExcalibur").Value;
        private readonly Texture2D texture_ = ModContent.Request<Texture2D>("SwordMastery/Content/Items/FlyingSword/Glaive_H/FlyingExcalibur_").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }
        public override void SetDefaults()
        {
            //Item.CloneDefaults(ItemID.EmpressBlade);
            Item.damage = 36;
            Item.mana = 10;
            Item.width = 52;
            Item.height = 52;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 2.25f;
            Item.value = 20000;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<FlyingExcaliburProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsFlyingExcalibur>();
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
            float intensity = 0.84f; // 控制光芒强度，越小越淡
            //Color(253, 238, 0)
            //•	R: 253 / 255 ≈ 0.99
            //•	G: 238 / 255 ≈ 0.93
            //•	B: 0 / 255 ≈ 0
            Lighting.AddLight(Item.Center, 0.99f * intensity, 0.93f * intensity, 0f * intensity);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsFlyingExcalibur>(), 3600);
            player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);
            return false;
        }
        public override void AddRecipes()
        {        
            CreateRecipe()
               .AddIngredient(ItemID.HallowedBar, 12)
               .AddTile(ModContent.TileType<BladeForgeTile>()) // 恶魔祭坛
               .Register();
        }
    }
    public class FlyingExcaliburProj : ModProjectile
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/FlyingExcalibur";
        private int existTimer = 0; // 计时器
        public bool IsDerivedFromZenith = false; // 标记是否为天顶剑衍生
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
            //SoundEngine.PlaySound(SoundID.Item100); // 播放声音
            base.OnSpawn(source);
            existTimer = 0; // 计时器
            IsDerivedFromZenith = false;
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
                if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsFlyingExcalibur>()))
                {
                    Projectile.timeLeft = 2;
                }
                if (!player.HasBuff(ModContent.BuffType<BuffsFlyingExcalibur>())
                    || IsDerivedFromZenith) Projectile.Kill();
            }
            ////修改此参数以确定攻击范围
            //var MaxDis = 500;
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
            var player = Main.player[Projectile.owner];
            player.GetModPlayer<FlyingExcaliburPlayer>().ExcaliburattackspeedTimer = 120;

            float radius = 40f; // 距离目标中心的半径
            int projType = ModContent.ProjectileType<FlyingExcaliburProj_P>();
            int damage = (int)(Projectile.damage * Main.rand.NextFloat(0.8f, 1.2f)); // 可根据需要调整
            // 角度为弹幕到敌人转换
            float angle = (float)Math.Atan2(target.Center.X - Projectile.Center.X, target.Center.Y - Projectile.Center.Y);
            //随机值
            float rand = Main.rand.NextFloat(0.5f, 2f);
            Vector2 spawnPos = target.Center + radius * angle.ToRotationVector2();
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 0.2f; // 朝向目标中心

            if (target.CanBeChasedBy(Projectile.owner))
            {
                clown = 30;
                var proj = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                projType,
                damage / 2,
                Projectile.knockBack,
                Projectile.owner,
                rand, 1f);
                if (proj.ModProjectile is FlyingExcaliburProj_P customProj)
                    customProj.Initialize(1f, 30f, 0.8f);
                var proj_ = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    projType,
                    damage / 2,
                    Projectile.knockBack,
                    Projectile.owner,
                    rand);
                if (proj_.ModProjectile is FlyingExcaliburProj_P customProj_)
                    customProj_.Initialize(-1f, 30f, 0.8f);
                var proj__ = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    projType,
                    (int)((damage / 2) * (1 + Projectile.ai[0] / 80)),
                    Projectile.knockBack,
                    Projectile.owner,
                    rand);
                if (proj__.ModProjectile is FlyingExcaliburProj_P customProj__)
                    customProj__.Initialize(-3f, 30f, 0.2f + Projectile.ai[0] / 100);
                var proj___ = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    projType,
                    (int)((damage / 2) * (1 + Projectile.ai[0] / 80)),
                    Projectile.knockBack,
                    Projectile.owner,
                    rand);
                if (proj___.ModProjectile is FlyingExcaliburProj_P customProj___)
                    customProj___.Initialize(3f, 30f, 0.2f + Projectile.ai[0]/100);
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
        private bool flag = false;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture_ = TextureAssets.Projectile[Type].Value ;
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
            Color LightsColor = new Color(253, 238, 0);
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
    public class FlyingExcaliburProj_P : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Excalibur;

        // 自定义参数，替代 ai[0]、ai[1]、ai[2]
        private float direction;
        private float maxTime;
        private float scaleFactor;
        private float currentTime;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 30;
            Projectile.stopsDealingDamageAfterPenetrateHits = true;
            Projectile.aiStyle = -1;
            Projectile.noEnchantmentVisuals = true;

            // 默认参数
            direction = 1f;
            maxTime = 30f;
            scaleFactor = 1f;
            currentTime = 0f;
        }

        // 提供一个初始化方法，便于外部传参
        public void Initialize(float dir, float maxT, float scale)
        {
            direction = dir;
            maxTime = maxT;
            scaleFactor = scale;
            currentTime = 0f;
        }

        public override void AI()
        {
            currentTime++;
            Player player = Main.player[Projectile.owner];
            float percentageOfLife = currentTime / maxTime;
            float velocityRotation = Projectile.velocity.ToRotation();
            float adjustedRotation = MathHelper.Pi * direction * percentageOfLife + velocityRotation + direction * MathHelper.Pi + player.fullRotation;
            Projectile.rotation = adjustedRotation;

            float scaleMulti = 0.6f;
            float scaleAdder = 1f;

            Projectile.Center -= Projectile.velocity;
            Projectile.scale = (scaleAdder + percentageOfLife * scaleMulti) * scaleFactor;

            // 粒子特效
            float dustRotation = Projectile.rotation + Main.rand.NextFloatDirection() * MathHelper.PiOver2 * 0.7f;
            Vector2 dustPosition = Projectile.Center + dustRotation.ToRotationVector2() * 86f * Projectile.scale;
            Vector2 dustVelocity = (dustRotation + direction * MathHelper.PiOver2).ToRotationVector2();
            
            if (Main.rand.NextFloat() * 2.6f < Projectile.Opacity * 0.6f)
            {
                // 原版Excalibur颜色：Color.Gold, Color.White
                Color dustColor = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat() * 0.3f);
                Dust coloredDust = Dust.NewDustPerfect(Projectile.Center + dustRotation.ToRotationVector2() * (Main.rand.NextFloat() * 80f * Projectile.scale + 20f * Projectile.scale), DustID.FireworksRGB, dustVelocity * 1f, 100, dustColor, 0.4f);
                coloredDust.fadeIn = 0.4f + Main.rand.NextFloat() * 0.15f;
                coloredDust.noGravity = true;
            }

            // 视觉特效
            for (float i = -MathHelper.PiOver4; i <= MathHelper.PiOver4; i += MathHelper.PiOver2)
            {
                Rectangle rectangle = Utils.CenteredRectangle(Projectile.Center + (Projectile.rotation + i).ToRotationVector2() * 70f * Projectile.scale, new Vector2(60f * Projectile.scale, 60f * Projectile.scale));
                Projectile.EmitEnchantmentVisualsAt(rectangle.TopLeft(), rectangle.Width, rectangle.Height);
            }

            // 生命周期结束时销毁
            if (currentTime >= maxTime)
            {
                Projectile.Kill();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float coneLength = 86f * Projectile.scale;
            float collisionRotation = MathHelper.Pi * 2f / 25f * direction;
            float maximumAngle = MathHelper.PiOver4;
            float coneRotation = Projectile.rotation + collisionRotation;

            if (targetHitbox.IntersectsConeSlowMoreAccurate(Projectile.Center, coneLength, coneRotation, maximumAngle))
            {
                return true;
            }

            float backOfTheSwing = Utils.Remap(currentTime, maxTime * 0.3f, maxTime * 0.5f, 1f, 0f);
            if (backOfTheSwing > 0f)
            {
                float coneRotation2 = coneRotation - MathHelper.PiOver4 * direction * backOfTheSwing;
                if (targetHitbox.IntersectsConeSlowMoreAccurate(Projectile.Center, coneLength, coneRotation2, maximumAngle))
                {
                    return true;
                }
            }

            return false;
        }

        public override void CutTiles()
        {
            Vector2 starting = (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * 60f * Projectile.scale;
            Vector2 ending = (Projectile.rotation + MathHelper.PiOver4).ToRotationVector2() * 60f * Projectile.scale;
            float width = 60f * Projectile.scale;
            Utils.PlotTileLine(Projectile.Center + starting, Projectile.Center + ending, width, DelegateMethods.CutTiles);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.damage > 0)
                Projectile.damage = (int)(Projectile.damage * 0.8f);
            if (Projectile.ai[1] != 0)
            ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.Excalibur,
                 new ParticleOrchestraSettings { PositionInWorld = Main.rand.NextVector2FromRectangle(target.Hitbox) },
                 Projectile.owner);
            hit.HitDirection = Main.player[Projectile.owner].Center.X < target.Center.X ? 1 : -1;
        }

        public override bool CanHitPvp(Player target) => base.CanHitPvp(target);
        public override bool CanHitPlayer(Player target) => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 position = Projectile.Center - Main.screenPosition;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle sourceRectangle = texture.Frame(1, 4);
            Vector2 origin = sourceRectangle.Size() / 2f;
            float scale = Projectile.scale * 0.9f;
            SpriteEffects spriteEffects = !(direction >= 0f) ? SpriteEffects.FlipVertically : SpriteEffects.None;
            float percentageOfLife = currentTime / maxTime;
            float lerpTime = Utils.Remap(percentageOfLife, 0f, 0.6f, 0f, 1f) * Utils.Remap(percentageOfLife, 0.6f, 1f, 1f, 0f);
            float lightingColor = Lighting.GetColor(Projectile.Center.ToTileCoordinates()).ToVector3().Length() / (float)Math.Sqrt(3.0);
            lightingColor = Utils.Remap(lightingColor, 0.2f, 1f, 0f, 1f);

            Color backDarkColor = new Color(180, 160, 60);
            Color middleMediumColor = new Color(255, 255, 80);
            Color frontLightColor = new Color(255, 240, 150);

            Color whiteTimesLerpTime = Color.DarkSlateBlue * lerpTime * 0.5f;
            whiteTimesLerpTime.A = (byte)(whiteTimesLerpTime.A * (1f - lightingColor));
            Color faintLightingColor = whiteTimesLerpTime * lightingColor * 0.5f;
            faintLightingColor.G = (byte)(faintLightingColor.G * lightingColor);
            faintLightingColor.B = (byte)(faintLightingColor.R * (0.25f + lightingColor * 0.75f));

            Main.EntitySpriteDraw(texture, position, sourceRectangle, backDarkColor /* lightingColor*/ * lerpTime, Projectile.rotation + direction * MathHelper.PiOver4 * -1f * (1f - percentageOfLife), origin, scale, spriteEffects, 0f);
            Main.EntitySpriteDraw(texture, position, sourceRectangle, faintLightingColor * 0.15f, Projectile.rotation + direction * 0.01f, origin, scale, spriteEffects, 0f);
            Main.EntitySpriteDraw(texture, position, sourceRectangle, middleMediumColor /* lightingColor*/ * lerpTime * 0.3f, Projectile.rotation, origin, scale, spriteEffects, 0f);
            Main.EntitySpriteDraw(texture, position, sourceRectangle, frontLightColor /* lightingColor*/ * lerpTime * 0.5f, Projectile.rotation, origin, scale * 0.975f, spriteEffects, 0f);
            Main.EntitySpriteDraw(texture, position, texture.Frame(1, 4, 0, 3), Color.White * 0.6f * lerpTime, Projectile.rotation + direction * 0.01f, origin, scale, spriteEffects, 0f);
            Main.EntitySpriteDraw(texture, position, texture.Frame(1, 4, 0, 3), Color.White * 0.5f * lerpTime, Projectile.rotation + direction * -0.05f, origin, scale * 0.8f, spriteEffects, 0f);
            Main.EntitySpriteDraw(texture, position, texture.Frame(1, 4, 0, 3), Color.White * 0.4f * lerpTime, Projectile.rotation + direction * -0.2f, origin, scale * 0.6f, spriteEffects, 0f);
            
            for (float i = 0f; i < 8f; i += 1f)
            {
                float edgeRotation = Projectile.rotation + direction * i * (MathHelper.Pi * -2f) * 0.025f + Utils.Remap(percentageOfLife, 0f, 1f, 0f, MathHelper.PiOver4) * direction;
                Vector2 drawPos = position + edgeRotation.ToRotationVector2() * (texture.Width * 0.5f - 6f) * scale;
                DrawPrettyStarSparkle(Projectile.Opacity, SpriteEffects.None, drawPos, new Color(255, 255, 255, 0) * lerpTime * (i / 9f), middleMediumColor, percentageOfLife, 0f, 0.5f, 0.5f, 1f, edgeRotation, new Vector2(0f, Utils.Remap(percentageOfLife, 0f, 1f, 3f, 0f)) * scale, Vector2.One * scale);
            }

            Vector2 drawPos2 = position + (Projectile.rotation + Utils.Remap(percentageOfLife, 0f, 1f, 0f, MathHelper.PiOver4) * direction).ToRotationVector2() * (texture.Width * 0.5f - 4f) * scale;
            DrawPrettyStarSparkle(Projectile.Opacity, SpriteEffects.None, drawPos2, new Color(255, 255, 255, 0) * lerpTime * 0.5f, middleMediumColor, percentageOfLife, 0f, 0.5f, 0.5f, 1f, 0f, new Vector2(2f, Utils.Remap(percentageOfLife, 0f, 1f, 4f, 1f)) * scale, Vector2.One * scale);

            return false;
        }

        private static void DrawPrettyStarSparkle(float opacity, SpriteEffects dir, Vector2 drawPos, Color drawColor, Color shineColor, float flareCounter, float fadeInStart, float fadeInEnd, float fadeOutStart, float fadeOutEnd, float rotation, Vector2 scale, Vector2 fatness)
        {
            Texture2D sparkleTexture = TextureAssets.Extra[98].Value;
            Color bigColor = shineColor * opacity * 0.5f;
            bigColor.A = 0;
            Vector2 origin = sparkleTexture.Size() / 2f;
            Color smallColor = drawColor * 0.5f;
            float lerpValue = Utils.GetLerpValue(fadeInStart, fadeInEnd, flareCounter, clamped: true) * Utils.GetLerpValue(fadeOutEnd, fadeOutStart, flareCounter, clamped: true);
            Vector2 scaleLeftRight = new Vector2(fatness.X * 0.5f, scale.X) * lerpValue;
            Vector2 scaleUpDown = new Vector2(fatness.Y * 0.5f, scale.Y) * lerpValue;
            bigColor *= lerpValue;
            smallColor *= lerpValue;
            Main.EntitySpriteDraw(sparkleTexture, drawPos, null, bigColor, MathHelper.PiOver2 + rotation, origin, scaleLeftRight, dir);
            Main.EntitySpriteDraw(sparkleTexture, drawPos, null, bigColor, 0f + rotation, origin, scaleUpDown, dir);
            Main.EntitySpriteDraw(sparkleTexture, drawPos, null, smallColor, MathHelper.PiOver2 + rotation, origin, scaleLeftRight * 0.6f, dir);
            Main.EntitySpriteDraw(sparkleTexture, drawPos, null, smallColor, 0f + rotation, origin, scaleUpDown * 0.6f, dir);
        }
    }
    public class FlyingExcaliburPlayer : ModPlayer
    {
        public int ExcaliburattackspeedTimer = 0;

        public override void ResetEffects()
        {
            ExcaliburattackspeedTimer = Math.Max(0, ExcaliburattackspeedTimer - 1);
        }

        public override void UpdateDead()
        {
            ExcaliburattackspeedTimer = 0;
        }
    }
    class BuffsFlyingExcalibur : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.GetModPlayer<FlyingExcaliburPlayer>().ExcaliburattackspeedTimer > 0)
            {
                player.whipRangeMultiplier += 0.10f; // 增加攻击距离
                player.GetDamage(DamageClass.Summon) += 0.1f; // 增加10%攻击力
            }
            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<FlyingExcaliburProj>()] > 0)//检测玩家持有的弹幕数量
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
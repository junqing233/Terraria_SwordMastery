using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;



namespace SwordMastery.Content.Items.FlyingSword.AGlobalControl
{
    public class MagicSachetProjectile_632 : ModProjectile
    {
        private const float MaxDamageMultiplier = 1.5f;
        private const float MaxBeamScale = 1.8f;
        private const float MaxBeamLength = 2400f;
        private const float BeamHitboxCollisionWidth = 22f;
        private const float BeamLengthChangeFactor = 0.75f;
        private const float VisualEffectThreshold = 0.1f;
        private const float OuterBeamOpacityMultiplier = 0.75f;
        private const float InnerBeamOpacityMultiplier = 0.1f;
        private const float BeamLightBrightness = 0.75f;

        // 颜色参数可自定义
        private const float BeamColorHue = 0.13f; // 金色
        private const float BeamColorSaturation = 0.85f;
        private const float BeamColorLightness = 0.65f;
        NPC target = null;
        private float BeamLength
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        // 自动充能进度（0~1）
        private float ChargeRatio
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        // 充能速度（每帧增加多少，1f/60f约1秒满）
        private const float ChargeSpeed = 1f / 60f;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            //Projectile.timeLeft = 360;
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
            Player player = Main.player[Projectile.owner];
            // 1. 跟随父弹幕
            int parentWhoAmI = (int)Projectile.ai[0];
            if (parentWhoAmI < 0 || parentWhoAmI >= Main.maxProjectiles ||
                !Main.projectile[parentWhoAmI].active ||
                Main.projectile[parentWhoAmI].type != ModContent.ProjectileType<MagicSachetProj>() ||
                player.statMana <= 2)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = Main.projectile[parentWhoAmI].Center;

            // 2. 平滑指向最近的敌人
            target = null;
            
            FlyingGunProj.ClosestNPC(ref target, 1200, player.Center, MagicSachet.IgnoreTilesForTargeting, player.MinionAttackTargetNPC, npc => npc.active);

            Vector2 desiredDir;
            if (target != null)
            {
                desiredDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            }
            else
            {
                desiredDir = Vector2.UnitY;
                Projectile.Kill();
            }

            // 平滑插值（0.15f为转向速度，可调）
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDir, 0.15f);

            // 自动充能
            ChargeRatio += ChargeSpeed;
            if (ChargeRatio > 1f)
                ChargeRatio = 1f;

            // 方向
            Vector2 beamDir = Projectile.velocity == Vector2.Zero ? -Vector2.UnitY : Vector2.Normalize(Projectile.velocity);

            // 伤害倍率随充能提升
            float damageMultiplier = MathHelper.Lerp(1f, MaxDamageMultiplier, ChargeRatio * ChargeRatio * ChargeRatio);
            Projectile.damage = (int)(Projectile.originalDamage * damageMultiplier);
            Projectile.friendly = true;

            // 缩放和透明度随充能变化
            Projectile.scale = MathHelper.Lerp(0.5f, 1.2f, ChargeRatio);
            Projectile.Opacity = MathHelper.Lerp(0.1f, 1f, ChargeRatio);

            // 位置和朝向
            Projectile.rotation = beamDir.ToRotation();

            // 束长度
            float hitscanBeamLength = 600;
            BeamLength = MathHelper.Lerp(BeamLength, hitscanBeamLength, BeamLengthChangeFactor);

            Vector2 beamDims = new Vector2(Projectile.velocity.Length() * BeamLength, Projectile.width * Projectile.scale);

            Color beamColor = GetOuterBeamColor();
            if (ChargeRatio >= VisualEffectThreshold)
            {
                ProduceBeamDust(beamColor);
                if (Main.netMode != NetmodeID.Server)
                    ProduceWaterRipples(beamDims);
            }

            DelegateMethods.v3_1 = beamColor.ToVector3() * BeamLightBrightness * ChargeRatio;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, beamDims.Y, new Utils.TileActionAttempt(DelegateMethods.CastLight));
        }
        private static readonly Color[] BeamColors = new Color[]
        {
            new Color(180, 255, 180), // 浅绿
            new Color(180, 220, 255), // 浅蓝
            new Color(255, 255, 180), // 浅黄
            new Color(60, 100, 220),  // 深蓝
            new Color(180, 120, 255), // 紫
        };
        private Color GetOuterBeamColor()
        {
            // 每60帧为一个渐变周期（1秒）
            const int framesPerColor = 60;
            float t = (Main.GameUpdateCount % (framesPerColor * BeamColors.Length)) / (float)framesPerColor;
            int idx = (int)t;
            int nextIdx = (idx + 1) % BeamColors.Length;
            float lerp = t - idx;

            Color c = Color.Lerp(BeamColors[idx], BeamColors[nextIdx], lerp);
            c.A = 64;
            return c;
        }

        private Color GetInnerBeamColor() => Color.White;

        private void ProduceBeamDust(Color beamColor)
        {
            // 框尺寸
            if (target == null)
                return;
            float boxSize = target.width / 2;
            // 在弹幕中心附近的boxSize*boxSize区域内随机生成
            Vector2 spawnPos = target.Center + new Vector2(
                Main.rand.NextFloat(-boxSize / 2, boxSize / 2),
                Main.rand.NextFloat(-boxSize / 2, boxSize / 2)
            );

            // 随机扩散方向
            float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
            float speed = Main.rand.NextFloat(1.2f, 3.2f);
            Vector2 velocity = angle.ToRotationVector2() * speed;

            float scale = Main.rand.NextFloat(0.7f, 1.2f);

            Dust dust = Dust.NewDustDirect(spawnPos, 0, 0, DustID.WhiteTorch, velocity.X, velocity.Y, 0, beamColor, scale);
            dust.color = beamColor;
            dust.noGravity = true;
            dust.velocity *= Projectile.scale * 8f;
            dust.scale *= Projectile.scale * 1.6f;
        }

        private void ProduceWaterRipples(Vector2 beamDims)
        {
            WaterShaderData shaderData = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
            float waveSine = 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
            Vector2 ripplePos = Projectile.position + new Vector2(beamDims.X * 0.5f, 0f).RotatedBy(Projectile.rotation);
            Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
            shaderData.QueueRipple(ripplePos, waveData, beamDims, RippleShape.Square, Projectile.rotation);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;
            float _ = float.NaN;
            Vector2 beamEndPos = Projectile.Center + Projectile.velocity * BeamLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, beamEndPos, BeamHitboxCollisionWidth * Projectile.scale, ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 centerFloored = Projectile.Center.Floor() + Projectile.velocity * Projectile.scale * 10.5f;
            Vector2 drawScale = new Vector2(Projectile.scale);
            float visualBeamLength = BeamLength - 14.5f * Projectile.scale * Projectile.scale;
            DelegateMethods.f_1 = 1f;

            Vector2 startPosition = centerFloored - Main.screenPosition;
            Vector2 endPosition = startPosition + Projectile.velocity * visualBeamLength;

            DrawBeam(Main.spriteBatch, texture, startPosition, endPosition, drawScale, GetOuterBeamColor() * OuterBeamOpacityMultiplier * Projectile.Opacity);
            drawScale *= 0.5f;
            DrawBeam(Main.spriteBatch, texture, startPosition, endPosition, drawScale, GetInnerBeamColor() * InnerBeamOpacityMultiplier * Projectile.Opacity);

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
            Vector2 beamStartPos = Projectile.Center;
            Vector2 beamEndPos = beamStartPos + Projectile.velocity * BeamLength;
            Utils.PlotTileLine(beamStartPos, beamEndPos, Projectile.width * Projectile.scale, cut);
        }
    }
    public class MagicSachetProjectile_535 : ModProjectile
    {
        private bool isOnhitTarget = false;
        private Vector2 velocityCache = Vector2.Zero;
        private int TimeLeft = 0;
        
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Summon; // 伤害类型
            Projectile.friendly = true;
            Projectile.light = 0.1f;
            Projectile.timeLeft = 360;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;

            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
        }
        public override void OnSpawn(IEntitySource source)
        {
            //随机方向
            Projectile.velocity = new Vector2(6, 6).RotatedByRandom(MathHelper.ToRadians(360));
            velocityCache = Projectile.velocity;
        }
        public override void AI()
        {
            // 获取玩家
            var player = Main.player[Projectile.owner];
            //追踪敌人
            NPC target = null;
            FlyingGunProj.ClosestNPC(ref target, 1200, player.Center, MagicSachet.IgnoreTilesForTargeting, player.MinionAttackTargetNPC, npc => npc.active);
            if (target != null && !isOnhitTarget)
            {
                Vector2 projectileToTarget = Vector2.Normalize(target.Center - Projectile.Center) * 12f;
                Projectile.velocity = projectileToTarget;
            }
            if (isOnhitTarget)
            {
                TimeLeft++;
                if (TimeLeft < 30)
                    Projectile.velocity = velocityCache * 0.5f;
                else if (target != null)
                {
                    Vector2 projectileToTarget = Vector2.Normalize(target.Center - Projectile.Center) * 12f;
                    Projectile.velocity = projectileToTarget;
                }
            }
        }

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            //粒子特效
            for (int i = 0; i < 4; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.Center, Projectile.width * 4, Projectile.height * 4,
                    DustID.GoldFlame, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].scale = 1.5f;
                Main.dust[dustIndex].color = Color.Yellow;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            isOnhitTarget = true;
        }
       
        public override void PostDraw(Color lightColor)
        {
            List<CustomVertexInfo> bars = new List<CustomVertexInfo>();

            // 把所有的点都生成出来，按照顺序
            for (int i = 1; i < Projectile.oldPos.Length; ++i)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) break;

                int width = 10;
                var normalDir = Projectile.oldPos[i - 1] - Projectile.oldPos[i];
                normalDir = Vector2.Normalize(new Vector2(-normalDir.Y, normalDir.X));

                var factor = i / (float)Projectile.oldPos.Length;
                var color = Color.Lerp(Color.White, Color.Gold, factor);
                var w = MathHelper.Lerp(1f, 0.05f, factor);

                bars.Add(new CustomVertexInfo(Projectile.oldPos[i] + normalDir * width, color, new Vector3((float)Math.Sqrt(factor), 1, w)));
                bars.Add(new CustomVertexInfo(Projectile.oldPos[i] + normalDir * -width, color, new Vector3((float)Math.Sqrt(factor), 0, w)));
            }

            List<CustomVertexInfo> triangleList = new List<CustomVertexInfo>();

            if (bars.Count > 2)
            {
                // 按照顺序连接三角形
                triangleList.Add(bars[0]);
                var vertex = new CustomVertexInfo((bars[0].Position + bars[1].Position) * 0.5f + Vector2.Normalize(Projectile.velocity) * 30, Color.White,
                    new Vector3(0, 0.5f, 1));
                triangleList.Add(bars[1]);
                triangleList.Add(vertex);
                for (int i = 0; i < bars.Count - 2; i += 2)
                {
                    triangleList.Add(bars[i]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 1]);

                    triangleList.Add(bars[i + 1]);
                    triangleList.Add(bars[i + 2]);
                    triangleList.Add(bars[i + 3]);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone);
                RasterizerState originalState = Main.graphics.GraphicsDevice.RasterizerState;

                var projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, 0, 1);
                var model = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0));

                // 获取着色器
                Effect trailEffect = ModContent.Request<Effect>("SwordMastery/Effects/MagicSachetProjectile").Value;

                if (trailEffect == null)
                {
                    return;
                }

                // 设置着色器参数
                trailEffect.Parameters["uTransform"].SetValue(model * projection);
                trailEffect.Parameters["uTime"].SetValue(-(float)Main.time * 0.03f);

                // 设置纹理
                Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>("SwordMastery/Textures/heatmap_MagicSachet").Value;
                Main.graphics.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("SwordMastery/Textures/Extra_1_1").Value;
                Main.graphics.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("SwordMastery/Textures/Extra_2_1").Value;
                Main.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                Main.graphics.GraphicsDevice.SamplerStates[1] = SamplerState.PointWrap;
                Main.graphics.GraphicsDevice.SamplerStates[2] = SamplerState.PointWrap;

                trailEffect.CurrentTechnique.Passes[0].Apply();

                Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangleList.ToArray(), 0, triangleList.Count / 3);

                Main.graphics.GraphicsDevice.RasterizerState = originalState;
                Main.spriteBatch.End();
                Main.spriteBatch.Begin();
            }
        }

        // 自定义顶点数据结构，注意这个结构体里面的顺序需要和shader里面的数据相同
        private struct CustomVertexInfo : IVertexType
        {
            private static VertexDeclaration _vertexDeclaration = new VertexDeclaration(new VertexElement[3]
            {
                    new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                    new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                    new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            });
            public Vector2 Position;
            public Color Color;
            public Vector3 TexCoord;

            public CustomVertexInfo(Vector2 position, Color color, Vector3 texCoord)
            {
                Position = position;
                Color = color;
                TexCoord = texCoord;
            }

            public VertexDeclaration VertexDeclaration
            {
                get
                {
                    return _vertexDeclaration;
                }
            }
        }
    }
}

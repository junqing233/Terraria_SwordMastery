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
    public class TigerVigor : ModItem
    {
        public static bool isDash = false;
        public static bool isStrengthen = false;
        public static int isStrengthenTimer = 0; // 新增：计时器
        public static bool isLife = false;
        public int attackType = 0;
        public int comboExpireTimer = 0;
        private int Counter = 0;

        public override void SetDefaults()
        {
            Item.damage = 84;
            Item.crit = 24;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.width = 70;
            Item.height = 80;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 6;
            Item.value = Item.buyPrice(0, 0, 1, 4);
            Item.rare = ItemRarityID.Purple;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<TigerVigorProj>();
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "TigerVigor", "神兵") { OverrideColor = new Color(255, 102, 0) });
            if (Language.ActiveCulture.Name == "zh-Hans")
            {
                tooltips.Add(new TooltipLine(Mod, "", "左键进行三段" + "[c/fb9902:虎魄斩]"));
                tooltips.Add(new TooltipLine(Mod, "", "右键进行" + "[c/f8d568:虎魄落风斩]"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/f8d568:虎魄落风斩]" + "会" + "[c/996600:强化]" + "刀身"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/996600:强化]" + "使" + "[c/f8d568:虎魄落风斩]" + "附带真伤"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/996600:强化]" + "使" + "[c/fb9902:虎魄斩]" + "附带五倍真伤"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/f8d568:虎魄落风斩]" + "期间无视死亡"));
                tooltips.Add(new TooltipLine(Mod, "", "在召唤了沙漠虎的状态下通过虚无魔镜用虎皮复制"));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "", "Left click: Triple-stage [c/fb9902:Tiger Soul Slash]"));
                tooltips.Add(new TooltipLine(Mod, "", "Right click: [c/f8d568:Tiger Soul Gale Slash]"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/f8d568:Tiger Soul Gale Slash] will [c/996600:Empower] the blade"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/996600:Empower] makes [c/f8d568:Tiger Soul Gale Slash] deal true damage"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/996600:Empower] makes [c/fb9902:Tiger Soul Slash] deal 5x true damage"));
                tooltips.Add(new TooltipLine(Mod, "", "[c/f8d568:Tiger Soul Gale Slash] grants death immunity while active"));
                tooltips.Add(new TooltipLine(Mod, "", "Use Tiger Skin Copy with the Void Mirror while the Desert Tiger is summoned"));
            }
        }
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }
        public override bool CanUseItem(Player player)
        {
            if(Counter < 2)
                Counter++;
            else
                Counter = 0;
            if (Counter == 2)
            {
                Vector2 direction = Vector2.Normalize(player.DirectionTo(Main.MouseWorld));
                FireProjectile(player, player.Center, direction, ModContent.ProjectileType<TigerVigorProj>(), Item.damage, Item.knockBack);
            }
            return base.CanUseItem(player);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.mouseRight)
            {
                FireProjectile(player, position, velocity, ModContent.ProjectileType<TigerVigorDashProj>(), (int)(Item.damage * 0.8f), knockback);
                isDash = true;
                isLife = true;
                return false;
            }
            else
            {
                FireProjectile(player, position, velocity, ModContent.ProjectileType<TigerVigorProj>(), damage, knockback);
                return false;
            }
        }

        private void FireProjectile(Player player, Vector2 position, Vector2 velocity, int projectileType, int damage, float knockback)
        {
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), position, velocity, projectileType, damage, knockback, player.whoAmI, attackType);
            attackType = (attackType + 1) % 2;
            comboExpireTimer = 0;
        }
        public override void HoldItem(Player player)
        {
            if (comboExpireTimer < 60)
            {
                if(Main.mouseLeft && Main.mouseRightRelease)
                    isLife = false;
                else
                    isLife = true;
            }
            else
                isLife = false;
           
            //Main.NewText(isLife);
            base.HoldItem(player);
        }
        public override void UpdateInventory(Player player)
        {
            comboExpireTimer = Math.Min(comboExpireTimer + 1, 120);
            if (comboExpireTimer >= 120)
            {
                attackType = 0;
                Counter = -1;
            }

            // ---------强化计时逻辑---------
            if (isStrengthen)
            {
                if (isStrengthenTimer > 0)
                {
                    isStrengthenTimer--;
                    if (isStrengthenTimer == 0)
                    {
                        isStrengthen = false;
                    }
                }
            }
        }
    }
    public class TigerVigorDashProj : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TigerVigor";
        
        private float dashDistanceRemaining = 300;
        Vector2 dashDir;
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.timeLeft = 60;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.damage = 0;
            Player player = Main.player[Projectile.owner];
            dashDir = Vector2.Normalize(Main.MouseWorld - player.Center);
            
            // 生成挥舞弹幕
            if (Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(player.HeldItem),
                    player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<TigerVigorProj>(),
                    player.HeldItem.damage,
                    player.HeldItem.knockBack,
                    player.whoAmI
                );
            }
            base.OnSpawn(source);
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            float dashStep = 10f; // 每帧移动的距离
            if (dashDistanceRemaining > 0f)
            {
                // 计算本帧移动的实际距离
                float moveDistance = Math.Min(dashStep, dashDistanceRemaining);
                player.position += dashDir * moveDistance; // 更新玩家位置
                dashDistanceRemaining -= moveDistance; // 减少剩余冲刺距离
            }
            if (Projectile.timeLeft > 30)
            {
                player.fullRotationOrigin = new Vector2(10, 15);
                if (player.direction == 1)
                    player.fullRotation += 0.35f; // 玩家旋转
                else
                    player.fullRotation -= 0.35f; // 玩家旋转
            }else
            {
                player.fullRotation = 0f; // 玩家旋转为 0 度
            }
            //player.immune = true;// 玩家无敌
            //player.immuneTime = 2; // 确保无敌时间短于冲刺持续时间

        }

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.fullRotation = 0f; // 玩家旋转为 0 度
            TigerVigor.isStrengthen = true;
            TigerVigor.isStrengthenTimer = 360; // 6秒（60帧*6）
            //TigerVigor.isLife = false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
    public class TigerVigorPlayer : ModPlayer
    {
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {

            if (TigerVigor.isLife)  
                return false;

            return true;
        }
        public override bool CanBeHitByProjectile(Projectile proj)
        {

            if (TigerVigor.isLife)  
                return false;

            return true;
        }
        public override bool CanUseItem(Item item)
        {
            //if (TigerVigor.isLife)
            //    return true;

            return base.CanUseItem(item);
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if(TigerVigor.isLife)
            {
                Player.statLife = Player.statLifeMax2;
                Player.HealEffect(Player.statLifeMax2);

                return false; // 阻止死亡
            }
            
            return true;
        }
    }
    public class TigerVigorProj : ModProjectile
    {
        private float SWINGRANGE = 1.67f * (float)Math.PI;
        private float FIRSTHALFSWING = 0.45f;
        private float SPINRANGE = 1.67f * (float)Math.PI;
        private float UNWIND = 0.4f;
        private float SPINTIME = 1f;
        
        private enum AttackType
        {
            Swing,
            Spin,
        }

        private enum AttackStage
        {
            Prepare,
            Execute,
            Unwind
        }

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
                Timer = 0;
            }
        }

        private ref float InitialAngle => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.ai[2];
        private ref float Progress => ref Projectile.localAI[1];
        private ref float Size => ref Projectile.localAI[2];

        private float prepTime => 4f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float execTime => 8f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float hideTime => 4f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        private float execTime_ => 16f / Owner.GetTotalAttackSpeed(Projectile.DamageType);
        public override string Texture => "SwordMastery/Content/Items/Weapons/Sword/TigerVigor";
        private Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.timeLeft = 10000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
        }

        public override void OnSpawn(IEntitySource source)
        {
            //Main.NewText(TigerVigor.isDash);
            Projectile.spriteDirection = Main.MouseWorld.X > Owner.MountedCenter.X ? 1 : -1;
            float targetAngle = (Main.MouseWorld - Owner.Center).ToRotation();

            if (CurrentAttack == AttackType.Spin)
            {
                InitialAngle = targetAngle - FIRSTHALFSWING * SWINGRANGE * Projectile.spriteDirection * 1.6f;
            }
            else
            {
                if (Projectile.spriteDirection == 1)
                {
                    targetAngle = MathHelper.Clamp(targetAngle, (float)-Math.PI * 1 / 3, (float)Math.PI * 1 / 6);
                }
                else
                {
                    if (targetAngle < 0)
                    {
                        targetAngle += 2 * (float)Math.PI;
                    }

                    targetAngle = MathHelper.Clamp(targetAngle, (float)Math.PI * 5 / 6, (float)Math.PI * 4 / 3);
                }

                InitialAngle = targetAngle - FIRSTHALFSWING * SWINGRANGE * Projectile.spriteDirection * 1.2f;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((sbyte)Projectile.spriteDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.spriteDirection = reader.ReadSByte();
        }

        public override void AI()
        {
            if (TigerVigor.isDash)
            {
                SWINGRANGE = (float)Math.PI * 5.7f;
                SPINRANGE = (float)Math.PI * 5.7f;
                Projectile.localNPCHitCooldown = 10;
            }
            
            Projectile.oldPos[0] = Projectile.position;
            Projectile.oldRot[0] = Projectile.rotation;

            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                Projectile.oldPos[i] = Projectile.oldPos[i - 1];
                Projectile.oldRot[i] = Projectile.oldRot[i - 1];
            }

            Owner.itemAnimation = 2;
            Owner.itemTime = 2;
            //if (isDash) // 新增模式
            //{
            //    Projectile.rotation = Owner.fullRotation;
            //    Projectile.Center = Owner.Center;
            //    // 可选：挥舞特效、伤害判定等

            //}
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

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            TigerVigor.isDash = false;
            base.Kill(timeLeft);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
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
            if(TigerVigor.isStrengthen)
            {
                float a = TigerVigor.isDash ? 0.01f : 0.05f;
                NpcUtils.DealTruePercentDamage(target, a, hit.HitDirection);
            }
            
        }
        public struct CustomVertex : IVertexType
        {
            public Vector3 Position;
            public Color Color;

            public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            );

            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

            public CustomVertex(Vector3 position, Color color)
            {
                Position = position;
                Color = color;
            }
        }

        public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor)
        {
            Vector2 origin;
            float rotationOffset;
            SpriteEffects effects;

            // 统一设置初始origin/rotationOffset/effects
            if (Projectile.spriteDirection > 0)
            {
                origin = new Vector2(0, Projectile.height);
                rotationOffset = MathHelper.ToRadians(45f);
                effects = SpriteEffects.None;
            }
            else
            {
                origin = new Vector2(Projectile.width, Projectile.height);
                rotationOffset = MathHelper.ToRadians(135f);
                effects = SpriteEffects.FlipHorizontally;
            }

            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            List<Vertex> ve = new();

            Color color = Color.White;
            bool isSwing = CurrentAttack == AttackType.Swing && CurrentStage != AttackStage.Prepare && Timer > 2;
            bool isSpin = CurrentAttack == AttackType.Spin && CurrentStage != AttackStage.Prepare && Timer > 2;

            void AddTrailVertex(Vector2 offsetA, Vector2 offsetB, float rotSign, float rotMul, bool negate)
            {
                for (int i = 0; i < 12; i++)
                {
                    float rot = Projectile.oldRot[i] + rotSign * rotationOffset * rotMul;
                    if (negate) rot = Projectile.oldRot[i] - rotSign * rotationOffset * rotMul;
                    ve.Add(new Vertex(Projectile.Center - Main.screenPosition + offsetA.RotatedBy(rot), new Vector3(i / 12f, 1, 1), color));
                    ve.Add(new Vertex(Projectile.Center - Main.screenPosition + offsetB.RotatedBy(rot), new Vector3(i / 12f, 0, 1), color));
                }
            }

            if (isSwing)
            {
                int longLen = TigerVigor.isStrengthen ? -245 : -125;
                int shortLen = TigerVigor.isStrengthen ? -90 : -50;
                int shortLen2 = TigerVigor.isStrengthen ? -90 : -40;

                if (Projectile.spriteDirection > 0)
                    AddTrailVertex(new Vector2(0, longLen), new Vector2(0, shortLen), 1, 2, false);
                else
                    AddTrailVertex(new Vector2(0, shortLen2), new Vector2(0, longLen), 1, 2, true);
            }
            else if (isSpin)
            {
                int longLen = TigerVigor.isStrengthen ? -245 : -125;
                int shortLen = TigerVigor.isStrengthen ? -90 : -40;

                if (Projectile.spriteDirection > 0)
                    AddTrailVertex(-new Vector2(0, shortLen), -new Vector2(0, longLen), 1, 2, true);
                else
                    AddTrailVertex(-new Vector2(0, longLen), -new Vector2(0, shortLen), 1, 2, false);
            }

            if (ve.Count >= 3)
            {
                gd.Textures[0] = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Sword/SwordTrail_0").Value;
                gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, ve.ToArray(), 0, ve.Count - 2);
            }

            // Spin时再调整origin/rotationOffset/effects，保证主贴图方向正确
            if (CurrentAttack == AttackType.Spin)
            {
                if (Projectile.spriteDirection > 0)
                {
                    origin = new Vector2(Projectile.width, Projectile.height);
                    rotationOffset = MathHelper.ToRadians(135f);
                    effects = SpriteEffects.FlipHorizontally;
                }
                else
                {
                    origin = new Vector2(0, Projectile.height);
                    rotationOffset = MathHelper.ToRadians(45f);
                    effects = SpriteEffects.None;
                }
            }
            if (TigerVigor.isStrengthen)
            {
                Color shadowColor = new Color(255, 230, 120, 80) * 0.4f;
                Main.spriteBatch.Draw(
                    TextureAssets.Projectile[Type].Value,
                    Projectile.Center - Main.screenPosition,
                    null,
                    shadowColor,
                    Projectile.rotation + rotationOffset,
                    origin,
                    Projectile.scale * 2f, // 两倍长度
                    effects,
                    0);
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

        public override bool? Colliding(Microsoft.Xna.Framework.Rectangle projHitbox, Microsoft.Xna.Framework.Rectangle targetHitbox)
        {
            Vector2 start = Owner.MountedCenter;
            float scale = Projectile.scale * (TigerVigor.isStrengthen ? 2f : 1f);
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * ((Projectile.Size.Length()) * scale * 1.02f);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 15f * scale, ref collisionPoint);
        }

        public override void CutTiles()
        {
            Vector2 start = Owner.MountedCenter;
            float scale = Projectile.scale * (TigerVigor.isStrengthen ? 2f : 1f);
            Vector2 end = start + Projectile.rotation.ToRotationVector2() * (Projectile.Size.Length() * scale * 1.06f);
            Utils.PlotTileLine(start, end, 30 * scale, DelegateMethods.CutTiles);
        }

        public override bool? CanDamage()
        {
            if (CurrentStage == AttackStage.Prepare)
                return false;
            return base.CanDamage();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.position.X > Owner.MountedCenter.X ? 1 : -1;

        }

        public void SetSwordPosition()
        {
            Projectile.rotation = InitialAngle + Projectile.spriteDirection * Progress;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(90f));
            Vector2 armPosition = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, Projectile.rotation - (float)Math.PI / 2);
            armPosition.Y += Owner.gfxOffY;

            Projectile.Center = armPosition;
            Projectile.scale = Size * 1.2f * Owner.GetAdjustedItemScale(Owner.HeldItem);

            Owner.heldProj = Projectile.whoAmI;
        }

        private void PrepareStrike()
        {
            Size = 1f;
            if (Timer >= prepTime)
            {
                SoundEngine.PlaySound(SoundID.Item1);
                CurrentStage = AttackStage.Execute;
            }
        }

        private void ExecuteStrike()
        {
            if (CurrentAttack == AttackType.Swing)
            {
                Progress = MathHelper.SmoothStep(0, SWINGRANGE, (1f - UNWIND / 2) * Timer / ((TigerVigor.isDash ? execTime_ : execTime) * 2));

                if (Timer >= (TigerVigor.isDash ? execTime_ : execTime) * 3)
                {
                    CurrentStage = AttackStage.Unwind;
                    //Main.NewText(TigerVigorDashProj.isDash);
                }
            }
            else
            {
                Progress = MathHelper.SmoothStep(0, -SPINRANGE, (1f - UNWIND / 2) * Timer / ((TigerVigor.isDash ? execTime_ : execTime) * SPINTIME * 2));

                if (Timer >= (TigerVigor.isDash ? execTime_ : execTime) * SPINTIME * 3)
                {
                    CurrentStage = AttackStage.Unwind;
                }
            }
        }

        private void UnwindStrike()
        {
            if (CurrentAttack == AttackType.Swing)
            {
                Progress = MathHelper.SmoothStep(0, SWINGRANGE, (1f - UNWIND / 10) + UNWIND * Timer / (hideTime));
                if (Timer >= hideTime)
                {
                    Projectile.Kill();
                }
            }
            else
            {
                Progress = MathHelper.SmoothStep(0, -SPINRANGE, (1f - UNWIND / 10) + UNWIND * Timer / (hideTime * SPINTIME));
                if (Timer >= hideTime * SPINTIME)
                {
                    Projectile.Kill();
                }
            }
        }
    }
}
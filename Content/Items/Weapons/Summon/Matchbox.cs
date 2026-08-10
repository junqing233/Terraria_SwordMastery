using Microsoft.Build.Evaluation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Weapons.Summon
{
    public class DryadShop : GlobalNPC
    {
        public override void ModifyShop(NPCShop shop)
        {
            // 检查是否是爆破专家的商店
            if (shop.NpcType == 38)
            {
                // 添加 PeaShooterSummoningStaff 到商店
                shop.Add(ModContent.ItemType<Matchbox>(), Condition.Hardmode);
            }
        }
    }
    public class Matchbox : ModItem
    {
        private readonly Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Summon/Matchbox").Value;
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;//这让这个物品在研究时只需要1个
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true; //这让控制器玩家可以在全屏范围内选择目标
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;//这让锁定目标时不会发生碰撞
        }
        public override void SetDefaults()
        {
            Item.damage = 52; // 基础伤害
            Item.crit = 14; // 暴击率
            Item.DamageType = DamageClass.Summon; // 伤害类型
            Item.width = 39; // 宽度
            Item.height = 31; // 高度
            Item.useTime = 20; // 使用时间
            Item.useAnimation = 20; // 使用动画
            Item.useStyle = ItemUseStyleID.Swing; // 使用方式
            Item.knockBack = 6; // 击退距离
            Item.value = Item.buyPrice(gold: 50); // 价值
            Item.rare = ItemRarityID.Red; // 稀有度
            Item.UseSound = SoundID.Item43; // 使用音效
            Item.autoReuse = true; // 自动重用
            //Item.useTurn = true; // 是否可转向
            Item.noUseGraphic = false; // 确保武器图形显示
            Item.mana = 10; // 使用时消耗的魔力值
            Item.noMelee = true; // 无法近战
            Item.shoot = ModContent.ProjectileType<SpringFestivalElf>(); // 射击类型
            Item.shootSpeed = 1f; // 射击速度
            Item.buffType = ModContent.BuffType<BuffsSpringFestivalElf>(); // 召唤物品的buff类型
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Language.ActiveCulture.Name == "zh-Hans")
            {
                var openTooltip = (new TooltipLine(Mod, "", "[c/fd0f0f:召唤新春精灵来为你战斗]"));
                tooltips.Add(openTooltip);
            }
            else
            {
                var openTooltip = (new TooltipLine(Mod, "", "[c/fd0f0f:Summon the Spring Festival spirit to fight for you]"));
                tooltips.Add(openTooltip);
            }
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            spriteBatch.Draw(texture, position, sourceRectangle, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, texture.Height / 2);
            Vector2 origin = new Vector2(texture.Width / 2, texture.Height / 2);
            spriteBatch.Draw(texture, drawPosition, sourceRectangle, lightColor, rotation, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsSpringFestivalElf>(), 3600);
            player.SpawnMinionOnCursor(source, player.whoAmI, type, Item.damage, knockback);
            //返回false阻止原版发射
            return false;
        }
    }

    public class SpringFestivalElf : ModProjectile
    {
        // 定义一个新的变量以跟踪发射的弹幕数量
        private int shotsFired = 0;
        Player player => Main.player[Projectile.owner];

        private Vector2 standbyTargetOffset;
        private int standbyTargetTimer;

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
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            Projectile.width = 29; // 弹幕宽度
            Projectile.height = 27; // 弹幕高度
            Projectile.friendly = true; // 友方弹幕
            Projectile.tileCollide = false; // 不与瓷砖碰撞
            Projectile.DamageType = DamageClass.Summon; // 伤害类型改为召唤伤害
            Projectile.penetrate = -1; // 无限穿透
            Projectile.ignoreWater = true; // 无视液体
            Projectile.timeLeft = 120; // 存在时间无限
            Projectile.alpha = 100; // 透明度
            Projectile.light = 0.75f; // 发光亮度
            Projectile.minion = true; // 设置为召唤物
            Projectile.minionSlots = 1f; // 占用一个召唤栏位
            Projectile.aiStyle = -1;//不使用原版AI
            base.SetDefaults();
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<SpringFestivalElf_>(), // 生成自定义弹幕
                0,
                0f,
                Projectile.owner,
                Projectile.whoAmI // 通过ai[0]传递父弹幕索引
            );
            // 随机初始目标点
            RandomizeStandbyTarget();
            standbyTargetTimer = Main.rand.Next(40, 100);
            Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<SpringFestivalElf_>(),
                0,
                0f,
                Projectile.owner,
                Projectile.whoAmI
            );
        }
        private void RandomizeStandbyTarget()
        {
            float radius = 100f;
            float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
            standbyTargetOffset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
        }
        void MoveToTarget(Vector2 targetPos, float MaxSpeed = 20f, float accSpeed = 0.5f)//运用之前学到的惯性追击
        {
            //原理：比较目标和自己的横向或者纵向坐标差，然后给自己的速度加上向着差值变小前进的加速度
            //如果自己的速度坐标差一样，说明自己正在原理目标，需要更大的加速度，这里我设定的是2倍
            if (Projectile.Center.X - targetPos.X < 0f)
                Projectile.velocity.X += Projectile.velocity.X < 0 ? 2 * accSpeed : accSpeed;
            else
                Projectile.velocity.X -= Projectile.velocity.X > 0 ? 2 * accSpeed : accSpeed;

            if (Projectile.Center.Y - targetPos.Y < 0f)
                Projectile.velocity.Y += Projectile.velocity.Y < 0 ? 2 * accSpeed : accSpeed;
            else
                Projectile.velocity.Y -= Projectile.velocity.Y > 0 ? 2 * accSpeed : accSpeed;
            if (Math.Abs(Projectile.velocity.X) > MaxSpeed)//如果横向速度超越最大值，则回到最大值
                Projectile.velocity.X = MaxSpeed * Math.Sign(Projectile.velocity.X);
            if (Math.Abs(Projectile.velocity.Y) > MaxSpeed)//如果纵向速度超越最大值，则回到最大值
                Projectile.velocity.Y = MaxSpeed * Math.Sign(Projectile.velocity.Y);

        }

        public override bool? CanCutTiles()
        {
            return false;//我们不想召唤兽会割草
        }

        void MovingParticles()
        {
            if (Projectile.velocity.Length() > 24f)
            {
                int dustType = DustID.Torch; // 粒子类型
                Vector2 pos = Projectile.position + Projectile.Size / 2; // 粒子位置
                int num = Main.rand.Next(1, 2); // 粒子数量
                for (int j = 0; j < num; j++)
                {
                    int d = Dust.NewDust(pos, 0, 0, dustType, 0, 0); // 产生粒子
                    Main.dust[d].fadeIn = 1.12f;
                }
            }
        }

        // 改写攻击逻辑
        void AttackShooting(NPC target)
        {
            //旋转弹幕
            //Projectile.rotation += rotationSpeed;
            Projectile.ai[0]++; // 增加计时器
            if (Projectile.ai[0] >= 30) //攻击一次
            {
                Projectile.ai[0] = 0; // 重置计时器
                Vector2 shootDirection = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * 16f;
                // 生成一个子弹
                Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                Projectile.Center,
                shootDirection,
                ModContent.ProjectileType<SpringFestivalElf_Shoot>(), // 生成自定义弹幕
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                target.whoAmI // 设置目标
                );
                shotsFired++; // 增加发射弹幕数量
                if (shotsFired >= 6) // 限制发射弹幕数量
                {
                    StartCharge(target); // 开始瞬移
                    shotsFired = 0; // 重置发射弹幕数量
                }

            }
        }

        private void StartCharge(NPC target)
        {
            // 计算随机半径在100到200之间
            float radius = Main.rand.Next(100, 201);

            // 获取敌人的中心位置
            Vector2 targetPosition = target.Center;

            // 计算旋转角度，使用GameUpdateCount使其随时间而变化
            float rotationSpeed; // 旋转速度
            rotationSpeed = Main.rand.NextFloat(0.01f, 0.08f);
            float angle = Main.GameUpdateCount * rotationSpeed;

            // 计算弹幕的新位置
            Vector2 newPosition = targetPosition + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;

            // 更新弹幕的位置
            Projectile.Center = newPosition;

            //粒子效果
            for (int i = 0; i < 5; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<SpringFestivalElfDust>(), 0f, 0f, 100, default, 3f);
                dust.noGravity = true;
                dust.velocity *= 1.2f;
                dust.fadeIn = 1.2f;
            }
        }
        NPC FindBestTarget(Player player, Projectile proj, float maxLockDist = 2000f, float searchRange = 1500f)
        {
            // 1. 鼠标锁定目标
            if (player.HasMinionAttackTargetNPC)
            {
                NPC lockTarget = Main.npc[player.MinionAttackTargetNPC];
                if (lockTarget.active && !lockTarget.friendly && !lockTarget.dontTakeDamage && Vector2.Distance(player.Center, lockTarget.Center) <= maxLockDist)
                    return lockTarget;
            }

            // 2. 距离玩家最近的可见敌人
            NPC best = null;
            float minDist = searchRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && !npc.dontTakeDamage && !npc.townNPC && npc.lifeMax > 5 && npc.CanBeChasedBy(Projectile))
                {
                    float distToPlayer = Vector2.Distance(player.Center, npc.Center);
                    if (distToPlayer < minDist &&
                        Collision.CanHitLine(proj.position, proj.width, proj.height, npc.position, npc.width, npc.height))
                    {
                        minDist = distToPlayer;
                        best = npc;
                    }
                }
            }
            if (best != null)
                return best;

            // 3. 退而求其次，最近可见敌人（用原有方法）
            int t = proj.FindTargetWithLineOfSight((int)searchRange);
            if (t >= 0)
                return Main.npc[t];

            return null;
        }
        public override void AI()
        {
            // 获取当前属于该玩家的召唤物序号
            int index = 0;
            for (int i = 0; i < Projectile.whoAmI; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == Projectile.owner && proj.type == Projectile.type)
                {
                    index++;
                }
            }
            // 每隔一段时间随机一个新目标点
            if (--standbyTargetTimer <= 0 && player.velocity.Length() > 0)
            {
                RandomizeStandbyTarget();
                standbyTargetTimer = Main.rand.Next(90, 360); // 约0.7~1.7秒
            }
            Vector2 mypos = player.Center + standbyTargetOffset;

            if (player.HasBuff<BuffsSpringFestivalElf>()) // 如果玩家有召唤物BUFF
                Projectile.timeLeft = 2; // 维持住弹幕的时间

            NPC target = FindBestTarget(player, Projectile);
            // 旋转与速度挂钩，平滑
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.velocity.X * 0.07f, 0.2f);
            MovingParticles();
            if (target != null && target.active) // 如果目标不为空且存活在此处执行攻击性AI
            {
                if (target.active)
                {
                    if (Vector2.Distance(player.Center, target.Center) > 2000)//如果找到的目标距离玩家太远了
                    {
                        Vector2 p = Vector2.Lerp(Projectile.Center, player.Center, 0.1f);
                        Projectile.velocity = p - Projectile.Center;//直接强制回归，不要继续攻击了
                        return;//我们的AI就不需要继续往下走了
                    }
                    Vector2 Position = target.Center + (Projectile.Center - target.Center).SafeNormalize(Vector2.Zero) * 150;//计算目标位置//为什么要这么设置目标坐标呢，因为作为一个远程类召唤兽，保持一定距离进行射击才符合逻辑
                    MoveToTarget(Position, 24, 0.3f);//设置追击目标位置，最大速度，加速度
                    AttackShooting(target);//进行攻击AI
                }
            }
            else // 否则说明没目标了，执行回归待机运动
            {
                float dis = Projectile.Distance(mypos);
                if (dis > 1200f)
                {
                    Vector2 p = Vector2.Lerp(Projectile.Center, mypos, 0.1f);
                    Projectile.velocity = p - Projectile.Center;
                }
                Vector2 toPlayer = mypos - Projectile.Center;
                float dist = toPlayer.Length();
                float moveSpeed = 10f;
                float acceleration = 0.18f;

                if (dist > 1f) // 防止零向量
                {
                    toPlayer.Normalize();
                    // 距离越远，速度越快，近距离自动减速
                    float speed = MathHelper.Lerp(0f, moveSpeed, MathHelper.Clamp(dist / 100f, 0f, 1f));
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, toPlayer * speed, acceleration);
                }
                else
                {
                    Projectile.velocity *= 0.92f;
                }
            }
        }


        public override bool PreDraw(ref Color lightColor)
        {
            // 先绘制中国结弹幕
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == Projectile.owner && proj.type == ModContent.ProjectileType<SpringFestivalElf_>())
                {
                    // 只绘制属于自己的中国结
                    if ((int)proj.ai[0] == Projectile.whoAmI)
                    {
                        Texture2D texture_ = Terraria.GameContent.TextureAssets.Projectile[proj.type].Value;
                        Rectangle rectangle_ = new Rectangle(
                            0,
                            texture_.Height / Main.projFrames[proj.type] * proj.frame,
                            texture_.Width,
                            texture_.Height / Main.projFrames[proj.type]
                        );
                        Vector2 center = proj.Center - Main.screenPosition - new Vector2(0, -20f);
                        Color MyColor_ = Color.LightBlue;
                        MyColor_.A = 0;
                        for (int i_ = 0; i_ < ProjectileID.Sets.TrailCacheLength[proj.type]; i_++)
                        {
                            float factor = 1 - (float)i_ / ProjectileID.Sets.TrailCacheLength[proj.type];
                            Vector2 oldcenter = proj.oldPos[i_] + proj.Size / 2 - Main.screenPosition;//获取旧位置的中心点

                            Main.EntitySpriteDraw(texture_, oldcenter - new Vector2(0, -20f), rectangle_, MyColor_ * factor,
                                proj.oldRot[i_],
                                new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[proj.type]),
                                 new Vector2(0.2f),
                                 SpriteEffects.None, 0);
                        }
                        Main.EntitySpriteDraw(
                            texture_,
                            center,
                            rectangle_,
                            Color.White,
                            proj.rotation,
                            new Vector2(texture_.Width / 2, texture_.Height / 2 / Main.projFrames[proj.type]),
                            new Vector2(0.2f),
                            SpriteEffects.None,
                            0
                        );
                    }
                }
            }

            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle rectangle = new Rectangle(
                0,
                texture.Height / Main.projFrames[Type] * Projectile.frame,
                texture.Width,
                texture.Height / Main.projFrames[Type]
                );

            Color MyColor = Color.LightBlue;
            MyColor.A = 0;
            for (int i = 0; i < ProjectileID.Sets.TrailCacheLength[Type]; i++)
            {
                float factor = 1 - (float)i / ProjectileID.Sets.TrailCacheLength[Type];
                Vector2 oldcenter = Projectile.oldPos[i] + Projectile.Size / 2 - Main.screenPosition;//获取旧位置的中心点
                
                Main.EntitySpriteDraw(texture, oldcenter, rectangle, MyColor * factor,
                    Projectile.oldRot[i],
                    new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                     new Vector2(0.5f),
                     SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                rectangle,
                Color.White,
                Projectile.rotation,
                new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                new Vector2(0.5f),
                SpriteEffects.None,
                0
                );

            return false;//return false阻止自动绘制
        }
    }
    public class SpringFestivalElf_ : ModProjectile
    {
        private const float offsetY = 30f; // 距离主体下方的距离
        public override void SetDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            Projectile.width = 10; // 弹幕宽度
            Projectile.height = 10; // 弹幕高度
            Projectile.friendly = true; // 友方弹幕
            Projectile.tileCollide = false; // 不与瓷砖碰撞
            Projectile.DamageType = DamageClass.Summon; // 伤害类型改为召唤伤害
            Projectile.penetrate = -1; // 无限穿透
            Projectile.ignoreWater = true; // 无视液体
            Projectile.timeLeft = 120; // 存在时间无限
            Projectile.light = 0.75f; // 发光亮度
            Projectile.minion = true; // 设置为召唤物
            Projectile.aiStyle = -1;//不使用原版AI
            base.SetDefaults();
        }
        public override void AI()
        {
            int parentIndex = (int)Projectile.ai[0];
            if (parentIndex < 0 || parentIndex >= Main.maxProjectiles)
            {
                Projectile.Kill();
                return;
            }

            Projectile parent = Main.projectile[parentIndex];
            if (!parent.active || parent.type != ModContent.ProjectileType<SpringFestivalElf>() || parent.owner != Projectile.owner)
            {
                Projectile.Kill();
                return;
            }

            // 1. 计算锚点（主体贴图最上方中点）
            Vector2 anchor = parent.Center;
            anchor.Y = parent.position.Y; // 贴图最上方

            // 2. 计算目标位置（始终在主体下方 offsetY 处，无左右晃动）
            Vector2 targetPos = anchor + new Vector2(0, offsetY);

            // 3. 平滑跟随目标点
            Projectile.Center = Vector2.Lerp(Projectile.Center, targetPos, 0.25f);

            // 4. 旋转逻辑
            float maxAngle = MathHelper.ToRadians(60f); // 最大旋转角度60度
            float vx = parent.velocity.X;
            float absVx = Math.Abs(vx);

            if (absVx > 2f)
            {
                // 速度大于2时，按比例旋转，最大±60度
                float angle = MathHelper.Clamp((absVx - 2f) / 10f, 0f, 1f) * maxAngle;
                Projectile.rotation = angle * Math.Sign(vx);
            }
            // 速度小于等于2时，不旋转，保持当前角度
            else
            {
                Projectile.rotation = 0f;
            }

            // 5. 保持弹幕存活
            Projectile.timeLeft = 2;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false; // 阻止自动绘制
        }
    }
    public class SpringFestivalElf_Shoot : ModProjectile
    {
        private bool Targeting = false;
        private float Sum = 1f;
        public override void SetDefaults()
        {
            Projectile.hostile = false; // 敌方伤害
            Projectile.width = 15; // 弹幕宽度
            Projectile.height = 15; // 弹幕高度
            Projectile.friendly = true; // 友方弹幕
            Projectile.tileCollide = false; // 不与瓷砖碰撞
            Projectile.DamageType = DamageClass.Summon; // 伤害类型
            Projectile.penetrate = 1; // 穿透
            Projectile.ignoreWater = true; // 无视液体
            Projectile.timeLeft = 120; // 存在时间，单位为帧
            Projectile.light = 0.75f; // 发光亮度
            base.SetDefaults();
        }
        public override void OnSpawn(IEntitySource source)
        {
            Sum = Main.rand.NextFloat(0.1f, 1f);
            if (Main.rand.Next(2) > 0) Sum *= -1;
        }
        NPC FindBestTarget(Player player, Projectile proj, float searchRange = 200f)
        {
            NPC best = null;
            float minDist = searchRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && !npc.dontTakeDamage && !npc.townNPC && npc.lifeMax > 5 && npc.CanBeChasedBy(Projectile))
                {
                    float distToPlayer = Vector2.Distance(player.Center, npc.Center);
                    if (distToPlayer < minDist &&
                        Collision.CanHitLine(proj.position, proj.width, proj.height, npc.position, npc.width, npc.height))
                    {
                        minDist = distToPlayer;
                        best = npc;
                    }
                }
            }
            if (best != null)
                return best;

            int t = proj.FindTargetWithLineOfSight((int)searchRange);
            if (t >= 0)
                return Main.npc[t];

            return null;
        }
        public override void AI()
        {
            Projectile.rotation += Sum; // 随机旋转
            var player = Main.player[Projectile.owner];
            NPC target = FindBestTarget(player, Projectile);

            // 击中敌人后就不再跟踪目标
            if (target != null && target.active && !Targeting)
            {
                Projectile.velocity = Vector2.Normalize(target.Center - Projectile.Center) * 16f; // 跟踪目标
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Targeting = true;
        }
        public override void OnKill(int timeLeft)
        {
            // 播放爆炸音效
            SoundEngine.PlaySound(SoundID.Item14, Projectile.position);
            if(Main.rand.NextBool(5))
                Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center,
                    Vector2.Zero,
                    ProjectileID.Volcano, // 生成自定义弹幕
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner
                    );
            else
            {
                // 生成烟雾粉尘
                for (int i = 0; i < 2; i++)
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                    dust.velocity *= 1.4f;
                }

                // 生成火焰粉尘
                for (int i = 0; i < 2; i++)
                {
                    Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 3f);
                    dust.noGravity = true;
                    dust.velocity *= 3f;
                    dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2f);
                    dust.velocity *= 2f;
                }

                // 生成大型烟雾石块
                var goreSpawnPosition = new Vector2(Projectile.position.X + Projectile.width / 2 - 24f, Projectile.position.Y + Projectile.height / 2 - 24f);
                Gore gore = Gore.NewGoreDirect(Projectile.GetSource_FromThis(), goreSpawnPosition, default, Main.rand.Next(61, 64), 1f);
                gore.scale = 0.5f;
                gore.velocity.X -= 0.5f;
                gore.velocity.Y += 0.5f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Main.projFrames[Type] = 1;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle rectangle = new Rectangle(
                0,
                texture.Height / Main.projFrames[Type] * Projectile.frame,
                texture.Width,
                texture.Height / Main.projFrames[Type]
                );
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                rectangle,
                Color.White,
                Projectile.rotation,
                new Vector2(texture.Width / 2, texture.Height / 2 / Main.projFrames[Type]),
                new Vector2(0.4f),
                SpriteEffects.None,
                0
                );
            return false;//return false阻止自动绘制
        }
    }
    public class SpringFestivalElfDust : ModDust
    {
        public override bool PreDraw(Dust dust)
        {
            // 获取当前粒子的纹理
            Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Weapons/Summon/SpringFestivalElfDust").Value;

            // 设置粒子的颜色
            Color color = Color.White;

            // 绘制当前粒子
            Main.spriteBatch.Draw(texture,
                dust.position - Main.screenPosition,
                null,
                color,
                dust.rotation,
                new Vector2(texture.Width / 2, texture.Height / 2),
                dust.scale,
                SpriteEffects.None,
                0);
            return false; // 禁用默认绘制
        }


        public override void OnSpawn(Dust dust)
        {
            dust.velocity *= 1f;   // 设置初始速度
            dust.scale = 1f;       // 设置粒子的大小
            dust.noGravity = true;    // 设置粒子无重力
            dust.fadeIn = 1f;         // 设置渐隐时间
        }

        public override bool Update(Dust dust)
        {
            // 控制粒子的运动
            dust.position += dust.velocity; // 更新位置
            dust.scale -= 0.01f;            // 粒子逐渐变小
            if (dust.scale <= 0)             // 如果变得太小，就消失
            {
                dust.active = false;
            }

            // 计算速度方向的旋转角度
            if (dust.velocity != Vector2.Zero) // 确保速度不是零
            {
                dust.rotation = dust.velocity.ToRotation(); // 将右侧旋转朝向速度方向，+ Pi/2 用于将默认方向（正右）调整到粒子方向
            }
            return true; // 继续更新
        }
    }
    public class BuffsSpringFestivalElf : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false; // 设置为false，表示这是一个增益buff
            Main.buffNoSave[Type] = true; // 设置为true，退出世界后不会保留该buff
            Main.buffNoTimeDisplay[Type] = true; // 设置为true，在屏幕上不会显示时间
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 如果召唤物存在，那么延长buff时间，否则删除BUFF
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SpringFestivalElf>()] > 0)//检测玩家持有的弹幕数量
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

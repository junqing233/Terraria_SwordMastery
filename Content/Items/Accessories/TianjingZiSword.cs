using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.Weapons.Sword;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace SwordMastery.Content.Items.Accessories
{
    public class MyGlobalNPC : GlobalNPC
    {
        //public class DropConditions
        //{
        //    public class Hardmode : IItemDropRuleCondition
        //    {
        //        public bool CanDrop(DropAttemptInfo info) =>
        //            Main.hardMode;
        //        public bool CanShowItemDropInUI() => true;
        //        public string GetConditionDescription() => "这是击败血肉之墙后的掉落率喵~";
        //    }
        //}
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.QueenSlimeBoss)
            {
                // 普通模式掉落规则
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TianjingZiSword>(), 30, 1, 1));
                npcLoot.Add(notExpertRule);

                // 专家模式掉落规则
                LeadingConditionRule expertRule = new LeadingConditionRule(new Conditions.IsExpert());
                expertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TianjingZiSword>(), 19, 1, 1));
                npcLoot.Add(expertRule);
            }
            //if (npc.type == NPCID.KingSlime)
            //{
            //    // 困难模式
            //    npcLoot.Add(ItemDropRule.ByCondition(
            //        new DropConditions.Hardmode(),
            //        ModContent.ItemType<TianjingSword>(), 30, 1, 1));
            //}
        }
    }
   
    //天晶子剑
    [AutoloadEquip(EquipType.Back)]
    public class TianjingZiSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 62; // 饰品宽度
            Item.height = 62; // 饰品高度
            Item.value = Item.buyPrice(gold: 1); // 价值
            Item.rare = ItemRarityID.Pink; // 稀有度
            Item.accessory = true; // 设为装备
            Item.defense = 2; // 防御力加成
        }
        // 合成材料
        //public override void AddRecipes()
        //{
        //    
        //}
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Language.ActiveCulture.Name == "zh-Hans")
                tooltips.Add(new TooltipLine(Mod, "", ""));
            else
                tooltips.Add(new TooltipLine(Mod, "", ""));
        }
        public override void UpdateInventory(Player player)
        {
            
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<TianjingZiSwordPlayer>().hasTianjingZiSword = true;
        }
    }
    public class TianjingZiSwordPlayer : ModPlayer
    {
        public bool hasTianjingZiSword;

        public override void ResetEffects()
        {
            hasTianjingZiSword = false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!hasTianjingZiSword)
                return;

            // 检查当前玩家是否已有 TianjingZiSwordProj_Head 弹幕
            bool exists = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Player.whoAmI && p.type == ModContent.ProjectileType<TianjingZiSwordProj_Head>())
                {
                    exists = true;
                    break;
                }
            }
            if (exists)
                return;
            int Damage = damageDone / 2;
            // 25%概率发射
            if (Main.rand.NextFloat() < 0.25f)
            {
                Projectile.NewProjectile(
                    Player.GetSource_Misc("TianjingZiSword"),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<TianjingZiSwordProj_Head>(),
                    Damage >= 10 ? Damage : 10, // 使用本次触发的实际伤害
                    1, // 可自定义击退
                    Player.whoAmI
                );
            }
        }
    }
    public class TianjingZiSwordProj_Head : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Accessories/TianjingZiSwordProj_Head";

        private float lemniscateT = 0f; // 8字形参数
        private readonly float lemniscateSpeed = 0.05f; // 控制运动快慢
        private Vector2 centerPos; // 8字形中心（目标点）
        private float playerCircleT = 0f; // 玩家绕圈参数

        private Vector2 lastDesiredPos;
        private Vector2 playerCircleTarget = Vector2.Zero;


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
                ModContent.ProjectileType<TianjingZiSwordProj_Body_1>(),
                ModContent.ProjectileType<TianjingZiSwordProj_Body_0>(),
                ModContent.ProjectileType<TianjingZiSwordProj_Body_1>(),
                ModContent.ProjectileType<TianjingZiSwordProj_Body_1>(),
                ModContent.ProjectileType<TianjingZiSwordProj_Body_0>(),
                ModContent.ProjectileType<TianjingZiSwordProj_Body_2>(),
                ModContent.ProjectileType<TianjingZiSwordProj_Body_3>()
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
                int tailType = ModContent.ProjectileType<TianjingZiSwordProj_Tail>();
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
        public override bool PreDraw(ref Color lightColor)
        {
            // 加载头部贴图
            Texture2D texture = ModContent.Request<Texture2D>("SwordMastery/Content/Items/Accessories/TianjingZiSwordProj_Head").Value;
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
                0.75f,
                effects,
                0f
            );
            return false;
        }
    }
    // 以Body_0为例
    public class TianjingZiSwordProj_Body_0 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Accessories/TianjingZiSwordProj_Body_0";

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
            float desiredDist = 27f; // 可根据贴图调整
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
                0.75f,
                effects,
                0
            );
            //Main.NewText(toPrev.X);
            return false;
        }
    }

    // 以Body_1为例
    public class TianjingZiSwordProj_Body_1 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Accessories/TianjingZiSwordProj_Body_1";

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
            float desiredDist = 27f;
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
                0.75f,
                effects,
                0
            );
            return false;
        }
    }

    // 以Body_2为例
    public class TianjingZiSwordProj_Body_2 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Accessories/TianjingZiSwordProj_Body_2";

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
            float desiredDist = 27f;
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
                0.75f,
                effects,
                0
            );
            return false;
        }
    }

    // 以Body_3为例
    public class TianjingZiSwordProj_Body_3 : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Accessories/TianjingZiSwordProj_Body_3";

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
            float desiredDist = 27f;
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
                0.75f,
                effects,
                0
            );
            return false;
        }
    }
    public class TianjingZiSwordProj_Tail : ModProjectile
    {
        public override string Texture => "SwordMastery/Content/Items/Accessories/TianjingZiSwordProj_Tail";

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
            float desiredDist = 27f;
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
                0.75f,
                effects,
                0
            );
            return false;
        }
    }
}
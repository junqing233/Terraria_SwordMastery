using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.Accessories;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Net.WebRequestMethods;
using static Terraria.GameContent.Animations.Actions.Sprites;

namespace SwordMastery.Content.Items.Weapons.Bow
{
    public class HeavenlyPunishmentNPC : GlobalNPC
    {
        public class HeavenlyPunishmentDropConditions
        {
            public class Hardmode : IItemDropRuleCondition
            {
                public bool CanDrop(DropAttemptInfo info) =>
                    Main.hardMode;
                public bool CanShowItemDropInUI() => true;
                public string GetConditionDescription() => "在困难模式下掉落喵~";
            }
        }
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.GiantBat)
            {
                // 普通模式掉落规则
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HeavenlyPunishment>(), 300, 1, 1));
                npcLoot.Add(notExpertRule);

                // 专家模式掉落规则
                LeadingConditionRule expertRule = new LeadingConditionRule(new Conditions.IsExpert());
                expertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HeavenlyPunishment>(), 200, 1, 1));
                npcLoot.Add(expertRule);
            }
            if (npc.type == NPCID.CaveBat)
            {
                // 困难模式
                npcLoot.Add(ItemDropRule.ByCondition(
                    new HeavenlyPunishmentDropConditions.Hardmode(),
                    ModContent.ItemType<HeavenlyPunishment>(), 150, 1, 1));
            }
        }
    }
    public class HeavenlyPunishment : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 36;
            Item.crit = 3;
            Item.DamageType = DamageClass.Ranged; // 远程
            Item.width = 42;
            Item.height = 108;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.knockBack = 1;
            Item.value = Item.buyPrice(1, 0, 0, 16); // 物品价值
            Item.rare = ItemRarityID.Green; // 稀有度
            Item.noMelee = true; // 无法近战
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true; // 自动使用
            Item.noUseGraphic = false; // 显示使用动画
            Item.useAmmo = AmmoID.Arrow; // 指定使用的弹药类型（箭）
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.shootSpeed = 16f;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "HeavenlyPunishment", "神兵") { OverrideColor = new Color(127, 0, 255) });
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(8,0); //手持位置偏移
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            // 获取物品的纹理
            Texture2D texture = Terraria.GameContent.TextureAssets.Item[Item.type].Value;
            Vector2 offset = new(texture.Width / 2, texture.Height / 2);
            // 绘制拖尾效果
            spriteBatch.Draw(texture, position, null, drawColor, 0.8f, offset, scale*1.4f, SpriteEffects.None, 0f);
            return false;
        }
        public override bool CanUseItem(Player player)
        {
            // 检查是否有足够的弹药
            return player.active && player.HasAmmo(Item); // 直接检查是否有弹药
        }
        //public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        //{
        //    Item ammo = player.inventory.FirstOrDefault(i => i.ammo == AmmoID.Arrow && i.stack > 0);
        //    Projectile.NewProjectile(source, position, velocity, ammo.shoot, damage, knockback, player.whoAmI);

        //    return false; // 阻止默认发射
        //}
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int arrowCount = 9;
            float spacing = 10f;
            Vector2 shootDir = velocity.SafeNormalize(Vector2.UnitX);
            Vector2 vertical = new Vector2(-shootDir.Y, shootDir.X);
            int mid = arrowCount / 2;

            // 靠前/靠后偏移表（单位：像素）
            // 负数为靠后，正数为靠前
            int[] forwardOffsets = { -10, -10, 0, 10, 20, 10, 0, -10, -10 };

            for (int i = 0; i < arrowCount; i++)
            {
                float offset = (i - mid) * spacing;
                Vector2 spawnPos = position + vertical * offset + shootDir * forwardOffsets[i];
                int projIndex = Projectile.NewProjectile(source, spawnPos, velocity, type, damage, knockback, player.whoAmI);

                if (projIndex >= 0 && projIndex < Main.maxProjectiles)
                {
                    Projectile proj = Main.projectile[projIndex];
                    proj.usesLocalNPCImmunity = true;
                    proj.localNPCHitCooldown = 10;
                }
            }

            return false;
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Accessories
{
    public class RavageCrabNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.Crab)
            {
                // 普通模式掉落规则
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RavageCrab>(), 100, 1, 1));
                npcLoot.Add(notExpertRule);

                // 专家模式掉落规则
                LeadingConditionRule expertRule = new LeadingConditionRule(new Conditions.IsExpert());
                expertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RavageCrab>(), 80, 1, 1));
                npcLoot.Add(expertRule);
            }
        }
    }
    public class RavageCrab : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 96; // 饰品宽度
            Item.height = 68; // 饰品高度
            Item.value = Item.sellPrice(1, 0, 42, 0); // 商店售卖价格
            Item.rare = ItemRarityID.Blue; // 稀有度
            Item.accessory = true; // 设为装备
            Item.defense = 2; // 防御力加成
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "RavageCrab", "神兵") { OverrideColor = Color.DeepSkyBlue });
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<RavageCrabPlayer>().ravageCrabEquipped = true;
        }
    }

    public class RavageCrabPlayer : ModPlayer
    {
        public bool ravageCrabEquipped;

        public override void ResetEffects()
        {
            ravageCrabEquipped = false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryApplyDebuff(target);
            //Main.NewText(ravageCrabEquipped);
        }    
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryApplyDebuff(target);
        }

        private void TryApplyDebuff(NPC target)
        {
            if (ravageCrabEquipped)
            {
                int buffType = ModContent.BuffType<BuffsRavageCrab>();
                target.AddBuff(buffType, 120); // 2秒
                // 叠加层数
                if (target.TryGetGlobalNPC<RavageCrabDebuffGlobalNPC>(out var debuff))
                {
                    debuff.ravageCrabDebuffStacks++;
                }
            }
        }
    }

    class BuffsRavageCrab : ModBuff
    {
        public override string Texture => GetType().Namespace.Replace(".", "/") + "/Buff";
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }

    public class RavageCrabDebuffGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public float ravageCrabDebuffStacks = 0;

        public override void ResetEffects(NPC npc)
        {
            // 如果buff不存在，层数归零
            if (!npc.HasBuff(ModContent.BuffType<BuffsRavageCrab>())) ravageCrabDebuffStacks = 0;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // 减少敌人护甲，每层-1防御
            if (ravageCrabDebuffStacks > 0)
                modifiers.Defense -= ravageCrabDebuffStacks * 0.1f;
        }
    }
}
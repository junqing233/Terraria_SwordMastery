using Microsoft.Xna.Framework;
using SwordMastery.Content.Items.Accessories;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Weapons.Miscellaneous
{
    public class ShennongRulerGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            // 雪原生物类型列表（可根据需要补充）
            int[] snowBiomeNPCs = new int[]
            {
                NPCID.CyanBeetle,//青壳虫
                NPCID.IceMimic,//冰雪宝箱怪
                NPCID.IceGolem,//冰雪巨人
            };

            if (snowBiomeNPCs.Contains(npc.type))
            {
                // 普通模式掉落规则
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShennongRuler>(), 300, 1, 1));
                npcLoot.Add(notExpertRule);

                // 专家模式掉落规则
                LeadingConditionRule expertRule = new LeadingConditionRule(new Conditions.IsExpert());
                expertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShennongRuler>(), 190, 1, 1));
                npcLoot.Add(expertRule);
            }
            // 雪原生物类型列表（可根据需要补充）
            int[] snowBiomeNPCs_ = new int[]
            {
                NPCID.IceSlime,//冰雪史莱姆
                NPCID.SpikedIceSlime,//冰雪尖刺史莱姆
                NPCID.IceBat,//冰雪蝙蝠
            };

            if (snowBiomeNPCs_.Contains(npc.type))
            {
                // 普通模式掉落规则
                LeadingConditionRule notExpertRule = new LeadingConditionRule(new Conditions.NotExpert());
                notExpertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShennongRuler>(), 1000, 1, 1));
                npcLoot.Add(notExpertRule);

                // 专家模式掉落规则
                LeadingConditionRule expertRule = new LeadingConditionRule(new Conditions.IsExpert());
                expertRule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShennongRuler>(), 500, 1, 1));
                npcLoot.Add(expertRule);
            }
        }
    }
    public class ShennongRuler : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 60;
            Item.maxStack = 1;
            Item.value = 1;
            Item.useAnimation = 30;//使用动画持续时间
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.consumable = false;// 物品是否可消耗
            Item.noUseGraphic = false; // 确保图形显示
            Item.rare = ItemRarityID.Green; // 物品稀有度
            //价值
            Item.value = Item.sellPrice(2, 0, 50);
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "ShennongRuler", "神兵") { OverrideColor = Color.OrangeRed });
        }
        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.PotionSickness))
            { return false; }
            else
            { return true; }
        }
        public override bool? UseItem(Player player)
        {
            // 计算最大生命值的40%~80%
            int minHeal = (int)(player.statLifeMax2 * 0.4f);
            int maxHeal = (int)(player.statLifeMax2 * 0.8f);
            int healAmount = Main.rand.Next(minHeal, maxHeal + 1);

            player.statLife += healAmount;
            player.HealEffect(healAmount);

            // 检查生命值不要超过最大值
            if (player.statLife > player.statLifeMax2)
            {
                player.statLife = player.statLifeMax2;
            }

            // 计算药水疾病时间，40%为1200帧，80%为2400帧，线性插值
            float healPercent = (float)(healAmount - minHeal) / (maxHeal - minHeal);
            int minSickness = 1200; // 20秒
            int maxSickness = 2400; // 40秒
            int sicknessTime = minSickness + (int)((maxSickness - minSickness) * healPercent);

            player.AddBuff(BuffID.PotionSickness, sicknessTime);

            // 再次播放音效
            SoundEngine.PlaySound(SoundID.Item3, player.position);
            return true;
        }
    }
}
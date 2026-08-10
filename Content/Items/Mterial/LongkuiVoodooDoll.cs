using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Mterial
{
    public class LongkuiVoodooDoll : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 32;
            Item.accessory = true;
            Item.defense = 0;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.buyPrice(platinum: 1);
            Item.hasVanityEffects = true;
            Item.maxStack = 9999;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (Language.ActiveCulture.Name == "zh-Hans") // 检查是否为简体中文
                tooltips.Add(new TooltipLine(Mod, "LongkuiVoodooDollTooltip", "“我是世上唯一够资格为你跳下去的人”\n"+"扔进岩浆，获得龙葵的牺牲\n"+"[c/78cdc9:仙剑奇侠传]"));
            else
                tooltips.Add(new TooltipLine(Mod, "LongkuiVoodooDollTooltip", "I am the only one who can jump down for you\n"+ "Throw in the magma to get the sacrifice of the Solanuma\n" + "[c/78cdc9:The Legend of Sword and Fairy]"));
        }
        
        //合成配方
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.TheBrideDress, 1)
                .AddIngredient(ItemID.SkyBlueFlower, 1)
                .AddIngredient(ItemID.GuideVoodooDoll, 1)
                .AddIngredient(ItemID.GenderChangePotion, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        
        //饰品效果
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            
        }
        public override void UpdateVanity(Player player)
        {
            
        }
    }
    public class LongkuiVoodooDollGlobal : GlobalItem
    {
        public override void Update(Item item, ref float gravity, ref float maxFallSpeed)
        {
            // 判断物品类型和是否在岩浆
            if (item.type == ModContent.ItemType<LongkuiVoodooDoll>() && item.lavaWet && item.active)
            {
                // 查找最近的玩家
                int playerIndex = Player.FindClosest(item.position, item.width, item.height);
                Player nearestPlayer = Main.player[playerIndex];
                // 添加Buff
                nearestPlayer.AddBuff(ModContent.BuffType<BuffsDemonBladAddRecipes>(), 6000);
                
                CombatText.NewText(new Rectangle((int)nearestPlayer.position.X, (int)nearestPlayer.position.Y - 20, nearestPlayer.width, nearestPlayer.height),
                        new Color(120, 205, 201), "获得龙葵的牺牲Buff"); // 显示文本提示
                // 让物品销毁，避免重复触发
                item.active = false;
            }
        }
    }
    class BuffsDemonBladAddRecipes : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
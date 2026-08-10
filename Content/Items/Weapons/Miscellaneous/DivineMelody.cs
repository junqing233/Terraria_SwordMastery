using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Weapons.Miscellaneous
{
    public class DivineMelody : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 42;
            Item.maxStack = 1;
            Item.value = 1;
            Item.useAnimation = 30;//使用动画持续时间
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Guitar;
            //Item.UseSound = SoundID.Item1;
            Item.consumable = false;// 物品是否可消耗
            Item.noUseGraphic = false; // 确保图形显示
            Item.rare = ItemRarityID.Green; // 物品稀有度
            //价值
            Item.value = Item.sellPrice(1, 0, 50);
            Item.mana = 100;
            Item.autoReuse = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Insert(0, new TooltipLine(Mod, "DivineMelody", "神兵") { OverrideColor = Color.Goldenrod });
        }
        public override bool? UseItem(Player player)
        {
            // 消除所有减益buff
            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int buffType = player.buffType[i];
                if (buffType > 0 && Main.debuff[buffType] && buffType != BuffID.PotionSickness)
                {
                    player.DelBuff(i);
                    i--; // 删除后索引前移，避免跳过下一个buff
                }
            }
            player.AddBuff(ModContent.BuffType<BuffsDivineMelody>(), 3600);

            // 随机播放一个吉他音效
            var sounds = new[] 
            {
                SoundID.GuitarAm,
                SoundID.GuitarBm,
                SoundID.GuitarC,
                SoundID.GuitarD,
                SoundID.GuitarEm,
                SoundID.GuitarG
            };
            var sound = sounds[Main.rand.Next(sounds.Length)];
            SoundEngine.PlaySound(sound, player.position);

            return true;
        }
        public override void AddRecipes()
        {
            // 创建一个新的配方组
            RecipeGroup group = new RecipeGroup(() => "任意鹦鹉",
                ItemID.ScarletMacaw,
                ItemID.BlueMacaw,
                ItemID.YellowCockatiel,
                ItemID.GrayCockatiel);

            // 注册配方组
            RecipeGroup.RegisterGroup("SwordMastery:DivineMelodyGroup", group);

            CreateRecipe()
               .AddRecipeGroup("SwordMastery:DivineMelodyGroup", 2) // 使用配方组
               .AddIngredient(ItemID.SoulofLight, 10) // 光明之魂
               .AddIngredient(ItemID.Harp, 1) // 竖琴
               .AddTile(TileID.MythrilAnvil) // 秘银砧
               .Register();
        }
    }
    public class BuffsDivineMelody : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = false; // 设置为false，表示这是一个增益buff
            Main.buffNoSave[Type] = true; // 设置为true，退出世界后不会保留该buff
            Main.buffNoTimeDisplay[Type] = false; // 设置为true，在屏幕上不会显示时间
        }

        public override void Update(Player player, ref int buffIndex)//击败月球领主
        {
            player.lifeRegen += 10; // 增加生命恢复
            player.statDefense += 20; // 增加防御
            player.moveSpeed += 0.5f;
            player.GetDamage(DamageClass.Generic) += 0.5f;// 增加50%的伤害
            player.GetArmorPenetration(DamageClass.Generic) += 20; // 增加穿甲
        }
    }
}
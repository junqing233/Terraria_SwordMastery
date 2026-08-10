using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace SwordMastery.Content.Items.Mterial
{
    public class RemoveLightStone : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = 9999;
            Item.value = Item.buyPrice(silver: 1);
            Item.rare = ItemRarityID.White;
        }
        public override void PostUpdate()
        {
            float intensity = 2f; // 控制光芒强度，越小越淡
            //Color(216, 71, 238)
            //•	R: 216 / 255 ≈ 0.82
            //•	G: 71 / 255 ≈ 0.29
            //•	B: 238 / 255 ≈ 0.93
            Lighting.AddLight(Item.Center, 0.82f * intensity, 0.29f * intensity, 0.93f * intensity);
        }
    }
}
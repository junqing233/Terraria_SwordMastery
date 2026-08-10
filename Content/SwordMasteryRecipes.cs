using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content
{
	// 该类包含了一些创建物品合成配方的示例。
	// 配方的详细说明可以在 https://github.com/tModLoader/tModLoader/wiki/Basic-Recipes 和 https://github.com/tModLoader/tModLoader/wiki/Intermediate-Recipes 维基页面中找到。请访问维基以了解更多关于配方的信息，如果内容不清楚的话。
	public class SwordMasteryRecipes : ModSystem
	{
		public override void AddRecipes()
		{
            //////////////////////////////////////////////////////////////////////////////////////
            // 以下基本配方将 1 个脊骨制作成 1 个 腐肉。
            //////////////////////////////////////////////////////////////////////////////////////
            Recipe recipe = Recipe.Create(ItemID.RottenChunk, 1);
			// 为配方添加一个脊骨作为原材料。
			recipe.AddIngredient(ItemID.Vertebrae);
			// 完成后调用此方法注册配方。
			recipe.Register();
		}
	}
}

using SwordMastery.Content.GlobaProjectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SwordMastery.Content.Prefixes
{
	// 该类用于声明物品“前缀”或“修饰符”。
	public class SwordMasteryPrefix : ModPrefix
	{
		// 我们在这里声明一个自定义的 *虚拟* 属性，以便另一个类型 ExampleDerivedPrefix 可以覆盖它并为自己改变有效的力量值。
		public virtual float Power => 1f;

		// 通过这种方式更改你的类别，默认为 PrefixCategory.Custom。影响哪些物品可以获取这个前缀。
		public override PrefixCategory Category => PrefixCategory.AnyWeapon;

		// 查看文档以获取原版权重和其他信息。
		// 如果有多个功能相似的前缀，可以使用 switch/case 为不同的前缀提供不同的几率。
		// 注意：权重为 0f 的前缀仍然可能被选中。请参阅 CanRoll 以排除前缀。
		// 注意：如果你使用 PrefixCategory.Custom，请实际使用 ModItem.ChoosePrefix。
		public override float RollChance(Item item) {
			return 0.8f;
		}

		// 确定是否可以滚动前缀。
		// 使用此方法来控制前缀是否可以被选中。
		public override bool CanRoll(Item item) 
		{
            // 只有开启配置时才允许出现
            return ModContent.GetInstance<SwordMasteryConfig>().EnableSwordMasteryPrefixes;
        }

        // 使用此函数来修改具有此前缀的物品的这些属性：
        // 攻击力倍增器、击退倍增器、使用时间倍增器、大小倍增器、发射速度倍增器、法力消耗倍增器、暴击加成。
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus) {
			damageMult *= 1f + 0.36f * Power;
            useTimeMult *= 1f - 0.12f * Power;//攻速
        }

		// 使用此函数来修改具有此修饰符的物品的成本。
		public override void ModifyValue(ref float valueMult) {
			valueMult *= 1f + 0.05f * Power;
		}
		// 使用此函数来修改具有此修饰符的物品的大多数其他属性。
		public override void Apply(Item item) 
		{
			
		}

		// 此前缀不影响任何非标准属性，因此这些额外的工具提示行实际上并不必要，
		// 但这种模式可以用于影响其他属性的前缀。
		public override IEnumerable<TooltipLine> GetTooltipLines(Item item) 
		{
			// 由于继承，此代码将为 ExamplePrefix 和 ExampleDerivedPrefix 运行。
			// 我们添加了两行工具提示，第一行是典型的前缀工具提示行，显示属性提升，
			// 而其他行是附加的风味文本。
			// Mods.ExampleMod.Prefixes.PowerTooltip 的本地化键使用了特殊格式，会自动在值前添加 + 或 -。
			// 此共享本地化键格式化为 Power 值，因此 ExamplePrefix 和 ExampleDerivedPrefix 将有不同的文本。
			// 这将导致 ExamplePrefix 为 "+1 Power"，ExampleDerivedPrefix 为 "+2 Power"。
			// Power 不是一个实际的属性，Power 的效果已经在 "+X% 攻击力" 的工具提示中显示，
			// 因此此示例仅用于教育目的。
			yield return new TooltipLine(Mod, "PrefixWeaponAwesome", PowerTooltip.Format(Power)) 
			{
				IsModifier = true, // 设置颜色为积极修饰符颜色。
			};

			// 此本地化键不被继承类共享。
			//ExamplePrefix 和 ExampleDerivedPrefix 为这条线有自己的本地化。
			yield return new TooltipLine(Mod, "PrefixWeaponAwesomeDescription", AdditionalTooltip.Value)
			{
				IsModifier = true,
			};

			// 如果可能且合适，请尝试重用 Terraria 前缀的名称标识符和本地化值。
			// 例如，此代码使用原版的防御词的本地化，结果为 "-5 防御"。
			// 注意这里使用了 IsModifierBad 来表示负面修饰符。
			/*yield return new TooltipLine(Mod, "PrefixAccDefense", "-5" + Lang.tip[25].Value)
            {
                IsModifier = true,
                IsModifierBad = true,
            };*/
		}

		// PowerTooltip 在 ExamplePrefix 和 ExampleDerivedPrefix 之间共享。
		public static LocalizedText PowerTooltip { get; private set; }

		// AdditionalTooltip 展示了如何使用可继承的本地化属性的方法。
		// 这是必要的，因为此示例使用了继承，并且我们希望为每个继承类提供不同的本地化文本。
		// https://github.com/tModLoader/tModLoader/wiki/Localization#inheritable-localized-properties
		public LocalizedText AdditionalTooltip => this.GetLocalization(nameof(AdditionalTooltip));

		public override void SetStaticDefaults() {
			// 这里没有使用 this.GetLocalization，因为我们希望使用一个共享键
			PowerTooltip = Mod.GetLocalization($"{LocalizationCategory}.{nameof(PowerTooltip)}");

			// 这段看似无用的代码是必需的，以正确注册 AdditionalTooltip 的键
			_ = AdditionalTooltip;
		}
	}
}
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SwordMastery.Content.Items.Weapons.Whip
{
	public class WhipDebuff : ModBuff
	{
		// 定义标签伤害的数值
		public static readonly int TagDamage = 5;

		public override void SetStaticDefaults() {
			// 这使得该削弱效果可以施加到原本对所有削弱效果免疫的NPC上。
			// 其他模组可能也会检查此属性用于不同的目的。
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}

	public class WhipAdvancedDebuff : ModBuff
	{
        public override string Texture => "SwordMastery/Content/Items/Weapons/Whip/WhipDebuff";
		// 定义标签伤害的百分比数值
		public static readonly int TagDamagePercent = 30;
		// 计算标签伤害的乘数
		public static readonly float TagDamageMultiplier = TagDamagePercent / 100f;

		public override void SetStaticDefaults() {
			// 这使得该削弱效果可以施加到原本对所有削弱效果免疫的NPC上。
			BuffID.Sets.IsATagBuff[Type] = true;
		}
	}

	public class WhipDebuffNPC : GlobalNPC
	{
		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) {
			// 只有玩家的攻击应该受益于此效果，因此检查projectile是否来自NPC、是否为陷阱以及是否与召唤物或哨兵有关。
			if (projectile.npcProj || projectile.trap || !projectile.IsMinionOrSentryRelated)
				return;

			// SummonTagDamageMultiplier用于平衡某些特定的召唤物和哨兵的投射物伤害，根据其类型进行缩放。
			var projTagMultiplier = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
			if (npc.HasBuff<WhipDebuff>()) {
				// 对每次攻击应用一个固定的伤害加成
				modifiers.FlatBonusDamage += WhipDebuff.TagDamage * projTagMultiplier;
			}

			// 如果你模组中有许多削弱效果，循环遍历NPC.buffType和buffTime数组一次，并跟踪找到的效果，可能比多次调用HasBuff更快
			if (npc.HasBuff<WhipAdvancedDebuff>()) {
				// 应用缩放伤害加成到下一次攻击，并然后移除该削弱效果，类似于原版的烟火
				modifiers.ScalingBonusDamage += WhipAdvancedDebuff.TagDamageMultiplier * projTagMultiplier;
				npc.RequestBuffRemoval(ModContent.BuffType<WhipAdvancedDebuff>());
			}
		}
	}
}

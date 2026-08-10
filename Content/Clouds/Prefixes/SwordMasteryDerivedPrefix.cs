using Microsoft.Xna.Framework;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.FlyingSword.AGlobalControl;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SwordMastery.Content.Prefixes//嗜血
{
    // 确保先查看 'ExamplePrefix'。
    // 这个类展示了如何使用继承来更轻松地创建单个前缀的变体。
    // 话虽如此，请记住继承只是程序员可用的成千上万种工具之一，并且伴随著巨大的权力也带来了巨大的责任。
    public class SuckBlood : SwordMasteryPrefix
    {
        public override float Power => base.Power * 0f;
        public override float RollChance(Item item) => 0.8f;

        public override void Apply(Item item)
        {
            // 标记该物品具有吸血效果
            item.GetGlobalItem<SwordMasteryGlobalItem>().SuckBlood = true;
            item.noMelee = false;
            item.noUseGraphic = false;
            item.autoReuse = true;
            item.useStyle = ItemUseStyleID.Swing;
            //item.shoot = ProjectileID.None;
        }
        
        // 可选：自定义属性修改
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            useTimeMult *= 1f;
            scaleMult *= 1.8f;
        }
    }
    public class BloodFiend : SwordMasteryPrefix//血煞
    {
        public override float Power => base.Power * 0f;
        public override float RollChance(Item item) => 0.8f;

        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1f + 0.5f;
        }
        public override void Apply(Item item)
        {
            // 标记该物品具有吸血效果
            item.GetGlobalItem<SwordMasteryGlobalItem>().BloodFiend = true;
        }
    }

    public class SwordMasteryGlobalItem : GlobalItem
    {
        public bool SuckBlood = false;
        public bool BloodFiend = false;
        
        public override bool InstancePerEntity => true;

        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(item, player, target, hit, damageDone);
            if (SuckBlood && damageDone > 0 && !target.friendly && target.lifeMax > 5)
            {
                int healAmount = (int)(damageDone * 0.08f); // 吸血量为造成伤害的8%
                if (healAmount > 0)
                {
                    player.statLife += healAmount;
                    player.HealEffect(healAmount, true);
                }else
                {
                    player.statLife += 1;
                    player.HealEffect(1, true);
                }
            }
            if (BloodFiend && damageDone > 0 && !target.friendly && target.lifeMax > 5)
            {
                float chance = Main.rand.NextFloat(); // 0~1之间
                if (chance < 0.8f)
                {
                    // 80%概率吸血
                    int healAmount = Math.Min(damageDone / 25, 3);
                    if (healAmount > 0)
                    {
                        player.statLife += healAmount;
                        player.HealEffect(healAmount, true);
                    }
                    else
                    {
                        player.statLife += 1;
                        player.HealEffect(1, true);
                    }
                }
                else
                {
                    // 20%概率减少血量
                    int loseAmount = Math.Min(damageDone / 25, 3);
                    player.statLife -= loseAmount;
                    if (loseAmount > 0)
                    {
                        player.statLife -= loseAmount;
                        CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, loseAmount);
                    }
                    else
                    {
                        player.statLife -= 1;
                        CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, 1);
                    }
                    if (player.statLife <= 0)
                        player.KillMe(Language.ActiveCulture.Name == "zh-Hans" ? PlayerDeathReason.ByCustomReason($"{player.name}未能熬过的血煞洗礼！"): 
                            PlayerDeathReason.ByCustomReason($"The baptism of blood that {player.name} have not survived."), 9999, 0);
                }
            }
        }
        
        public override void HoldItem(Item item, Player player)
        {
            base.HoldItem(item, player);
            // 血煞
            if (item.GetGlobalItem<SwordMasteryGlobalItem>().BloodFiend ||
               (MagicSachetProj.appliedBloodFiend && player.HasBuff<BuffsMagicSachet>()) ||
                (player.HasBuff<BuffsFlyingsword>() && FlyingswordProj.appliedBloodFiend) ||
                (player.HasBuff<BuffsFlyingGun>() && FlyingGunProj.appliedBloodFiend))
            {
                player.statLifeMax2 = (int)(player.statLifeMax2 * 0.8f);
            }
            // 王牌
            if (item.prefix == ModContent.PrefixType<UltimateFinisher>())
            {
                player.GetModPlayer<UltimateFinisherPlayer>().ultimatefinisher = true;
                player.GetModPlayer<UltimateFinisherPlayer>().ultimatefinisherItem = item;
            }else
            {
                player.GetModPlayer<UltimateFinisherPlayer>().ultimatefinisher = false;
                player.GetModPlayer<UltimateFinisherPlayer>().ultimatefinisherItem = null;
            }
            // 神佑前缀：增加防御力
            if (item.prefix == ModContent.PrefixType<DivineBlessing>())
            {
                int def = Math.Clamp(player.GetWeaponDamage(item) / 10, 1, 20);
                player.statDefense += def;
            }
        }
        public override void UpdateAccessory(Item item, Player player, bool hideVisual)
        {
            base.UpdateAccessory(item, player, hideVisual);
            // 契约
            if (item.prefix == ModContent.PrefixType<PrimeCall>())
            {
                player.maxMinions += 1;//增加召唤栏
                player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.02f; // 增加攻速
            }
            // 号令
            if (item.prefix == ModContent.PrefixType<DualSoul>())
            {
                player.maxMinions += 1;//增加召唤栏
                player.whipRangeMultiplier += 0.02f; //增加鞭攻击距离
            }
            var config = ModContent.GetInstance<SwordMasteryConfig>();
            if (config.EnableExtraAffix)
            {
                //初灵
                if (item.prefix == ModContent.PrefixType<PrimeCall_>())
                {
                    player.maxMinions += 1;//增加召唤栏
                }
                //双魂
                if (item.prefix == ModContent.PrefixType<DualSoul_>())
                {
                    player.maxMinions += 2;//增加召唤栏
                }
                //叁契
                if (item.prefix == ModContent.PrefixType<TriBond>())
                {
                    player.maxMinions += 3;//增加召唤栏
                }
                //肆御
                if (item.prefix == ModContent.PrefixType<QuadHost>())
                {
                    player.maxMinions += 4;//增加召唤栏
                }
            }
        }
        public override bool CanConsumeAmmo(Item weapon, Item ammo, Player player)
        {
            if (weapon.prefix == ModContent.PrefixType<Infinity>())
            {
                // 99%概率不消耗弹药
                return Main.rand.NextFloat() >= 0.99f;
            }
            return base.CanConsumeAmmo(weapon, ammo, player);
        }
        
    }
    public class SwordMasteryGlobalProjectile : GlobalProjectile
    {
        public bool HasSuckedBlood = false; // 是否已吸血
        public override bool InstancePerEntity => true;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            //Main.NewText(item.GetGlobalItem<SwordMasteryGlobalItem>().BloodFiend);
            Player player = Main.player[projectile.owner];
            Item item = player.HeldItem;
            if ((item != null && !item.IsAir && item.GetGlobalItem<SwordMasteryGlobalItem>().BloodFiend) ||
                (player.HasBuff<BuffsMagicSachet>() && MagicSachetProj.appliedBloodFiend) ||
                (player.HasBuff<BuffsFlyingsword>() && FlyingswordProj.appliedBloodFiend) ||
                (player.HasBuff<BuffsFlyingGun>() && FlyingGunProj.appliedBloodFiend))
            {
                if (damageDone > 0 && !target.friendly && target.lifeMax > 5)
                {
                    float chance = Main.rand.NextFloat(); // 0~1之间
                    if (chance < 0.8f)
                    {
                        // 80%概率吸血
                        int healAmount = Math.Min(damageDone / 25, 3);
                        if (healAmount > 0)
                        {
                            player.statLife += healAmount;
                            player.HealEffect(healAmount, true);
                        }
                        else
                        {
                            player.statLife += 1;
                            player.HealEffect(1, true);
                        }
                    }
                    else
                    {
                        // 20%概率减少血量
                        int loseAmount = Math.Min(damageDone / 25, 3);
                        player.statLife -= loseAmount;
                        if (loseAmount > 0)
                        {
                            player.statLife -= loseAmount;
                            CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, loseAmount);
                        }
                        else
                        {
                            player.statLife -= 1;
                            CombatText.NewText(player.Hitbox, CombatText.DamagedFriendly, 1);
                        }
                        if (player.statLife <= 0)
                            player.KillMe(Language.ActiveCulture.Name == "zh-Hans" ? PlayerDeathReason.ByCustomReason($"{player.name}未能熬过的血煞洗礼！") :
                            PlayerDeathReason.ByCustomReason($"The baptism of blood that {player.name} have not survived."), 9999, 0);
                    }
                }
            }
        }
    }
    public class Awaken : SwordMasteryPrefix//觉醒
    {
        // 覆盖力量值
        public override float Power => 1f;
        // 物品类别
        public override PrefixCategory Category => PrefixCategory.Melee;
        // 可选：自定义出现概率
        public override float RollChance(Item item) => 0.8f;

        // 可选：自定义属性修改
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1f + 0.21f; // 伤害提升更多
            knockbackMult *= 1.21f;// 额外击退
            useTimeMult *= 1f - 0.16f;//攻速
            scaleMult *= 1f + 0.16f;//大小
            shootSpeedMult *= 1f + 0.16f;//弹幕速度
            critBonus = 12;
        }
    }
    public class Awaken_2 : SwordMasteryPrefix//觉醒
    {
        // 覆盖力量值
        public override float Power => 1f;
        // 物品类别
        public override PrefixCategory Category => PrefixCategory.Magic;
        // 可选：自定义出现概率
        public override float RollChance(Item item) => 0.8f;

        // 可选：自定义属性修改
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1f + 0.21f; // 伤害提升更多
            knockbackMult *= 1.21f;// 额外击退
            useTimeMult *= 1f - 0.16f;//攻速
            shootSpeedMult *= 1f + 0.16f;//弹幕速度
            manaMult *= 0.84f;
            critBonus = 12;
        }
    }
    public class Awaken_3 : SwordMasteryPrefix//觉醒
    {
        // 覆盖力量值
        public override float Power => 1f;
        // 物品类别
        public override PrefixCategory Category => PrefixCategory.Ranged;
        // 可选：自定义出现概率
        public override float RollChance(Item item) => 0.8f;

        // 可选：自定义属性修改
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1f + 0.22f; // 伤害提升更多
            knockbackMult *= 1.18f;// 额外击退
            useTimeMult *= 1f - 0.20f;//攻速
            scaleMult *= 1f + 0.04f;//大小
            shootSpeedMult *= 1f + 0.16f;//弹幕速度
            critBonus = 12;
        }
    }
    public class UltimateFinisher : SwordMasteryPrefix//王牌
    {
        // 覆盖力量值
        public override float Power => 0f;

        // 可选：自定义出现概率
        public override float RollChance(Item item) => 0.8f;

        public override void ApplyAccessoryEffects(Player player)
        {
            base.ApplyAccessoryEffects(player);
        }
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1f + 1f;
        }
        // 自定义描述颜色
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            // 使用正弦函数实现平滑渐变
            float t = (Main.GameUpdateCount % 240) / 240f;
            float progress = (MathF.Sin(t * MathF.PI * 2) + 0.2f) / 2f; // 0~1循环

            Color gradientColor = Color.Lerp(new Color(244, 119, 221), Color.Transparent, progress);

            yield return new TooltipLine(Mod, "PrefixWeaponAwesomeDescription", AdditionalTooltip.Value)
            {
                OverrideColor = gradientColor,
            };
        }
    }
    //王牌实现
    public class UltimateFinisherPlayer : ModPlayer
    {
        public bool ultimatefinisher = false;
        public Item ultimatefinisherItem = null;

        public override void UpdateEquips()
        {
            if (Player.HeldItem.IsAir || Main.mouseItem.prefix == ModContent.PrefixType<UltimateFinisher>())
            {
                ultimatefinisher = false;
                ultimatefinisherItem = null;
            }
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (ultimatefinisher && ultimatefinisherItem != null && !ultimatefinisherItem.IsAir)
            {
                // 满血复活
                Player.statLife = Player.statLifeMax2;
                Player.HealEffect(Player.statLifeMax2);

                Player.immune = true;// 玩家无敌
                Player.immuneTime = 60; // 确保无敌时间短于冲刺持续时间

                // 播放音效和粒子
                SoundEngine.PlaySound(new SoundStyle("SwordMastery/Assets/Sounds/Metalstriking"));
                CombatText.NewText(new Rectangle((int)Player.position.X, (int)Player.position.Y - 20, Player.width, Player.height), new Color(244, 119, 221), 
                    Language.ActiveCulture.Name == "zh-Hans" ? "你使用了【王牌】！": "You used <UltimateFinisher>!", true, false);
                for (int i = 0; i < 60; i++)
                    Dust.NewDust(Player.position, Player.width, Player.height, DustID.MagicMirror, Scale: 1.5f);
                // 移除前缀并变为碎裂
                if(ultimatefinisherItem.CanApplyPrefix(39))
                    ultimatefinisherItem.Prefix(39);
                
                else if(ultimatefinisherItem.CanApplyPrefix(40))
                    ultimatefinisherItem.Prefix(40);
                else 
                    ultimatefinisherItem.Prefix(41);
                
                // 清除标记
                ultimatefinisher = false;
                ultimatefinisherItem = null;

                return false; // 阻止死亡
            }
            return true;
        }
    }
    public class DivineBlessing : SwordMasteryPrefix // 神佑
    {
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.8f;

        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1f + 0.6f;
        }
        // 自定义描述颜色
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            int def = Math.Clamp(item.damage / 10, 1, 20);
            string text = Language.ActiveCulture.Name == "zh-Hans" ? $"+{def}防御力": $" {def} defense";
            yield return new TooltipLine(Mod, "PrefixWeaponAwesomeDescription", Language.ActiveCulture.Name == "zh-Hans" ? "防御力:伤害/10(Min:1,Max:20)": "Defense: Damage/10(Min:1,Max:20)")
            {
                IsModifier = true
            };
            yield return new TooltipLine(Mod, "PrefixDivineBlessingDefense", text)
            {
                IsModifier = true
            };
        }
    }
    public class Infinity : SwordMasteryPrefix // 无限
    {
        public override PrefixCategory Category => PrefixCategory.Ranged;
        public override float Power => 0f;
        public override float RollChance(Item item) => 1.2f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1.5f;
        }
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            yield return new TooltipLine(Mod, "PrefixInfinity", Language.ActiveCulture.Name == "zh-Hans" ? "99%概率不消耗弹药": "99% chance of not consuming ammo")
            {
                IsModifier = true
            };
        }
    }
    public class Infinity_2 : SwordMasteryPrefix // 无限
    {
        public override PrefixCategory Category => PrefixCategory.Magic;
        public override float Power => 0f;
        public override float RollChance(Item item) => 1.2f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 0.8f;
        }
        // 关键：法力消耗减少99%
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            manaMult *= 0.01f; // 法力消耗仅为原来的1%
        }
    }
    public class Titan : SwordMasteryPrefix // 泰坦
    {
        public override PrefixCategory Category => PrefixCategory.Melee;
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.88f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1f;
        }
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 1.48f;
            knockbackMult *= 1.24f;
            useTimeMult *= 1.8f;
            scaleMult *= 2.56f;
        }
    }
    public class InchFlash : SwordMasteryPrefix // 寸闪
    {
        public override PrefixCategory Category => PrefixCategory.AnyWeapon;
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.88f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1f;
        }
        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult *= 0.52f;
            knockbackMult *= 0.82f;
            useTimeMult *= 0.36f;
            scaleMult *= 0.42f;
        }
    }
    public class PrimeCall : SwordMasteryPrefix // 契约
    {
        public override PrefixCategory Category => PrefixCategory.Accessory;
        public override float Power => 0f;
        public override float RollChance(Item item) => 1f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1.2f;
        }
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            string text = Language.ActiveCulture.Name == "zh-Hans"
                ? "+1 召唤栏\n+2% 鞭攻速"
                : "+1 Max Minions\n+2% Whip attack Speed";
            yield return new TooltipLine(Mod, "PrefixCallMarksMinion", text)
            {
                IsModifier = true
            };
        }
    }
    public class DualSoul : SwordMasteryPrefix // 号令
    {
        public override PrefixCategory Category => PrefixCategory.Accessory;
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.8f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1.2f;
        }
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            string text = Language.ActiveCulture.Name == "zh-Hans"
                ? "+1 召唤栏\n+2% 鞭范围"
                : "+1 Max Minions\n+2% Whip Range";
            yield return new TooltipLine(Mod, "PrefixCallMarksMinion", text)
            {
                IsModifier = true
            };
        }
    }
    public class PrimeCall_ : SwordMasteryPrefix // 初灵
    {
        public override PrefixCategory Category => PrefixCategory.Accessory;
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.8f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 0.9f;
        }
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            string text = Language.ActiveCulture.Name == "zh-Hans"
                ? "+1 召唤栏"
                : "+1 Max Minions";
            yield return new TooltipLine(Mod, "PrefixCallMarksMinion", text)
            {
                IsModifier = true
            };
        }
        public override bool CanRoll(Item item)
        {
            var config = ModContent.GetInstance<SwordMasteryConfig>();
            // 只有开启配置时才允许出现
            return config.EnableExtraAffix;
        }
    }
    public class DualSoul_ : SwordMasteryPrefix // 双魂
    {
        public override PrefixCategory Category => PrefixCategory.Accessory;
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.8f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1.0f;
        }
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            string text = Language.ActiveCulture.Name == "zh-Hans"
                ? "+2 召唤栏"
                : "+2 Max Minions";
            yield return new TooltipLine(Mod, "PrefixCallMarksMinion", text)
            {
                IsModifier = true
            };
        }
        public override bool CanRoll(Item item)
        {
            var config = ModContent.GetInstance<SwordMasteryConfig>();
            // 只有开启配置时才允许出现
            return config.EnableExtraAffix;
        }
    }
    public class TriBond : SwordMasteryPrefix // 叁契
    {
        public override PrefixCategory Category => PrefixCategory.Accessory;
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.8f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1.1f;
        }
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            string text = Language.ActiveCulture.Name == "zh-Hans"
                ? "+3 召唤栏"
                : "+3 Max Minions";
            yield return new TooltipLine(Mod, "PrefixCallMarksMinion", text)
            {
                IsModifier = true
            };
        }
        public override bool CanRoll(Item item)
        {
            var config = ModContent.GetInstance<SwordMasteryConfig>();
            // 只有开启配置时才允许出现
            return config.EnableExtraAffix;
        }
    }
    public class QuadHost : SwordMasteryPrefix // 肆御
    {
        public override PrefixCategory Category => PrefixCategory.Accessory;
        public override float Power => 0f;
        public override float RollChance(Item item) => 0.8f;
        public override void ModifyValue(ref float valueMult)
        {
            valueMult *= 1.2f;
        }
        public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
        {
            string text = Language.ActiveCulture.Name == "zh-Hans"
                ? "+4 召唤栏"
                : "+4 Max Minions";
            yield return new TooltipLine(Mod, "PrefixCallMarksMinion", text)
            {
                IsModifier = true
            };
        }
        public override bool CanRoll(Item item)
        {
            var config = ModContent.GetInstance<SwordMasteryConfig>();
            // 只有开启配置时才允许出现
            return config.EnableExtraAffix;
        }
    }
}
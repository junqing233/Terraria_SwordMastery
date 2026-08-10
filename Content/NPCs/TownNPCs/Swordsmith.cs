using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using SwordMastery.Content.Items.FlyingSword.AGlobalControl;
using SwordMastery.Content.Items.FlyingSword.BladeForge;
using SwordMastery.Content.Items.Weapons.Sword;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Security;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Biomes;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using DesertBiome = Terraria.GameContent.Personalities.DesertBiome;

namespace SwordMastery.Content.NPCs.TownNPCs
{
    [AutoloadHead]
    public class Swordsmith : ModNPC
    {
        private static Profiles.StackedNPCProfile NPCProfile;
        private static int ShimmerHeadIndex;
        public override void Load()
        {
            // 将我们的 Shimmer 头部添加到 NPCHeadLoader。
            ShimmerHeadIndex = Mod.AddNPCHeadTexture(Type, Texture + "_Shimmer_Head");
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 26; // NPC 的帧数

            NPCID.Sets.ExtraFramesCount[Type] = 9; // 通常适用于城镇 NPC，但这表示 NPC 可以执行额外的行为，比如坐在椅子上和与其他 NPC 交谈。
            NPCID.Sets.AttackFrameCount[Type] = 5;// 通常适用于城镇 NPC，但这表示 NPC 可以执行额外的行为，比如攻击。
            NPCID.Sets.DangerDetectRange[Type] = 50; // NPC 尝试攻击敌人时，从 NPC 中心开始的像素距离。
            NPCID.Sets.PrettySafe[Type] = 300;
            NPCID.Sets.AttackType[Type] = 3;  // 城镇 NPC 执行的攻击类型。0 = 投掷，1 = 射击，2 = 魔法，3 = 近战
            NPCID.Sets.AttackTime[Type] = 10; // NPC 攻击动画开始后需要的时间。
            NPCID.Sets.AttackAverageChance[Type] = 1;
            NPCID.Sets.HatOffsetY[Type] = -4; // 当派对激活时，派对帽在 Y 轴的偏移量。
            NPCID.Sets.ShimmerTownTransform[NPC.type] = true; // 该设置表示城镇 NPC 有一个 Shimmered 形式，否则当接触 Shimmer 时，城镇 NPC 将像其他敌人一样变得透明。

            // 此设置条目是该 NPC 最重要的部分。由于它为真，它告诉游戏我们希望这个 NPC 表现得像一个城镇 NPC，但实际上不是。
            // 这意味着：该 NPC 将具有城镇 NPC 的 AI，像城镇 NPC 一样攻击，并拥有商店（或您想要的其他额外功能）。
            // 然而，该 NPC 不会在地图上显示头部，在没有玩家附近或世界关闭时会消失，并且将像其他 NPC 一样生成。
            //NPCID.Sets.ActsLikeTownNPC[Type] = true;

            // 这防止幸福按钮
            //NPCID.Sets.NoTownNPCHappiness[Type] = true;

            // 重申一下，由于这个 NPC 技术上不是一个城镇 NPC，我们需要告诉游戏我们仍然希望这个 NPC 生成时具有自定义/随机化的名称。
            // 为此，我们只需让这个钩子返回 true，这将使游戏在生成 NPC 时调用 TownNPCName 方法来确定 NPC 的名称。
            NPCID.Sets.SpawnsWithCustomName[Type] = true;

            // 将此 NPC 与自定义表情连接。
            // 这使得当 NPC 在世界中时，其他 NPC 会 “谈论他”。
            NPCID.Sets.FaceEmote[Type] = NPCID.Sets.FaceEmote[NPCID.Guide];// 让 NPC 表情与 Guide 相同。

            // 原版骨商无法与门互动（打开或关闭它们），但如果您希望您的 NPC 在这方面能够互动，
            // 请取消注释下面这一行。
            NPCID.Sets.AllowDoorInteraction[Type] = true;// 允许 NPC 与门互动

            // 影响 NPC 在图鉴中的外观
            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Velocity = 1f, // 在图鉴中将 NPC 绘制为看起来向 x 方向移动 +1 个单位
                Direction = -1 // -1 为左，1 为右。NPC 默认朝左绘制，但 ExamplePerson 将朝右绘制
            };

            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);

            NPC.Happiness
                .SetBiomeAffection<ForestBiome>(AffectionLevel.Like) // Example Person 喜欢森林。
                .SetBiomeAffection<DesertBiome>(AffectionLevel.Dislike) // Example Person 不喜欢沙漠。
                .SetNPCAffection(550, AffectionLevel.Love) // 爱与酒馆邻居一起生活。
                .SetNPCAffection(NPCID.Guide, AffectionLevel.Like) // 喜欢与向导邻居一起生活。
                .SetNPCAffection(441, AffectionLevel.Dislike) // 不喜欢与税收官邻居一起生活。
                .SetNPCAffection(NPCID.Merchant, AffectionLevel.Dislike) // 讨厌与商人一起生活。
                .SetNPCAffection(369, AffectionLevel.Hate);// 不喜欢与渔夫一起生活

            // 创建ExamplePerson的“档案”，允许在派对和/或Shimmer状态下使用不同贴图。
            NPCProfile = new Profiles.StackedNPCProfile(
                new Profiles.DefaultNPCProfile(Texture, NPCHeadLoader.GetHeadSlot(HeadTexture), Texture),
                new Profiles.DefaultNPCProfile(Texture + "_Shimmer", ShimmerHeadIndex, Texture + "_Shimmer")
            );
        }
        public override ITownNPCProfile TownNPCProfile()
        {
            return NPCProfile;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<MasterSword>(), 4, 1, 1));
        }
        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true; // NPC 不会攻击玩家
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 15;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;

            AnimationType = NPCID.Guide; // 动画类型
        }

        // 确保允许 NPC 聊天，因为“像一个城镇 NPC”并不会自动允许聊天。
        public override bool CanChat()
        {
            return true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // 我们可以使用 AddRange 来一次性添加多个元素，而不是调用 Add 多次
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                // 设置在图鉴中列出的此城镇 NPC 的首选生物群落。
                // 对于城镇 NPC，您通常将此设置为他们最喜欢的生物群落。
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,

                // 在图鉴中设置 NPC 的风格文本。
                new FlavorTextBestiaryInfoElement("铸剑师"),

                // 您可以添加多个元素如果您想这样做
                // 您还可以使用本地化键（请参见 Localization/en-US.lang）
                //new FlavorTextBestiaryInfoElement("Mods.ExampleMod.Bestiary.ExampleBoneMerchant")
            });
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // 此代码在图鉴中缓慢旋转 NPC
            // （简单检查 NPC.IsABestiaryIconDummy 和递增 NPC.Rotation 在这里将不起作用，因为将被每次产生的 drawModifiers.Rotation 重写）
            if (NPCID.Sets.NPCBestiaryDrawOffset.TryGetValue(Type, out NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers))// 如果存在 NPCBestiaryDrawModifiers
            {
                //drawModifiers.Rotation += 0.001f;
                drawModifiers.Position = new Vector2(drawModifiers.Position.X, drawModifiers.Position.Y);

                // 用调整后的旋转替换现有的 NPCBestiaryDrawModifiers
                NPCID.Sets.NPCBestiaryDrawOffset.Remove(Type);// 移除旧的 NPCBestiaryDrawModifiers
                NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifiers);// 添加新的 NPCBestiaryDrawModifiers
            }

            return true;
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            int num = NPC.life > 0 ? 1 : 5;

            for (int k = 0; k < num; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RedMoss);
            }

            // NPC死亡时生成血块
            if (Main.netMode != NetmodeID.Server && NPC.life <= 0)
            {
                string variant = "";
                if (NPC.IsShimmerVariant)
                    variant += "_Shimmer";
                // 获取血块类型。该NPC有Shimmer和派对形态的头、手、腿血块（共12种）
                int hatGore = NPC.GetPartyHatGore();
                int headGore = Mod.Find<ModGore>($"{Name}_Gore{variant}_Head").Type;
                int armGore = Mod.Find<ModGore>($"{Name}_Gore_Arm").Type;
                int legGore = Mod.Find<ModGore>($"{Name}_Gore_Leg").Type;

                // 生成血块。手和腿的位置下移以更自然
                if (hatGore > 0)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, hatGore);
                }
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, headGore, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 20), NPC.velocity, armGore);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 20), NPC.velocity, armGore);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 34), NPC.velocity, legGore);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position + new Vector2(0, 34), NPC.velocity, legGore);
            }
        }

        public override List<string> SetNPCNameList()// 自定义 NPC 名称
        {
            return new List<string> {
                "铁铸"
            };
        }

        //public override void OnSpawn(IEntitySource source)
        //{
        //    if (source is EntitySource_SpawnNPC)
        //    {
        //        // 当城镇 NPC 成功生成到世界中时将其“解锁”。
        //        TownNPCRespawnSystem.unlockedExamplePersonSpawn = true;

        //    }
        //}

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            //foreach (var player in Main.ActivePlayers)
            //{
            //    // 玩家必须拥有 ExampleItem 或 ExampleBlock 中的任意一种才能生成 NPC
            //    if (player.inventory.Any(item => item.type == ModContent.ItemType<BladeForge>()))
            //    {
            //        //GotItems_1 = false;
            //        return true;
            //        //return player.inventory.Any(item => item.type == ModContent.ItemType<FirstSword>());
            //    }
            //}

            return true;
        }


        public override string GetChat()
        {
            int guide = NPC.FindFirstNPC(NPCID.Guide);
            int fisherman = NPC.FindFirstNPC(369);

            WeightedRandom<string> chat = new WeightedRandom<string>();
            if (NPC.homeless)// 无家可归
            {
                chat.Add(Language.GetTextValue("小子，你是谁？"));
            }
            if (Main.bloodMoon)// 血月
            {
                chat.Add(Language.GetTextValue("小子，我应与你并肩作战！"));
                chat.Add(Language.GetTextValue("血月？不祥……"));
            }
            if (guide >= 0 && Main.rand.NextBool(4))// 向导
            {
                chat.Add(Language.GetTextValue($"我也许应该跟{Main.npc[guide].GivenName}谈一谈", Main.npc[guide].GivenName));
            }
            if (fisherman >= 0)// 渔夫
            {
                chat.Add(Language.GetTextValue($"{Main.npc[fisherman].GivenName}就纯是个畜生！我有时候真想一剑砍死它！", Main.npc[fisherman].GivenName));
            }
            if (BirthdayParty.PartyIsUp)// 生日派对
            {
                chat.Add(Language.GetTextValue("庆祝之余，也别忘了练剑"));
                chat.Add(Language.GetTextValue("去领块蛋糕吧，小子，然后大吃一顿，哈哈！"));
            }
            if (Main.eclipse)// 日食
            {
                chat.Add(Language.GetTextValue("天狗食日？"));
                chat.Add(Language.GetTextValue("怪物的数量太多了，需不需要我出手？"));
            }
            // 这些是 NPC 在与您交谈时可能会告诉您的内容。
            chat.Add(Language.GetTextValue("小子，你又变强了？"));
            chat.Add(Language.GetTextValue("魔石？要得到它需要把永夜御刃分解掉才行"));
            chat.Add(Language.GetTextValue("也许你可以把龙葵巫毒娃娃扔进岩浆里去"));
            chat.Add(Language.GetTextValue("兔子？挺……挺萌的……"));
            chat.Add(Language.GetTextValue("哈哈，当然，我很乐意帮你铸剑"));
            chat.Add(Language.GetTextValue("我只是一个普普通通的铸剑师"));
            chat.Add(Language.GetTextValue("记住，剑之一道，只追求一个“极”字"));
            chat.Add(Language.GetTextValue("漫天灯笼的夜晚很美，呵呵，不知道她怎么样了？"), 0.1);
            if (Main.rainTime > 0.0f)// 雨天
            {
                chat.Add(Language.GetTextValue("丝歌千洗，碧空万里……嗯？别这么看着我"));
                chat.Add(Language.GetTextValue("传闻有人在大雨之中练剑，领悟出了一种特殊剑法，不知是真是假……"));
            }
            if (Main.time >= 0.9f && Main.time <= 0.95f)
            {
                chat.Add(Language.GetTextValue("还在练剑吗？熬坏了身体可不好"));
            }
            if (Main.slimeRainTime > 0.0f)// 史莱姆雨
            {
                chat.Add(Language.GetTextValue("史莱姆雨？这个世界可真有意思"));
            }
            if (Main.hardMode)
            {
                chat.Add(Language.GetTextValue("我感受到现在这个世界的怪物异常凶残，不过，哼哼，我仍可一剑斩之"));
            }
            chat.Add(Language.GetTextValue("唉……不知不觉这么多年过去了，你还在怨我吗？"));
            string chosenChat = chat; // chat 被隐式转换为字符串。这是进行随机选择的地方。

           
            return chosenChat;
        }
        //public override void LoadData(TagCompound tag)// 加载数据
        //{
        //    NumberOfTimesTalkedTo = tag.GetInt("numberOfTimesTalkedTo");// 加载数据
        //}

        //public override void SaveData(TagCompound tag)// 保存数据
        //{
        //    tag["numberOfTimesTalkedTo"] = NumberOfTimesTalkedTo;// 保存数据
        //}


        private static int lastBlessingTime = 0; // 记录上一次成功施加祝福的时间

        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            if (firstButton) // 打开商店
            {
                shop = "商店";
            }
        }


        public override void OnCaughtBy(Player player, Item item, bool failed)// 自定义被捕效果
        {
            base.OnCaughtBy(player, item, failed);
        }
        public override void SetChatButtons(ref string button, ref string button2)
        {
            // 打开聊天 UI 时的聊天按钮是什么
            button = Language.GetTextValue("LegacyInterface.28");
        }


        public override void AddShops()
        {
            var npcShop = new NPCShop(Type, "商店")
            //.Add(new Item(ModContent.ItemType<Flyingsword>()) { shopCustomPrice = Item.buyPrice(gold: 10) })
            .Add(item: ItemID.AmmoBox, Condition.TimeNight)
            .Add(item: ItemID.IronHammer)//铁锤
            .Add(item: ItemID.LeadHammer)//铅锤
            .Add(item: ItemID.Pwnhammer, Condition.Hardmode)//神锤
            .Add(item: ItemID.IronAnvil)//铁砧
            .Add(item: ItemID.LeadAnvil)//铅砧
            .Add(item: ItemID.Furnace)//熔炉
            ;
            npcShop.Register(); // 此商店选项卡的名称
        }

        public override void ModifyActiveShop(string shopName, Item[] items)
        {
            foreach (Item item in items)
            {
                // 跳过 'air' 物品和 null 物品。
                if (item == null || item.type == ItemID.None)
                {
                    continue;
                }
            }


            //if (Main.bloodMoon) // 血月
            //{
            //    // 查找第一个空的槽位
            //    for (int i = 0; i < items.Length; i++)
            //    {
            //        if (items[i] == null || items[i].type == ItemID.None)
            //        {
            //            items[i] = new Item(ModContent.ItemType<RainStaff>()) { shopCustomPrice = Item.buyPrice(gold: 100) };
            //            break; // 退出循环
            //        }
            //    }
            //}
            //if (Main.raining)//下雨
            //{
            //    // 查找第一个空的槽位
            //    for (int i = 0; i < items.Length; i++)
            //    {
            //        if (items[i] == null || items[i].type == ItemID.None)
            //        {
            //            items[i] = new Item(ModContent.ItemType<RainStaff>()) { shopCustomPrice = Item.buyPrice(gold: 10) };
            //            break; // 退出循环
            //        }
            //    }
            //    for (int i = 0; i < items.Length; i++)
            //    {
            //        if (items[i] == null || items[i].type == ItemID.None)
            //        {
            //            items[i] = new Item(ModContent.ItemType<RainStaff1>()) { shopCustomPrice = Item.buyPrice(gold: 10) };
            //            break; // 退出循环
            //        }
            //    }
            //}
            //if (!Main.dayTime) // 检查白天
            //{
            //    // 查找第一个空的槽位
            //    for (int i = 0; i < items.Length; i++)
            //    {
            //        if (items[i] == null || items[i].type == ItemID.None)
            //        {
            //            items[i] = new Item(ItemID.AmmoBox) { shopCustomPrice = Item.buyPrice(gold: 100) };
            //            break; // 退出循环
            //        }
            //    }
            //}
        }

        public override bool CanGoToStatue(bool toKingStatue)
        {
            return toKingStatue;
        }
        public override void OnGoToStatue(bool toKingStatue)
        {
            if (toKingStatue)
            {
                Main.NewText("铸剑师受到了国王雕像的召唤！！");
            }
        }
        public override void TownNPCAttackStrength(ref int damage, ref float knockback)// 自定义攻击属性
        {
            damage = NPC.damage;
            knockback = 6f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)// 自定义攻击冷却
        {
            cooldown = 15;
            randExtraCooldown = 2;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
            itemWidth = 38;
            itemHeight = 38;
        }

        public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize, ref float scale, ref Vector2 offset) // 自定义武器绘制
        {
            int itemType = ModContent.ItemType<MasterSword>();
            Main.GetItemDrawFrame(itemType, out item, out itemFrame);
        }
    }
}

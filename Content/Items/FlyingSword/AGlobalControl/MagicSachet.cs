using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SwordMastery.Content.GlobaProjectiles;
using SwordMastery.Content.Items.FlyingSword.Glaive;
using SwordMastery.Content.Items.FlyingSword.Glaive_H;
using SwordMastery.Content.Prefixes;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace SwordMastery.Content.Items.FlyingSword.AGlobalControl
{
    public class MagicSachetCrateDrop : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            // 木匣ID：ItemID.WoodenCrate, ItemID.WoodenCrateHard
            if (item.type == ItemID.WoodenCrate || item.type == ItemID.WoodenCrateHard)
            {
                // 3%概率掉落 MagicSachet
                itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<MagicSachet>(), 33));
            }
        }
    }
    // 1. 物品类
    public class MagicSachet : ModItem
    {
        // 存储选中的武器类型
        public static int MaxItems = 1;
        public Item[] items = Enumerable.Range(0, MaxItems).Select(_ => new Item()).ToArray();
        public static float ExtraDrawRotation = 0f; // 额外绘制旋转角度（单位：弧度）
        public static bool IgnoreTilesForTargeting = false; // 是否索敌穿墙
        public static bool IsTail = false; // 是否拖尾
        public static float AttackSpread = MathHelper.ToRadians(180f); // 默认180度
        public bool isClick = false;
        public static bool IsClick = false;
        internal static MagicSachetWeaponSlotUI weaponSlotUI;
        public Guid InstanceId = Guid.NewGuid(); // 每个物品唯一
        
        // 存档支持
        // 物品创建时初始化物品数组
        public override void OnCreated(ItemCreationContext context)
        {
            InstanceId = Guid.NewGuid(); // 保证每次新建都唯一
            InitializeItems();// 初始化物品数组
        }
        public override void SaveData(TagCompound tag)
        {
            var savedItems = items.Where(item => item != null).Select(item => ItemIO.Save(item)).ToList();
            var savedTextures = items.Where(item => item != null).Select(item => item.type).ToList();

            tag["items"] = savedItems;
            tag["textures"] = savedTextures;
        }

        public override void LoadData(TagCompound tag)
        {
            if (items == null || items.Length != MaxItems)
                items = new Item[MaxItems];
            for (int i = 0; i < MaxItems; i++)
                items[i] = new Item();

            var loadedItems = tag.GetList<TagCompound>("items");
            var loadedTextures = tag.GetList<int>("textures");

            for (int i = 0; i < loadedItems.Count && i < items.Length; i++)
            {
                items[i] = ItemIO.Load(loadedItems[i]);
                //FlyingSwordWeaponSlotUI.ItemID = items[i].type;
                // 确保物品纹理已加载
                if (items[i] != null && !items[i].IsAir)
                {
                    if (i < loadedTextures.Count)
                    {
                        int itemType = loadedTextures[i];
                        if (itemType >= 0 && itemType < TextureAssets.Item.Length)
                        {
                            if (!TextureAssets.Item[itemType].IsLoaded)
                            {
                                TextureAssets.Item[itemType] = ModContent.Request<Texture2D>($"Terraria/Images/Item_{itemType}");
                            }
                        }
                    }
                }
            }
        }
        
        // 初始化物品数组
        private void InitializeItems()
        {
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = new Item();
                items[i].SetDefaults(ItemID.None);
            }
        }

        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.mana = 2;
            Item.width = 28;
            Item.height = 28;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = 20000;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item100;
            Item.shoot = ModContent.ProjectileType<MagicSachetProj>();
            Item.shootSpeed = 16f;
            Item.buffType = ModContent.BuffType<BuffsMagicSachet>();
            Item.DamageType = DamageClass.Summon;
        }
        public override bool AllowPrefix(int pre)
        {
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SkyBlueFlower, 1) // 天蓝花朵
                .AddIngredient(ItemID.Silk, 12) // 丝绸
                .AddIngredient(ItemID.PinkPearl, 1) // 粉珍珠
                .AddTile(TileID.WorkBenches)
                .Register();
        }
        private float FinalWeaponDamage = 0;
        private float FinalWeaponKnockback = 0;
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (items[0] != null && !items[0].IsAir && items[0].damage > 0
                && (items[0].DamageType.CountsAsClass(DamageClass.Magic)
                || items[0].DamageType.CountsAsClass(DamageClass.MagicSummonHybrid)
                )
                )
            {
                int useTime = items[0].useTime;
                int minUseTime = 10;
                int maxUseTime = 40;
                float minScale = 0.62f; // 最短时间最小倍率
                float maxScale = 0.82f; // 最长时间最大倍率
                float scale;
                // 获取武器最终伤害（已包含饰品等加成）
                float finalWeaponDamage = player.GetWeaponDamage(items[0]);
                FinalWeaponDamage = finalWeaponDamage;
                // 读取配置并调整伤害
                var config = ModContent.GetInstance<SwordMasteryConfig>();
                if (config.StrengthExperience == StrengthMode.Ordinary)
                {
                    FinalWeaponDamage = (int)(FinalWeaponDamage * 0.6f);
                }
                if (useTime <= minUseTime)
                    scale = minScale;
                else if (useTime >= maxUseTime)
                    scale = maxScale;
                else
                    scale = minScale + (maxScale - minScale) * ((float)(useTime - minUseTime) / (maxUseTime - minUseTime));
                damage *= scale * finalWeaponDamage / Item.damage;
            }
            else
            {
                damage *= 0f;
            }
        }
        public override void ModifyWeaponKnockback(Player player, ref StatModifier knockback)
        {
            if (items[0] != null && !items[0].IsAir && items[0].knockBack > 0
                && (items[0].DamageType.CountsAsClass(DamageClass.Magic)
                || items[0].DamageType.CountsAsClass(DamageClass.MagicSummonHybrid)
                )
                )
            {
                FinalWeaponKnockback = player.GetWeaponKnockback(items[0]);
                // 近似一半（注意：StatModifier只能乘法，不能直接赋值）
                knockback *= 0.5f * FinalWeaponKnockback / Item.knockBack;
            }
            else
            {
                knockback *= 0f;
            }
        }
        public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
        {
            float G = 0;
            // 伤害行处理
            var damageLine = tooltips.FirstOrDefault(t => t.Name == "Damage" && t.Mod == "Terraria");
            if (damageLine != null)
            {
                int showDamage = 0;
                if (items[0] != null && !items[0].IsAir && items[0].damage > 0
                    && (items[0].DamageType.CountsAsClass(DamageClass.Magic)
                    || items[0].DamageType.CountsAsClass(DamageClass.MagicSummonHybrid)
                    )
                    )
                {
                    int useTime = items[0].useTime;
                    int minUseTime = 10;
                    int maxUseTime = 40;
                    float minScale = 0.62f;
                    float maxScale = 0.82f;
                    float scale;
                    if (useTime <= minUseTime)
                        scale = minScale;
                    else if (useTime >= maxUseTime)
                        scale = maxScale;
                    else
                        scale = minScale + (maxScale - minScale) * ((float)(useTime - minUseTime) / (maxUseTime - minUseTime));
                    G = scale;
                    showDamage = (int)(FinalWeaponDamage * scale);
                }
                string[] split = damageLine.Text.Split(' ');
                string suffix = split.Length > 1 ? string.Join(' ', split.Skip(1)) : "伤害";
                damageLine.Text = $"{showDamage} {suffix}";
            }

            // 击退行处理
            var knockbackLine = tooltips.FirstOrDefault(t => t.Name == "Knockback" && t.Mod == "Terraria");
            if (knockbackLine != null)
            {
                float showKnockback = 0f;
                if (items[0] != null && !items[0].IsAir && items[0].knockBack > 0
                    && (items[0].DamageType.CountsAsClass(DamageClass.Magic)
                    || items[0].DamageType.CountsAsClass(DamageClass.MagicSummonHybrid))
                    )
                {
                    showKnockback = FinalWeaponKnockback / 2f;
                }
                // 获取原始击退描述的后缀（如“击退”）
                string[] split = knockbackLine.Text.Split(' ');
                string suffix = split.Length > 1 ? string.Join(' ', split.Skip(1)) : "击退";
                knockbackLine.Text = $"{showKnockback:0.##} {suffix}";
                // 在击退行后插入“发射攻速”
                int insertIndex = tooltips.IndexOf(knockbackLine) + 1;
                int useTime = items[0] != null && !items[0].IsAir ? items[0].useTime : 0;
                double seconds = useTime / 60.0;
                var speedLine = new TooltipLine(Mod, "ShootSpeed", Language.ActiveCulture.Name == "zh-Hans" ? $"射滞: {seconds:0.00}秒" : $"Shoot Lull: {seconds:0.00} s");
                tooltips.Insert(insertIndex, speedLine);
                // 插入魔力消耗
                int manaCost = items[0] != null && !items[0].IsAir ? (int)(items[0].mana * G) : 0;
                var manaLine = new TooltipLine(Mod, "ManaCost", Language.ActiveCulture.Name == "zh-Hans" ? $"魔力消耗: {manaCost}" : $"Mana Cost: {manaCost}");
                tooltips.Insert(insertIndex + 1, manaLine);
            }
            // 物品名后拼接物品槽武器名
            var nameLine = tooltips.FirstOrDefault(t => t.Name == "ItemName" && t.Mod == "Terraria");
            if (nameLine != null && items[0] != null && !items[0].IsAir)
            {
                nameLine.Text += $"({items[0].Name})";
            }else
            {
                nameLine.Text += $"(空)";
            }
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                weaponSlotUI = new MagicSachetWeaponSlotUI();
            }
        }
        public override bool CanRightClick()
        {
            if (Main.mouseRight && !isClick)
            {
                if (Main.mouseRightRelease)
                {
                    if (Main.netMode != NetmodeID.Server)
                    {
                        //Main.NewText("1");
                        IsClick = !IsClick;
                        if (IsClick)
                        {
                            ModContent.GetInstance<MagicSachetUISystem>().ShowWeaponSlotUI(this);
                            SoundEngine.PlaySound(SoundID.MenuOpen); // 播放打开音效
                        }
                        else
                        {
                            ModContent.GetInstance<MagicSachetUISystem>().HideWeaponSlotUI();
                            SoundEngine.PlaySound(SoundID.MenuClose); // 播放关闭音效
                        }
                    }
                }
            }
            isClick = Main.mouseRightRelease;
            return false;
        }
        // 没有武器时无法使用
        public override bool CanUseItem(Player player)
        {
            //return FlyingSwordWeaponSlotUI.ItemID > 0;
            return items[0] != null && !items[0].IsAir;
        }
        private Texture2D tex = null;
        private Texture2D weaponTex = null;
        // 物品图标右下角绘制
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            tex = TextureAssets.Item[Type].Value;
            spriteBatch.Draw(tex, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);

            if (items[0] != null && !items[0].IsAir)
            {
                weaponTex = TextureAssets.Item[items[0].type].Value;
                Rectangle weaponFrame = Main.itemAnimations[items[0].type]?.GetFrame(weaponTex) ?? weaponTex.Frame();
                Vector2 offset = new Vector2(10, 10) * scale;
                spriteBatch.Draw(weaponTex, position + offset, weaponFrame, Color.White, 0f, origin, scale * 0.5f, SpriteEffects.None, 0f);
            }
            return false;
        }

        // 发射弹幕时传递武器类型
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(ModContent.BuffType<BuffsMagicSachet>(), 3600);
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            if (proj >= 0 && proj < Main.maxProjectiles)
            {
                Main.projectile[proj].originalDamage = damage;
                // 传递唯一标识符（用 ai[1] 或 localAI[0]，或 ModProjectile 字段）
                Main.projectile[proj].ai[1] = items[0].type; // Guid转int
                Main.projectile[proj].localAI[1] = items[0].shoot;
                Main.projectile[proj].localAI[0] = items[0].shootSpeed;
                Main.projectile[proj].ai[0] = items[0].useAnimation;
                Main.projectile[proj].ai[2] = items[0].mana;
                // 存储武器类型到玩家
                player.GetModPlayer<MagicSachetPlayer>().LastSummonWeaponType = items[0].type;
                // 在Shoot方法里
                Main.projectile[proj].localAI[2] = items[0].prefix; // 传递前缀ID
                // 在 Shoot 方法里
                Main.projectile[proj].GetGlobalProjectile<MagicSachetProjGlobal>().OnHitNPCTypeId = items[0].type;
            }
            return false;
        }
    }
    public class MagicSachetProjGlobal : GlobalProjectile
    {
        public int OnHitNPCTypeId;
        public override bool InstancePerEntity => true;
    }
    // 2. UI系统
    public class MagicSachetUISystem : ModSystem
    {
        internal static UserInterface weaponSlotInterface;
        internal static MagicSachetWeaponSlotUI weaponSlotUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                weaponSlotUI = new MagicSachetWeaponSlotUI();
                weaponSlotInterface = new UserInterface();
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            //if (weaponSlotUI.Visible)
                weaponSlotInterface?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(System.Collections.Generic.List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx != -1)
            {
                layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                    "MagicSachet: WeaponSlotUI",
                    () =>
                    {
                        if (/*weaponSlotUI.Visible && */weaponSlotInterface?.CurrentState != null)
                            weaponSlotInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }

        public void ShowWeaponSlotUI(MagicSachet item)
        {
            weaponSlotUI.SetItem(item);
            weaponSlotInterface.SetState(weaponSlotUI);
            weaponSlotUI.Visible = true;
        }
        public void HideWeaponSlotUI()
        {
            weaponSlotInterface.SetState(null);
            weaponSlotUI.Visible = false;
        }
    }

    // 3. UIState实现
    public class MagicSachetWeaponSlotUI : UIState
    {
        private UIItemSlotMagicSachet slot;
        private MagicSachet magicSachet;
        public bool Visible = false;
        public static float ItemID = 0;
        private UITextButtonMagicSachet spreadBtn;
        private UITextButtonMagicSachet rotateButton;
        private UITextButtonMagicSachet ignoreTilesBtn;
        private UITextButtonGun TailButton;

        public override void OnInitialize()
        {
            int offsetX = 0;
            if (FlyingSwordUISystem.weaponSlotUI != null && FlyingSwordUISystem.weaponSlotUI.Visible)
                offsetX += 100;
            if (FlyingGunUISystem.weaponSlotUI != null && FlyingGunUISystem.weaponSlotUI.Visible)
                offsetX += 100;

            
            // 物品槽
            slot = new UIItemSlotMagicSachet(magicSachet?.items, 0);
            slot.Left.Set(600 + offsetX, 0f);
            slot.Top.Set(200, 0f);
            Append(slot);


            // 攻击角度调节按钮
            spreadBtn = new UITextButtonMagicSachet(
                "",
                () => { // 左键：增加角度
                    MagicSachet.AttackSpread += MathHelper.ToRadians(45f);
                    if (MagicSachet.AttackSpread > MathHelper.ToRadians(360f))
                        MagicSachet.AttackSpread = MathHelper.ToRadians(360f);
                },
                () => { // 右键：减少角度
                    MagicSachet.AttackSpread -= MathHelper.ToRadians(45f);
                    if (MagicSachet.AttackSpread < MathHelper.ToRadians(45f))
                        MagicSachet.AttackSpread = MathHelper.ToRadians(45f);
                }
            );
            spreadBtn.Left.Set(586 + offsetX, 0f);
            spreadBtn.Top.Set(260, 0f);
            Append(spreadBtn);


            //索敌按钮
            ignoreTilesBtn = new UITextButtonMagicSachet(
               "",
               () => {
                   MagicSachet.IgnoreTilesForTargeting = !MagicSachet.IgnoreTilesForTargeting;
               }
           );
            ignoreTilesBtn.Left.Set(586 + offsetX, 0f);
            ignoreTilesBtn.Top.Set(300, 0f);
            Append(ignoreTilesBtn);

            // 旋转按钮
            rotateButton = new UITextButtonMagicSachet(
                Language.ActiveCulture.Name == "zh-Hans" ? "旋转武器": "Rotate the weapon",
                () => {
                    MagicSachet.ExtraDrawRotation += MathHelper.ToRadians(45f);
                    if (MagicSachet.ExtraDrawRotation > MathHelper.TwoPi)
                        MagicSachet.ExtraDrawRotation -= MathHelper.TwoPi;
                },
                () => {
                    MagicSachet.ExtraDrawRotation -= MathHelper.ToRadians(45f);
                    if (MagicSachet.ExtraDrawRotation < -MathHelper.TwoPi)
                        MagicSachet.ExtraDrawRotation += MathHelper.TwoPi;
                }
            );
            rotateButton.Left.Set(586 + offsetX, 0f);
            rotateButton.Top.Set(340, 0f);
            Append(rotateButton);

            //拖尾按钮
            TailButton = new UITextButtonGun(
               "",
               () => {
                   MagicSachet.IsTail = !MagicSachet.IsTail;
               }
           );
            TailButton.Left.Set(586 + offsetX, 0f);
            TailButton.Top.Set(380, 0f);
            Append(TailButton);
        }
       
        public void SetItem(MagicSachet magicSachet)
        {
            this.magicSachet = magicSachet;
            if (slot != null)
            {
                RemoveAllChildren();
                OnInitialize();
            }
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            var rectSpread = spreadBtn.GetDimensions().ToRectangle();
            if(spreadBtn.isMouseOver)
            {
                
                string spreadText = Language.ActiveCulture.Name == "zh-Hans" ? $"角度: {MathHelper.ToDegrees(MagicSachet.AttackSpread):0}°": $"Angle: {MathHelper.ToDegrees(MagicSachet.AttackSpread):0}°";
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, spreadText, rectSpread.Center.X - 34, rectSpread.Center.Y - 8, Color.Gold, Color.Black, Vector2.Zero, 0.8f);

            }else
            {
                string spreadText = Language.ActiveCulture.Name == "zh-Hans" ? "攻击覆盖度": "Attack coverage"; 
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, spreadText, rectSpread.Center.X - 38, rectSpread.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }

            // 只有鼠标移入旋转按钮时才绘制提示
            if (rotateButton.isMouseOver)
            {
                var rect_ = rotateButton.GetDimensions().ToRectangle(); 
                string tip = Language.ActiveCulture.Name == "zh-Hans" ? $"旋转:{MathHelper.ToDegrees(MagicSachet.ExtraDrawRotation):0}°": $"Revolve:{MathHelper.ToDegrees(MagicSachet.ExtraDrawRotation):0}°";
                // 右移10像素
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rect_.Center.X - 31, rect_.Center.Y - 8, Color.MediumPurple, Color.Black, Vector2.Zero, 0.8f);
            }
            var rectIgnore = ignoreTilesBtn.GetDimensions().ToRectangle(); 
            if (ignoreTilesBtn.isMouseOver)
            {
                string tip = MagicSachet.IgnoreTilesForTargeting
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Current: Wall Targeting")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Current: Normal Targeting");
                Color color_ = MagicSachet.IgnoreTilesForTargeting
                    ? Color.LightSkyBlue : Color.Red;
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, rectIgnore.Center.X - 24, rectIgnore.Center.Y - 8, color_, Color.Black, Vector2.Zero, 0.8f);
            }
            else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "索敌穿墙" : "Wall Targeting", rectIgnore.Center.X - 30, rectIgnore.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
            //拖尾开关
            var Rect = TailButton.GetDimensions().ToRectangle();
            if (TailButton.isMouseOver)
            {
                string tip = MagicSachet.IsTail
                    ? (Language.ActiveCulture.Name == "zh-Hans" ? "已开启" : "Turned On")
                    : (Language.ActiveCulture.Name == "zh-Hans" ? "已关闭" : "Closed");
                Color color_ = MagicSachet.IsTail
                    ? Color.Goldenrod : Color.Red;
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, tip, Rect.Center.X - 24, Rect.Center.Y - 8, color_, Color.Black, Vector2.Zero, 0.8f);
            }
            else
            {
                Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, Language.ActiveCulture.Name == "zh-Hans" ? "光亮拖尾" : "Brightly Tailed", Rect.Center.X - 30, Rect.Center.Y - 8, Color.White, Color.Black, Vector2.Zero, 0.8f);
            }
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            int offsetX = 0;
            if (FlyingSwordUISystem.weaponSlotUI != null && FlyingSwordUISystem.weaponSlotUI.Visible)
                offsetX += 100;
            if (FlyingGunUISystem.weaponSlotUI != null && FlyingGunUISystem.weaponSlotUI.Visible)
                offsetX += 100;

            slot.Left.Set(600 + offsetX, 0f);
            spreadBtn.Left.Set(586 + offsetX, 0f);
            ignoreTilesBtn.Left.Set(586 + offsetX, 0f);
            rotateButton.Left.Set(586 + offsetX, 0f);
            TailButton.Left.Set(586 + offsetX, 0f);

            if (magicSachet != null) 
            {
                // 只允许近战武器
                if (magicSachet.items[0] != null && !magicSachet.items[0].IsAir && magicSachet.items[0].damage > 0 
                    && (magicSachet.items[0].DamageType == DamageClass.Magic
                    || magicSachet.items[0].DamageType == DamageClass.MagicSummonHybrid
                    )
                    )
                {
                    //flyingsword.selectedWeaponType = flyingsword.items[0].type;
                    ItemID = magicSachet.items[0].type;
                }
                else
                {
                    //flyingsword.selectedWeaponType = 0;
                    ItemID = 0;
                }
            }
            if (slot.isMouseOver)
            {
                Main.LocalPlayer.mouseInterface = true;
            }
            if (!Main.playerInventory)
            {
                ModContent.GetInstance<MagicSachetUISystem>().HideWeaponSlotUI();
                MagicSachet.IsClick = !MagicSachet.IsClick;
            }
        }
    }
    public class UITextButtonMagicSachet : UIElement
    {
        private string text;
        private Action onClick;
        private Action onRightClick;
        public bool isMouseOver = false;


        public UITextButtonMagicSachet(string text, Action onClick, Action onRightClick = null)
        {
            this.text = text;
            this.onClick = onClick;
            this.onRightClick = onRightClick;
            Width.Set(80, 0f);
            Height.Set(32, 0f);
        }
        //public override void RightClick(UIMouseEvent evt)
        //{
        //    base.RightClick(evt);
        //    if (onRightClick != null)
        //        onRightClick.Invoke();
        //    SoundEngine.PlaySound(SoundID.MenuTick);
        //}
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            var rect = GetDimensions().ToRectangle();
            spriteBatch.Draw(TextureAssets.InventoryBack9.Value, rect, Color.White * 0.8f);

            // 仅当不是“旋转武器”按钮，或鼠标未移入时才绘制按钮文字
            if (text != "旋转武器" && text != "Rotate the weapon" || !isMouseOver) 
            {
                int offsetX = -38;
                if (Language.ActiveCulture.Name == "zh-Hans" ? text == "旋转武器": text == "Rotate the weapon")
                    offsetX += 7;

                Utils.DrawBorderStringFourWay(
                    spriteBatch,
                    FontAssets.MouseText.Value,
                    text,
                    rect.Center.X + offsetX,
                    rect.Center.Y - 8,
                    Color.White,
                    Color.Black,
                    Vector2.Zero,
                    0.8f
                );
            }
        }
        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            isMouseOver = true;
            Main.LocalPlayer.mouseInterface = true; // 在物品槽内设置 mouseInterface
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            isMouseOver = false;
        }
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            onClick?.Invoke();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            onRightClick?.Invoke();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void Update(GameTime gameTime)
        {
            if(isMouseOver)
                Main.LocalPlayer.mouseInterface = true; // 在物品槽内设置 mouseInterface
            base.Update(gameTime);
        }
    }
    // UIElement
    public class UIItemSlotMagicSachet : UIElement
    {
        private Item[] items;
        private int index;
        public bool isMouseOver = false;

        public UIItemSlotMagicSachet(Item[] items, int index)
        {
            this.items = items;
            this.index = index;
            Width.Set(52f, 0f);
            Height.Set(52f, 0f);
        }
        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            isMouseOver = true;
            Main.LocalPlayer.mouseInterface = true; // 在物品槽内设置 mouseInterface
            //Main.NewText("2");
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            isMouseOver = false;
        }
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            var player = Main.LocalPlayer;
            // 取出物品
            if (!items[index].IsAir && Main.mouseItem.IsAir && !player.ItemAnimationActive)
            {
                Main.mouseItem = items[index].Clone();
                items[index].TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 放入物品（只允许近战武器）
            else if (items[index].IsAir && !Main.mouseItem.IsAir
                && Main.mouseItem.damage > 0
                && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Magic)
                || Main.mouseItem.DamageType.CountsAsClass(DamageClass.MagicSummonHybrid))
                && !player.ItemAnimationActive
                )
            {
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
            }
            // 交换物品（只允许近战武器）
            else if (!items[index].IsAir && !Main.mouseItem.IsAir
                && Main.mouseItem.damage > 0
                 && (Main.mouseItem.DamageType.CountsAsClass(DamageClass.Magic)
                || Main.mouseItem.DamageType.CountsAsClass(DamageClass.MagicSummonHybrid))
                && !player.ItemAnimationActive
                )
            {
                Item temp = items[index].Clone();
                items[index] = Main.mouseItem.Clone();
                Main.mouseItem = temp;
                SoundEngine.PlaySound(SoundID.Grab);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            //spriteBatch.Draw(TextureAssets.InventoryBack9.Value, GetDimensions().ToRectangle(), Color.White * 0.72f);
            //if (items[index] != null && !items[index].IsAir)
            //{
            //    Texture2D tex = TextureAssets.Item[items[index].type].Value;
            //    float scale = Math.Min(1f, 30f / (tex.Width + tex.Height) * 2);
            //    var frame = Main.itemAnimations[items[index].type]?.GetFrame(tex) ?? tex.Frame();
            //    var drawPosition = GetDimensions().Position() + new Vector2(25f) - frame.Size() * 0.5f * scale;
            //    spriteBatch.Draw(tex, drawPosition, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            //}
            spriteBatch.Draw(TextureAssets.InventoryBack9.Value, GetDimensions().ToRectangle(), Color.White * 0.72f);
            if (items[index] != null && !items[index].IsAir)
            {
                Texture2D tex = TextureAssets.Item[items[index].type].Value;
                Rectangle frame = Main.itemAnimations[items[index].type]?.GetFrame(tex) ?? tex.Frame();
                float scale = Math.Min(1f, 30f / (frame.Width + frame.Height) * 2);
                var drawPosition = GetDimensions().Position() + new Vector2(25f) - frame.Size() * 0.5f * scale;
                spriteBatch.Draw(tex, drawPosition, frame, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            if (isMouseOver) // 如果鼠标移入物品槽，绘制一个半透明的覆盖层来防止点击
            {
                if (!items[index].IsAir)
                {
                    Main.hoverItemName = items[index].Name;
                    Main.HoverItem = items[index].Clone();
                }
                else
                    Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value,
                       Language.ActiveCulture.Name == "zh-Hans" ? "可放入魔法武器": "Can be placed in magic weapons", 
                       Main.MouseScreen.X + 26, Main.MouseScreen.Y, Color.White, Color.Black, Vector2.Zero, 0.8f);

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White * 0.02f);
            }
        }
    }
    // 4. UIItemSlot实现
    //public class UIItemSlotMagicSachet : UIElement
    //{
    //    public Item item = new Item();
    //    private MagicSachet magicSachet;

    //    public UIItemSlotMagicSachet(Item item, int index, MagicSachet magicSachet)
    //    {
    //        this.item = item;// 物品数组
    //        this.magicSachet = magicSachet;
    //        Width.Set(52f, 0f);
    //        Height.Set(52f, 0f);
    //    }
    //    public override void Draw(SpriteBatch spriteBatch)
    //    {
    //        base.Draw(spriteBatch);
    //        CalculatedStyle style = GetInnerDimensions();
    //        spriteBatch.Draw(TextureAssets.InventoryBack9.Value, style.Position(), Color.White);
    //        if (!item.IsAir)
    //        {
    //            Texture2D tex = TextureAssets.Item[item.type].Value;
    //            spriteBatch.Draw(tex, style.Position() + new Vector2(26, 26), null, Color.White, 0f, tex.Size() / 2, 1f, SpriteEffects.None, 0f);
    //        }
    //    }

    //    public override void Update(GameTime gameTime)
    //    {
    //        base.Update(gameTime);

    //    }
    //}

    // 5. 弹幕类
    public class MagicSachetProj : ModProjectile
    {
        private float parentItemType = 0;
        private float parentItemShoot = 0;
        private float parentItemShootSpeed = 0;
        private float parentItemAnimation = 0;
        private float parentItemMana = 0;
        //private float parentItemAmmo = 0;
        //private float parentItemMaxStack = 0;
        public static bool appliedBloodFiend = false;

        private bool Isparent = false;

        NPC targetNPC = null;


        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            Main.projPet[Projectile.type] = true;
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
        }
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.hide = false;
            Projectile.minion = true;
            Projectile.timeLeft = 2;
            Projectile.height = Projectile.width = 10;
            Projectile.minionSlots = 1;
            Projectile.tileCollide = false;
        }
        
        public override bool MinionContactDamage()
        {
            return false;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.velocity = Vector2.Zero;
            base.OnSpawn(source);
            int weaponType = (int)Projectile.ai[1];
            
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                Texture2D texture_ = TextureAssets.Item[weaponType].Value;
                cachedAverageColor = GetTextureAverageColor(texture_);
                averageColorCalculated = true;
             }
        }

        void Attack()
        {
            Player player = Main.player[Projectile.owner];
            if (targetNPC != null && targetNPC.active)
            {
                var weapon = parentItemShoot;

                if (weapon != 0 && weapon > ProjectileID.None)
                {
                    int projType = (int)weapon;
                   
                    Vector2 shootDirection = (targetNPC.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    float shootSpeed = parentItemShootSpeed >= 12 ? parentItemShootSpeed : 12;
                    int shootDamage = Projectile.damage;
                   
                    float shootKnockback = Projectile.knockBack;

                    int newProj = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        shootDirection * shootSpeed,
                        projType,
                        shootDamage,
                        shootKnockback,
                        player.whoAmI
                    );
                    if (newProj >= 0 && newProj < Main.maxProjectiles)
                    {
                        var proj = Main.projectile[newProj];
                        proj.friendly = true;
                        proj.timeLeft = 120;

                        if (!proj.usesLocalNPCImmunity || proj.localNPCHitCooldown > 10)
                        {
                            proj.usesLocalNPCImmunity = true;
                            proj.localNPCHitCooldown = 10; // 可调
                        }

                        if (proj.type == ProjectileID.PurificationPowder)
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed * 8f,
                                ProjectileID.Bullet,
                                shootDamage,
                                0f,
                                player.whoAmI
                            );
                        }
                        if (parentItemType == 3014) // 爬藤怪法杖
                        {
                            proj.Kill();
                            int X = Main.rand.Next(175, 225);
                            int damage = shootDamage * ProjCount;
                            int a = Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                ProjectileID.ClingerStaff,
                                damage,
                                shootKnockback,
                                player.whoAmI,
                                targetNPC.Center.Y - X / 2,
                                X
                            );
                            Main.projectile[a].Center = targetNPC.Center;
                        }
                        else if (parentItemType == 3852) // 无限智慧巨著
                        {
                            //proj.Kill();
                            Vector2 dir = shootDirection.SafeNormalize(Vector2.UnitY);
                            if(Main.rand.NextBool(10))
                                Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                ProjectileID.DD2ApprenticeStorm,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI
                            );
                            else
                            //前后排列
                            for (int i = 0; i < 2; i++)
                            {
                                float offset = (i - 2) * 28f;
                                Vector2 spawnPos = Projectile.Center + dir * offset;
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    dir * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 2795) // 激光机枪
                        {
                            proj.Kill();
                            for (int i = 0; i < Main.rand.Next(1, 3); i++)
                            {
                                // 随机偏移角度（-10° ~ +10°）
                                float offset = Main.rand.NextFloat(-0.08f, 0.08f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    ProjectileID.LaserMachinegunLaser,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 3269) // 蛇发女妖头
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<MagicSachetProjectile_535>(),
                                shootDamage,
                                shootKnockback,
                                player.whoAmI,
                                targetNPC.whoAmI
                            );
                        }
                        else if (parentItemType == 4270) // 血荆棘
                        {
                            proj.Kill();
                            for (int i = 0; i < 2; i++)
                            {
                                // 在敌人周围半径40~60像素的随机角度生成
                                float angle = Main.rand.NextFloat(0, MathHelper.TwoPi);
                                float radius = Main.rand.NextFloat(80f, 240f);
                                Vector2 spawnPos = targetNPC.Center + angle.ToRotationVector2() * radius;

                                // 方向指向敌人中心
                                Vector2 dirToTarget = (targetNPC.Center - spawnPos).SafeNormalize(Vector2.UnitY);

                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    spawnPos,
                                    dirToTarget * shootSpeed,
                                    ProjectileID.SharpTears,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI,
                                    0,
                                    1
                                );
                            }
                        }
                        else if (parentItemType == 3541) // 终极棱镜
                        {
                            proj.Kill();

                            // 检查是否已存在属于本主体弹幕的 MagicSachetProjectile_632
                            bool alreadyExists = false;
                            int myWhoAmI = Projectile.whoAmI;
                            for (int i = 0; i < Main.maxProjectiles; i++)
                            {
                                Projectile p = Main.projectile[i];
                                if (p.active
                                    && p.type == ModContent.ProjectileType<MagicSachetProjectile_632>()
                                    && p.ai[0] == myWhoAmI // 只查找属于本主体弹幕的
                                )
                                {
                                    alreadyExists = true;
                                    break;
                                }
                            }

                            if (!alreadyExists)
                            {
                                int newProj_ = Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed * 0.01f,
                                    ModContent.ProjectileType<MagicSachetProjectile_632>(),
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI
                                );
                                if (newProj_ >= 0 && newProj_ < Main.maxProjectiles)
                                {
                                    Main.projectile[newProj_].ai[0] = myWhoAmI; // 记录父弹幕
                                }
                            }
                        }
                        else if (parentItemType == 4715) // 星星吉他
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                ProjectileID.SparkleGuitar,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI,
                                targetNPC.Center.X,
                                targetNPC.Center.Y
                            );
                        }
                        else if (parentItemType == 3006) // 夺命杖
                        {
                            proj.Kill();
                            int a = Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                Vector2.Zero,
                                ProjectileID.SoulDrain,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI
                            );
                            Main.projectile[a].Center = targetNPC.Center;
                            Main.projectile[a].usesLocalNPCImmunity = true;
                            Main.projectile[a].localNPCHitCooldown = 8;
                            player.AddBuff(BuffID.SoulDrain, 30);
                            int X = targetNPC.width / 10;
                            int Y = targetNPC.height / 10;
                            Vector2 start = targetNPC.Center + new Vector2(Main.rand.Next(-X, X), Main.rand.Next(-Y, Y));
                            // 在敌人中心产生粒子，飞向玩家中心
                            for (int i = 0; i < 6; i++)
                            {
                                Dust dust = Dust.NewDustPerfect(start, DustID.LifeDrain, Vector2.Zero, 1, Color.White, 1.2f);
                                dust.noGravity = true;
                                dust.fadeIn = 1f;
                            }
                        }
                        else if (parentItemType == 3542) // 星云烈焰
                        {
                            if(Main.rand.NextBool(4))
                            {
                                proj.Kill();
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    shootDirection * shootSpeed,
                                    ProjectileID.NebulaBlaze2,
                                    shootDamage * Main.rand.Next(2, 4),
                                    shootKnockback,
                                    player.whoAmI
                                );
                            }
                        }
                        else if (parentItemType == 2882) // 充能爆破炮
                        {
                            proj.Kill();
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed * 6f,
                                ProjectileID.ChargedBlasterOrb,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI,
                                0,
                                0.6f
                            );
                        }
                        else if (parentItemType == 3779) // 神灯烈焰
                        {
                            proj.Kill();
                            // 计算伤害倍率
                            double damageMultiplier = Math.Pow(2, (ProjCount - 1) / 16.0);
                            int damage = (int)Math.Round(shootDamage * damageMultiplier);
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed * 6f,
                                ProjectileID.SpiritFlame,
                                damage,
                                shootKnockback,
                                player.whoAmI,
                                -2,
                                -2
                            );
                        }
                        else if (parentItemType == 4952)// 夜光
                        {
                            proj.Kill();
                            float X = Main.rand.NextFloat(-10f, 10f);
                            for (int i = 0; i < Main.rand.Next(1, 3); i++)
                            {
                                float offset = Main.rand.NextFloat(-0.17f, 0.17f); // 约±10°
                                Vector2 perturbedDirection = shootDirection.RotatedBy(offset);
                                int a = Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    Projectile.Center,
                                    perturbedDirection * shootSpeed,
                                    projType,
                                    shootDamage,
                                    shootKnockback,
                                    player.whoAmI,
                                    0,
                                    X
                                );
                                Main.projectile[a].width = 10;
                                Main.projectile[a].height = 10;
                                Main.projectile[a].usesLocalNPCImmunity = true;
                                Main.projectile[a].localNPCHitCooldown = 10;
                            }
                        }
                        else if (parentItemType == 2750) // 流星法杖
                        {
                            proj.Kill();
                            // 四种弹幕类型
                            int[] fireworks = new int[]
                            {
                                ProjectileID.Meteor1,
                                ProjectileID.Meteor2,
                                ProjectileID.Meteor3
                            };
                            // 随机选取一个不同的类型
                            int Prj = Main.rand.Next(fireworks.Length);
                            int selected = fireworks[Prj];

                            float X = Main.rand.NextFloat(0.4f, 0.8f);
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                Projectile.Center,
                                shootDirection * shootSpeed,
                                selected,
                                shootDamage,
                                shootKnockback,
                                player.whoAmI,
                                0,
                                X
                            );
                        }
                    }
                }
            }
        }
        private int ShootTimer = 0;
        private int ProjCount = 0;
        private readonly int MaxDis = 1200;

        public delegate bool SpecialCondition(NPC possibleTarget);
        public override bool PreAI()
        {
            // 获取玩家
            var player = Main.player[Projectile.owner];
            if (!Isparent)
            {
                parentItemAnimation = Projectile.ai[0];
                parentItemType = (int)Projectile.ai[1];
                parentItemMana = Projectile.ai[2];
                parentItemShootSpeed = Projectile.localAI[0];
                parentItemShoot = Projectile.localAI[1];


                Isparent = true;
            }
            if (!player.dead && player.HasBuff(ModContent.BuffType<BuffsMagicSachet>()))
            {
                Projectile.timeLeft = 2;
            }
            if (!player.HasBuff(ModContent.BuffType<BuffsMagicSachet>())) Projectile.Kill();
            targetNPC = null;
            if (FlyingGunProj.ClosestNPC(ref targetNPC, MaxDis, player.Center, MagicSachet.IgnoreTilesForTargeting, player.MinionAttackTargetNPC, npc => npc.active))
            {
                Projectile.rotation = (targetNPC.Center - Projectile.Center).ToRotation();
            }
            else
            {
                Projectile.rotation = 0;
            }
            if (targetNPC != null && targetNPC.active)
            {
                ShootTimer++;
                if (ShootTimer > parentItemAnimation)
                {
                    if(player.CheckMana((int)(parentItemMana/2), true, false))
                    {
                        player.manaRegenDelay = (int)player.maxRegenDelay;
                        Attack();
                        ShootTimer = 0;
                    }
                }
            }
            if (targetNPC != null && targetNPC.active)
            {
                // 计算玩家到敌人的距离
                float playerToTarget = Vector2.Distance(player.Center, targetNPC.Center);

                // 设定弹幕环绕距离为：玩家到敌人距离的80%~120%，并限制最小/最大值
                float minRadius = 60f;
                float maxRadius = 320f;
                float orbitRadius = MathHelper.Clamp(playerToTarget * 0.8f, minRadius, maxRadius) + ProjCount * 4;

                AttackOrbitAI(Projectile, targetNPC, orbitRadius, 0.15f);
            }
            else
            {
                StandbyOrbitAI(Projectile, 80f + ProjCount * 3, 0.02f, 0.15f, player.MountedCenter);
            }
            return base.PreAI();
        }
        void AttackOrbitAI(Projectile projectile, NPC target, float orbitRadius, float lerpFactor)
        {
            // 统计同类弹幕数量和编号
            int index = 0, total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == projectile.owner && proj.type == projectile.type)
                {
                    if (i < projectile.whoAmI) index++;
                    total++;
                    ProjCount = total;
                }
            }

            Player player = Main.player[projectile.owner];
            Vector2 dir = (player.Center - target.Center).SafeNormalize(Vector2.UnitX);
            float baseAngle = dir.ToRotation();
            float spread = MagicSachet.AttackSpread;
            float angle = baseAngle - spread / 2 + spread * (index + 0.5f) / Math.Max(1, total);
            // 随机分布
            //float angle = MathHelper.TwoPi * (index + 0.5f) / Math.Max(1, total);
            float randomRadius = orbitRadius * (0.7f + 0.3f * (float)Math.Sin(Main.GameUpdateCount * 0.05f + index));
            float yOffset = (float)Math.Sin(Main.GameUpdateCount * 0.07f + index) * 16f;
            Vector2 targetPos = target.Center + randomRadius * angle.ToRotationVector2() + new Vector2(0, yOffset);
            projectile.Center = Vector2.Lerp(projectile.Center, targetPos, lerpFactor);
        }
        void StandbyOrbitAI(Projectile projectile, float orbitRadius, float orbitSpeed, float lerpFactor, Vector2 center)
        {
            // 统计同类弹幕数量和编号
            int index = 0, total = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.owner == projectile.owner && proj.type == projectile.type)
                {
                    if (i < projectile.whoAmI) index++;
                    total++;
                    ProjCount = total;
                }
            }

            // 月牙形分布参数
            float arcSpan = MathHelper.ToRadians(180f); // 总弧度（120°月牙）
            float baseAngle = MathHelper.PiOver2; // 玩家正上方
            float angle = baseAngle - arcSpan / 2 + arcSpan * (index + 0.5f) / Math.Max(1, total);

            // 计算月牙形偏移（两侧向下偏移，中心最高）
            float xOffset = (float)Math.Cos(angle) * orbitRadius;
            float yOffset = -(float)Math.Sin(angle) * orbitRadius * 0.7f; // 0.7控制月牙弯曲度

            // 漂浮效果（上下缓慢偏移）
            float floatAmplitude = 4f; // 漂浮幅度
            float floatSpeed = 0.04f;   // 漂浮速度
            float floatOffset = (float)Math.Sin(Main.GameUpdateCount * floatSpeed + index * 0.7f) * floatAmplitude;

            Vector2 targetPos = center + new Vector2(xOffset, yOffset + floatOffset); // -32f让弹幕整体在玩家头顶

            // 平滑插值移动
            projectile.Center = Vector2.Lerp(projectile.Center, targetPos, lerpFactor);
        }
        public override void AI()
        {
            //前缀血煞吸血效果添加
            int weaponType = (int)parentItemType;
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                // 获取父武器实例
                Item weapon = new Item();
                weapon.SetDefaults(weaponType);

                int prefix = (int)Projectile.localAI[2];

                // 判断是否为BloodFiend前缀
                if (prefix == ModContent.PrefixType<BloodFiend>())
                {
                    appliedBloodFiend = true;
                }
            }
        }

        [Obsolete]
        public override void Kill(int timeLeft)
        {
            appliedBloodFiend = false;

            parentItemAnimation = 0;
            parentItemType = 0;
            parentItemMana = 0;
            parentItemShootSpeed = 0;
            parentItemShoot = 0;

            base.Kill(timeLeft);
        }
        private Color cachedAverageColor = Color.White;
        private bool averageColorCalculated = false;

        private Color GetTextureAverageColor(Texture2D texture)
        {
            if (averageColorCalculated) return cachedAverageColor;
            if (texture == null) return Color.White;
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            int r = 0, g = 0, b = 0, count = 0;
            foreach (var c in data)
            {
                if (c.A > 32)
                {
                    r += c.R; g += c.G; b += c.B; count++;
                }
            }
            if (count == 0) return Color.White;
            cachedAverageColor = new Color(r / count, g / count, b / count);
            averageColorCalculated = true;
            return cachedAverageColor;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            int weaponType = (int)parentItemType;
            if (weaponType > 0 && weaponType < ItemLoader.ItemCount)
            {
                Texture2D texture_ = TextureAssets.Item[weaponType].Value;
                int frameCount = Main.itemAnimations[weaponType]?.FrameCount ?? 1;
                int frameHeight = texture_.Height / frameCount;
                int frameY = 0;
                // 弹幕帧与物品帧同步（可自定义）
                if (Main.itemAnimations[weaponType] != null)
                    frameY = Main.itemAnimations[weaponType].Frame * frameHeight;
                Rectangle rectangle = new Rectangle(0, frameY, texture_.Width, frameHeight);

                SpriteEffects effects = SpriteEffects.None; // 贴图效果
                float rotationOffset = MathHelper.ToRadians(-45f) - MagicSachet.ExtraDrawRotation;
                var player = Main.player[Projectile.owner];
                if (targetNPC != null)
                {
                    if (targetNPC.Center.X < Projectile.Center.X)
                    {
                        effects = SpriteEffects.FlipHorizontally;
                        rotationOffset = MathHelper.ToRadians(135f) + MagicSachet.ExtraDrawRotation;
                    }
                    else
                    {
                        effects = SpriteEffects.None;
                        rotationOffset = MathHelper.ToRadians(45f) - MagicSachet.ExtraDrawRotation;
                    }
                }
                else
                {
                    if (player.direction == -1)
                    {
                        effects = SpriteEffects.FlipHorizontally;
                        rotationOffset = MathHelper.ToRadians(45f) + MagicSachet.ExtraDrawRotation;
                    }
                }

                Color LightsColor = cachedAverageColor;
                var value = (float)(Math.Cos(Main.timeForVisualEffects * 0.04 + Projectile.ai[2] * 0.7) * 0.3f + 0.4f);
                var v3 = Main.rgbToHsl(LightsColor);
                v3.X += ((float)Math.Cos(Main.timeForVisualEffects * 0.06 + Projectile.ai[2] * 0.5) * 0.5f + 0.5f) * 0.1f;
                var c = Main.hslToRgb(v3);
                c.A = 0;
                if(MagicSachet.IsTail)
                {
                    Color MyColor = c * (0.4f / 3f);
                    MyColor.A = 0;
                    int maxStep = ProjectileID.Sets.TrailCacheLength[Type] - 7;
                    if (Projectile.ai[0] != 0) maxStep += 7;
                    for (int i = 1; i < maxStep - 2; i++)
                    {
                        for (float j = 0; j < 1; j += 0.3f)
                        {
                            float factor = (1 - (float)i / ProjectileID.Sets.TrailCacheLength[Type]) * 0.7f + 0.4f;
                            Vector2 oldcenter = Vector2.Lerp(Projectile.oldPos[i], Projectile.oldPos[i - 1], j) + Projectile.Size / 2 - Main.screenPosition;
                            var oldRo = MathHelper.Lerp(Projectile.oldRot[i], Projectile.oldRot[i - 1], j);
                            Main.EntitySpriteDraw(texture_,
                                                  oldcenter,
                                                  rectangle,
                                                  MyColor * factor/* * Projectile.alpha*/,
                                                  oldRo + rotationOffset,
                                                  new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                                                  Projectile.scale * 1f * factor,
                                                  effects,
                                                  0);
                        }
                    }
                }

                Main.EntitySpriteDraw(
                    texture_,
                    Projectile.Center - Main.screenPosition,
                    rectangle,
                    lightColor /** Projectile.alpha*/,
                    Projectile.rotation + rotationOffset,
                    new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                    Projectile.scale * 1f,
                    effects,
                    0
                    );
                #region 以下：渐变高光
                if(MagicSachet.IsTail)
                for (int i = 0; i < 3; i++)
                {
                    Main.EntitySpriteDraw(texture_,
                                          Projectile.Center - Main.screenPosition,
                                          rectangle,
                                          c * value * 0.6f /** Projectile.alpha*/,
                                          Projectile.rotation + rotationOffset,
                                          new Vector2(rectangle.Width / 2, rectangle.Height / 2),
                                          Projectile.scale * 1f,
                                          effects,
                                          0);
                }
                #endregion
                return false;
            }
            return false; // 阻止默认绘制
        }
    }

    // 6. Buff类
    class BuffsMagicSachet : ModBuff
    {
        public override string Texture => "SwordMastery/Content/Items/FlyingSword/AGlobalControl/BuffsFlyingControl";
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<MagicSachetProj>()] > 0)
            {
                player.buffTime[buffIndex] = 18000;
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
        public override bool RightClick(int buffIndex)
        {
            return base.RightClick(buffIndex);
        }
        private Texture2D weaponTex = null;
        public override void PostDraw(SpriteBatch spriteBatch, int buffIndex, BuffDrawParams drawParams)
        {
            base.PostDraw(spriteBatch, buffIndex, drawParams);
            Player player = Main.LocalPlayer;
            int weaponType = player.GetModPlayer<MagicSachetPlayer>().LastSummonWeaponType;
            if (weaponType > 0 && weaponType < TextureAssets.Item.Length && TextureAssets.Item[weaponType].IsLoaded)
            {
                weaponTex = TextureAssets.Item[weaponType].Value;
                Rectangle weaponFrame = Main.itemAnimations[weaponType]?.GetFrame(weaponTex) ?? weaponTex.Frame();
                float maxSize = 20f;
                float scale = 1f;
                if (weaponFrame.Width > maxSize || weaponFrame.Height > maxSize)
                    scale = maxSize / Math.Max(weaponFrame.Width, weaponFrame.Height);

                Vector2 center = drawParams.Position + new Vector2(16, 16);

                Rectangle buffRect = new Rectangle(
                    (int)drawParams.Position.X,
                    (int)drawParams.Position.Y,
                    32, 32
                );
                bool mouseInBuff = buffRect.Contains(Main.mouseX, Main.mouseY);

                Color drawColor = mouseInBuff ? Color.White * 1f : Color.White * 0.5f;

                spriteBatch.Draw(
                    weaponTex,
                    center,
                    weaponFrame,
                    drawColor,
                    0f,
                    weaponFrame.Size() / 2,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
    public class MagicSachetPlayer : ModPlayer
    {
        public int LastSummonWeaponType = 0;
    }
}
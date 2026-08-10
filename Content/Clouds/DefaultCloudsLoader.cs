using Terraria.ModLoader;

namespace SwordMastery.Content.Clouds
{
	// 默认情况下，"Clouds" 文件夹中的图像文件会自动加载为云。我们使用这个类在自动加载之前手动加载 ExampleCloud.png，以便使用自定义参数注册云。
	public class DefaultCloudsLoader : ILoadable
	{
		public void Load(Mod mod) {
			// 注册一个新的简单云。有关具有自定义逻辑的云的示例，请参见 Content/Clouds/AdvancedExampleCloud.cs。
			CloudLoader.AddCloudFromTexture(mod, "SwordMastery/Content/Clouds/Cloud", spawnChance: 1f, rareCloud: true);
		}

		public void Unload() {
		}
	}
}

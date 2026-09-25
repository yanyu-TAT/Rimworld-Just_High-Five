using UnityEngine;
using Verse;

namespace ClapYourHands
{
    /// <summary>模组主类（原版 <see cref="Mod"/>）。</summary>
    public class ClapYourHandsMod : Mod
    {
        /// <summary>日志前缀，便于在 Player.log 中检索本模组。</summary>
        public const string LogPrefix = "[ClapYourHands] ";

        /// <summary>模组设置实例，由原版从设置文件加载。</summary>
        public static ClapSettings Settings { get; private set; }

        public ClapYourHandsMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ClapSettings>();
            Log.Message(LogPrefix + "loaded.");
        }

        /// <summary>返回非空值才会在原版模组菜单里出现「Mod 选项」。</summary>
        public override string SettingsCategory() => "ClapYourHands.Settings.Category".Translate();

        public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
    }
}
/* Todo List:
 * - [x] 添加互动效果、情绪及对应的增益效果
 * - [x] 添加完成完美击掌时的特效和音效
 * - [x] 添加模组设置项（音效音量缩放）
 */

/* Develop Log:
 * 09/12 22:26 初始化了项目框架并进行了初次提交
 * 09/13 01:25 完成了大部分主要内容并进行了测试
 * 09/13 13:35 调整了部分英文翻译
 * 09/26 02:42 添加了完美击掌时的特效和音效，以及相关的部分设置
 */

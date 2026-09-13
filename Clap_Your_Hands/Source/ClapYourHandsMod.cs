using Verse;

namespace ClapYourHands
{
    /// <summary>模组主类（原版 <see cref="Mod"/>）。</summary>
    public class ClapYourHandsMod : Mod
    {
        /// <summary>日志前缀，便于在 Player.log 中检索本模组。</summary>
        public const string LogPrefix = "[ClapYourHands] ";

        public ClapYourHandsMod(ModContentPack content) : base(content)
        {
            Log.Message(LogPrefix + "loaded.");
        }
    }
}
/* Todo List:
 * - [x] 添加互动效果、情绪及对应的增益效果
 * - [ ] 添加完成完美击掌时的特效和音效
 */

/* Develop Log:
 * 09/12 22:26 初始化了项目框架并进行了初次提交
 * 09/13 01:25 完成了大部分主要内容并进行了测试
 * 09/13 13:35 调整了部分英文翻译
 */
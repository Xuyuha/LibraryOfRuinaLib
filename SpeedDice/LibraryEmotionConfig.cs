namespace Library.SpeedDice;

/// <summary>
/// 情感系统配置，控制情感等级的升级阈值及各等级加成
/// </summary>
public sealed class LibraryEmotionConfig
{
    /// <summary>
    /// 每级情感升级所需的情感单位数，按等级索引
    /// 默认：[3, 3, 5, 7, 9] 即 1→2级需3单位，2→3级需3单位，可自定
    /// </summary>
    public IReadOnlyList<int> UnitThresholds { get; init; } = [3, 3, 5, 7, 9];

    /// <summary>
    /// 是否可通过造成/承受伤害获取情感单位
    /// </summary>
    public bool GainEmotionFromDamage { get; init; } = true;

    /// <summary>
    /// 伤害换情感单位的阈值比例：每造成目标最大生命值此比例（×100%）的伤害，获得1情感单位
    /// 不足阈值的剩余伤害会累积到下一次。默认 0.10 即 10%
    /// </summary>
    public decimal DamageUnitFractionOfMaxHp { get; init; } = 0.10m;

    /// <summary>
    /// 掷出极端值（最大/最小）时获得的情感单位数（目前我采用最大）
    /// </summary>
    public int ExtremeRollEmotionUnits { get; init; }

    /// <summary>
    /// 击杀敌人时获得的情感单位数（可自己改）
    /// </summary>
    public int KillEmotionUnits { get; init; } = 3;

    /// <summary>
    /// 友方死亡时获得的情感单位数
    /// </summary>
    public int AllyDeathEmotionUnits { get; init; }

    /// <summary>
    /// 每级情感等级提升时获得的额外光芒上限
    /// </summary>
    public int MaxEnergyPerLevel { get; init; } = 1;

    /// <summary>
    /// 获得额外速度骰子的情感等级要求
    /// </summary>
    public int ExtraSpeedDieLevel { get; init; } = 4;

    /// <summary>
    /// 达到等级要求时额外获得的速度骰子数量
    /// </summary>
    public int ExtraSpeedDice { get; init; } = 1;

    /// <summary>
    /// 获得额外抽牌奖励的情感等级要求
    /// </summary>
    public int BonusDrawLevel { get; init; } = 5;

    /// <summary>
    /// 触发额外抽牌所需通过骰子打出的卡牌数
    /// </summary>
    public int BonusDrawRequiredTriggeredCards { get; init; } = 2;

    /// <summary>
    /// 触发额外抽牌时的抽牌数量
    /// </summary>
    public int BonusDrawAmount { get; init; } = 2;
}

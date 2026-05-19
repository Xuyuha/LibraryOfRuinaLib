// using Library.Utils;
// using MegaCrit.Sts2.Core.Entities.Cards;
// using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// using MegaCrit.Sts2.Core.Localization.DynamicVars;

// public abstract class LibraryDice //骰子，准备做成伤害容器
// {
//     DynamicVar MinDamage;
//     DynamicVar FloatDamage;
//     LibraryDamageType DamageType;
//     public abstract Task DiceEffct (PlayerChoiceContext choiceContext, CardPlay cardPlay);
//     public async Task TriggerEffct (PlayerChoiceContext choiceContext, CardPlay cardPlay){
//         if(!LibraryHooks.TryDiceEffect(choiceContext, cardPlay.Target, cardPlay.Card))return;
//         await LibraryHooks.BeforeDiceEffect(choiceContext, cardPlay.Target, cardPlay.Card);
//         await DiceEffct(choiceContext, cardPlay);
//         await LibraryHooks.AfterDiceEffect(choiceContext, cardPlay.Target, cardPlay.Card);
//     }
// }
﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Triggered Ability", menuName = "Card Abilities/Triggered Ability")]
public class TriggeredAbility : CardAbility
{
    public AbilityTrigger AbilityTrigger;
    [Range(0, 1), Tooltip("Limit the number of times this ability triggers each turn, 0 for unlimited")]
    public int TriggerLimit;
    public List<EffectGroup> EffectGroupList;

    public int TriggerCount { get; set; }

    public override void LoadCardAbility(CardAbility cardAbility)
    {
        base.LoadCardAbility(cardAbility);
        var triggeredAbility = cardAbility as TriggeredAbility;
        AbilityTrigger = triggeredAbility.AbilityTrigger;
        int triggerLimit = AbilityTrigger.TriggerLimit;
        if (triggerLimit != 0) TriggerLimit = triggerLimit;
        else TriggerLimit = triggeredAbility.TriggerLimit;
        EffectGroupList = triggeredAbility.EffectGroupList.ToList();

        TriggerCount = 0;
    }
}

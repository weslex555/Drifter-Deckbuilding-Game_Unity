﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : HeroManager
{
    /* SINGELTON_PATTERN */
    public static PlayerManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private bool heroPowerUsed;
    private int currentAether, heroUltimateProgress;

    private List<HeroAugment> heroAugments;
    private List<HeroItem> heroItems;

    public override string HERO_TAG => "PlayerHero";
    public override string CARD_TAG => "PlayerCard";
    public override string HAND_ZONE_TAG => "PlayerHand";
    public override string PLAY_ZONE_TAG => "PlayerZone";
    public override string ACTION_ZONE_TAG => "PlayerActionZone";
    public override string DISCARD_ZONE_TAG => "PlayerDiscard";
    public override string HERO_POWER_TAG => "HeroPower";
    public override string HERO_ULTIMATE_TAG => "HeroUltimate";

    public override Hero HeroScript { get => heroScript; set { heroScript = value; } }
    public List<HeroAugment> HeroAugments { get => heroAugments; }
    public List<HeroItem> HeroItems { get => heroItems; }
    
    public override bool IsMyTurn
    {
        get => isMyTurn;
        set
        {
            isMyTurn = value;
            if (UIManager.Instance != null) Managers.U_MAN.UpdateEndTurnButton();
        }
    }

    public override int TurnNumber { get => turnNumber; set { turnNumber = value; } }

    public int CurrentAether_Direct { set { currentAether = value; } }
    public int CurrentAether
    {
        get => currentAether;
        set
        {
            int previousCount = currentAether;
            currentAether = value;
            int valueChange = currentAether - previousCount;

            AnimationManager.CountingTextObject.ClearCountingTexts();

            /*
            var acpd = FindObjectOfType<AetherCellPopupDisplay>();
            if (acpd != null)
            {
                new AnimationManager.CountingTextObject(acpd.TotalAetherObject.GetComponent<TextMeshProUGUI>(),
                    valueChange, Color.red, acpd.TotalAether_Additional);

                new AnimationManager.CountingTextObject(acpd.AetherQuantityObject.GetComponent<TextMeshProUGUI>(),
                    valueChange, Color.red, acpd.AetherQuantity_Additional);
            }
            */

            if (UIManager.Instance != null) Managers.U_MAN.SetAetherCount(valueChange);
        }
    }
    public bool HeroPowerUsed
    {
        get => heroPowerUsed;
        set
        {
            heroPowerUsed = value;
            var phd = HeroObject.GetComponent<PlayerHeroDisplay>();
            var powerImage = phd.HeroPowerImage;
            var img = powerImage.GetComponent<Image>();
            img.color = heroPowerUsed ? Color.gray : Color.white;
            var powerReadyIcon = phd.PowerReadyIcon;
            powerReadyIcon.SetActive(!heroPowerUsed);
        }
    }
    public int HeroUltimateProgress
    {
        get => heroUltimateProgress;
        set
        {
            heroUltimateProgress = value;
            if (HeroObject == null) return;

            var phd = HeroObject.GetComponent<PlayerHeroDisplay>();
            phd.UltimateProgressValue = heroUltimateProgress;
            var ultimateReadyIcon = phd.UltimateReadyIcon;
            var ultimateButton = phd.UltimateButton;

            int heroUltimateGoal = GameManager.HERO_ULTMATE_GOAL;
            if (heroUltimateProgress >= heroUltimateGoal) ultimateReadyIcon.SetActive(true);
            else ultimateReadyIcon.SetActive(false);

            if (heroUltimateProgress == heroUltimateGoal)
            {
                Managers.AU_MAN.StartStopSound("SFX_HeroUltimateReady");
                Managers.AN_MAN.AbilityTriggerState(ultimateButton);
            }
        }
    }
    public override int MaxHealth => GameManager.PLAYER_STARTING_HEALTH;

    protected override void Start()
    {
        base.Start();
        heroAugments ??= new();
        heroItems ??= new();
        HeroScript = null; // Unnecessary?
        CurrentAether = 0;
        IsMyTurn = false; // Needs to be false to disable DragDrop outside of combat
    }

    public void AddItem(HeroItem item, bool isNewItem = false)
    {
        if (GetItem(item.ItemName))
        {
            Debug.LogError("ITEM ALREADY EXISTS!");
            return;
        }

        heroItems.Add(item);
        Managers.U_MAN.CreateItemIcon(item, isNewItem);
        if (isNewItem) Managers.AU_MAN.StartStopSound("SFX_BuyItem");
    }

    private bool GetItem(string itemName)
    {
        int itemIndex = heroItems.FindIndex(x => x.ItemName == itemName);
        return itemIndex != -1;
    }

    public void AddAugment(HeroAugment augment, bool isNewAugment = false)
    {
        if (GetAugment(augment.AugmentName))
        {
            Debug.LogError("AUGMENT ALREADY EXISTS!");
            return;
        }

        heroAugments.Add(augment);
        Managers.U_MAN.CreateAugmentIcon(augment, isNewAugment);
        if (isNewAugment) Managers.AU_MAN.StartStopSound("SFX_AcquireAugment");
    }

    public bool GetAugment(string augmentName)
    {
        if (Managers.G_MAN.IsTutorial) return false;
        int augmentIndex = heroAugments.FindIndex(x => x.AugmentName == augmentName);
        return augmentIndex != -1;
    }

    public bool UseHeroPower(bool isUltimate, bool isPreCheck = false)
    {
        static void ErrorSound() => Managers.AU_MAN.StartStopSound("SFX_Error");

        if (isUltimate) return UseHeroUltimate(isPreCheck);

        if (HeroPowerUsed)
        {
            if (isPreCheck) return false;
            Managers.U_MAN.CreateFleetingInfoPopup("Hero power already used this turn!");
            ErrorSound();
        }
        else if (CurrentEnergy < GetPowerCost(out _))
        {
            if (isPreCheck) return false;
            Managers.U_MAN.CreateFleetingInfoPopup("Not enough energy!");
            ErrorSound();
        }
        else
        {
            var heroPower = HeroObject.GetComponent<PlayerHeroDisplay>().HeroPower;
            var groupList = HeroScript.CurrentHeroPower.EffectGroupList;

            if (!Managers.EF_MAN.CheckLegalTargets(groupList, heroPower, true))
            {
                if (isPreCheck) return false;
                Managers.U_MAN.CreateFleetingInfoPopup("You can't do that right now!");
                ErrorSound();
            }
            else
            {
                if (isPreCheck) return true;
                CurrentEnergy -= GetPowerCost(out _);
                HeroPowerUsed = true;
                PlayerPowerSounds();
                ParticleBurst(heroPower);
                Managers.EF_MAN.StartEffectGroupList(groupList, heroPower);
            }
        }

        return true;
    }

    private bool UseHeroUltimate(bool isPreCheck)
    {
        static void ErrorSound() => Managers.AU_MAN.StartStopSound("SFX_Error");

        var heroUltimate = HeroObject.GetComponent<PlayerHeroDisplay>().HeroUltimate;
        var groupList = (HeroScript as PlayerHero).CurrentHeroUltimate.EffectGroupList;

        if (HeroUltimateProgress < GameManager.HERO_ULTMATE_GOAL)
        {
            if (isPreCheck) return false;
            Managers.U_MAN.CreateFleetingInfoPopup("Ultimate not ready!");
            ErrorSound();
        }
        else if (CurrentEnergy < GetUltimateCost(out _))
        {
            if (isPreCheck) return false;
            Managers.U_MAN.CreateFleetingInfoPopup("Not enough energy!");
            ErrorSound();
        }
        else if (!Managers.EF_MAN.CheckLegalTargets((HeroScript as PlayerHero).CurrentHeroUltimate.EffectGroupList, heroUltimate, true))
        {
            if (isPreCheck) return false;
            Managers.U_MAN.CreateFleetingInfoPopup("You can't do that right now!");
            ErrorSound();
        }
        else
        {
            if (isPreCheck) return true;
            CurrentEnergy -= GetUltimateCost(out _);
            PlayerPowerSounds(true);
            ParticleBurst(heroUltimate);
            Managers.EF_MAN.StartEffectGroupList(groupList, heroUltimate);
        }

        return true;
    }

    public int GetMaxItems(out bool hasBonus)
    {
        int bonusItems = 0;
        hasBonus = false;

        if (GetAugment("Kinetic Reinforcer"))
        {
            hasBonus = true;
            bonusItems = 3;
        }
        return GameManager.MAXIMUM_ITEMS + bonusItems;
    }

    public int GetPowerCost(out Color powerColor)
    {
        var pwr = HeroScript.CurrentHeroPower;
        int cost = pwr.PowerCost;
        cost += Managers.CA_MAN.GetCostConditionValue(pwr, HeroObject); // TESTING

        if (pwr.PowerCost > 0 && cost < pwr.PowerCost) powerColor = Color.green;
        else powerColor = Color.white;

        if (cost < 0) cost = 0;
        return cost;
    }

    public int GetUltimateCost(out Color ultimateColor)
    {
        var ult = (HeroScript as PlayerHero).CurrentHeroUltimate;
        int cost = ult.PowerCost;
        cost += Managers.CA_MAN.GetCostConditionValue(ult, HeroObject); // TESTING

        if (Managers.G_MAN.GetReputationTier(GameManager.ReputationType.Techs) > 2)
        {
            if (ult.PowerCost > 0 && cost < ult.PowerCost) ultimateColor = Color.green;
            else ultimateColor = Color.white;
        }
        else ultimateColor = Color.white;

        if (cost < 0) cost = 0;
        return cost;
    }

    private void ParticleBurst(GameObject parent) =>
        Managers.AN_MAN.CreateParticleSystem(parent, ParticleSystemHandler.ParticlesType.ButtonPress, 1);

    public void PlayerPowerSounds(bool isUltimate = false)
    {
        Sound[] soundList;
        if (isUltimate) soundList = (HeroScript as PlayerHero).CurrentHeroUltimate.PowerSounds;
        else soundList = HeroScript.CurrentHeroPower.PowerSounds;
        foreach (var s in soundList) Managers.AU_MAN.StartStopSound(null, s);
    }
}

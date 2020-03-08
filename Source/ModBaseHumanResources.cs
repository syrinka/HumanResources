﻿using HugsLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace HumanResources
{
    public class ModBaseHumanResources : ModBase
    {
        
        public override string ModIdentifier
        {
            get
            {
                return "JPT_HumanResources";
            }
        }

        // ThingDef injection stolen from the work of notfood for Psychology
        public override void DefsLoaded()
        {
            //Log.Warning("DefsLoaded start...");

            //Adding Tech Tab to Pawns
            var zombieThinkTree = DefDatabase<ThinkTreeDef>.GetNamedSilentFail("Zombie");
            IEnumerable<ThingDef> things = (from def in DefDatabase<ThingDef>.AllDefs
                                            where def.race?.intelligence == Intelligence.Humanlike && !def.defName.Contains("Android") && !def.defName.Contains("Robot")&& (zombieThinkTree == null || def.race.thinkTreeMain != zombieThinkTree)
                                            select def);
            List<string> registered = new List<string>();
            foreach (ThingDef t in things)
            {
                if (t.inspectorTabsResolved == null)
                {
                    t.inspectorTabsResolved = new List<InspectTabBase>(1);
                }
                t.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_PawnKnowledge)));
                if (t.comps == null)
                {
                    t.comps = new List<CompProperties>(1);
                }
                t.comps.Add(new CompProperties_Knowledge());
                registered.Add(t.defName);
            }

            //Preparing knowledge support things
            UniversalWeapons.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWeapon));
            UniversalCrops.AddRange(DefDatabase<ThingDef>.AllDefs.Where(x => x.plant != null && x.plant.Sowable));
            ThingFilter lateFilter = new ThingFilter();
            ThingCategoryDef knowledgeCat = DefDatabase<ThingCategoryDef>.GetNamed("Knowledge");
            foreach (ResearchProjectDef tech in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                tech.InferSkillBias();
                tech.CreateStuff(lateFilter, unlocked.stuffByTech);
                foreach (ThingDef weapon in tech.UnlockedWeapons()) UniversalWeapons.Remove(weapon);
                foreach (ThingDef plant in tech.UnlockedPlants()) UniversalCrops.Remove(plant);
            };
            Log.Message("[HumanResources] Codified technologies:" + DefDatabase<ThingDef>.AllDefs.Where(x => x.IsWithinCategory(knowledgeCat)).Select(x => x.label).ToStringSafeEnumerable());
            //Log.Message("[HumanResources] Codified technologies:" + DefDatabase<ThingDef>.AllDefs.Where(x => x.stuffProps != null && x.stuffProps.categories.Contains(DefDatabase<StuffCategoryDef>.GetNamed("Technic"))).ToStringSafeEnumerable());
            Log.Message("[HumanResources] Universal weapons: " + UniversalWeapons.ToStringSafeEnumerable());

            //TechBook dirty trick, but only now this is possible!
            //DefDatabase<ThingDef>.GetNamed("TechBook").stuffCategories.Add(DefDatabase<StuffCategoryDef>.GetNamed("Technic"));
            foreach (ThingDef t in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.defName.Contains("TechBook")))
            {
                t.stuffCategories.Add(DefDatabase<StuffCategoryDef>.GetNamed("Technic"));
            }

            //Filling main technic category with subcategories
            foreach (ThingDef t in lateFilter.AllowedThingDefs.Where(t => !t.thingCategories.NullOrEmpty()))
            {
                foreach (ThingCategoryDef c in t.thingCategories)
                {
                    c.childThingDefs.Add(t);
                    if (!knowledgeCat.childCategories.Contains(c))
                    {
                        knowledgeCat.childCategories.Add(c);
                    }
                }
            }

            //Populating knowledge recipes and book shelves
            ThingFilter filter = new ThingFilter();
            filter.SetAllow(knowledgeCat, true);
            foreach (RecipeDef r in DefDatabase<RecipeDef>.AllDefs.Where(x => x.defName.StartsWith("Tech_")))
            {
                r.fixedIngredientFilter.CopyAllowancesFrom(filter);
                r.defaultIngredientFilter.CopyAllowancesFrom(filter);
            }
            foreach (ThingDef t in DefDatabase<ThingDef>.AllDefs.Where(x => x.thingClass == typeof(Building_BookStore)))
            {
                t.building.fixedStorageSettings.filter.CopyAllowancesFrom(filter);
                t.building.defaultStorageSettings.filter.CopyAllowancesFrom(filter);
            }
        }

        public static List<ThingDef> UniversalWeapons = new List<ThingDef>();
        public static List<ThingDef> UniversalCrops = new List<ThingDef>();
        public static UnlockManager unlocked = new UnlockManager();

        public override void WorldLoaded()
        {
            //unlocked = new UnlockManager();
            unlocked.RecacheUnlockedWeapons();
        }
    }   
}
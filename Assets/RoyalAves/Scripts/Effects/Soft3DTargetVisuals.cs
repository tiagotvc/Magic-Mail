using System.Linq;
using UnityEngine;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
namespace RoyalAves.Effects
{
 public static class Soft3DTargetVisuals
 {
  // Saved levels retain their original sprite references. Resolve only these opted-in visual families
  // to the new art in memory, retaining legacy aliases and the authored goal quantity/index.
  public static void Resolve(TargetCounter counter)
  {
   string name=counter.targetLevel.name;
   if(name!="SolidBlock"&&name!="EnchantedStone"&&name!="Inkwell"&&name!="RoutePotions"&&name!="BagOpenings"&&name!="SortingSignals")return;
   var prefab=Resources.Load<GameObject>("Blocks/"+name);
   if(prefab==null||prefab.GetComponentInChildren<Soft3DElementStyle>(true)==null)return;
   var icon=counter.targetLevel.defaultSprites.SelectMany(g=>g.sprites).FirstOrDefault(s=>s.uiSprite)?.icon;
   if(icon!=null)counter.extraObject=icon;
   counter.extraObjects=(counter.extraObjects??new Sprite[0]).Concat(prefab.GetComponentsInChildren<SpriteRenderer>(true).Select(r=>r.sprite)).Concat(icon!=null?new[]{icon}:new Sprite[0]).Where(s=>s!=null).Distinct().ToArray();
  }
 }
}

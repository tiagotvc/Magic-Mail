using UnityEngine;
namespace RoyalAves.Effects
{
 // Explicit opt-in keeps accepted cabinets, safe, tape and unrelated power-ups unchanged.
 public sealed class Soft3DElementStyle : MonoBehaviour
 {
  public static bool Has(Component source)=>source!=null&&source.GetComponent<Soft3DElementStyle>()!=null;
 }
}

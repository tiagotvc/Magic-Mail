using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using UnityEngine;
namespace RoyalAves.Gameplay
{
 public static class PotionFootprint
 {
  public static bool Has(FieldData field,int col,int row)=>SafeFootprint.Inside(field,col,row)&&field.levelSquares[row*field.maxCols+col].blocks.Any(b=>b.squareType==SquareTypes.RoutePotions);
  public static bool TryPaint(FieldData field,int col,int row,out string error)
  {
   error=null;
   for(int y=row;y<row+2;y++)for(int x=col;x<col+2;x++)
   {
    if(!SafeFootprint.Inside(field,x,y)){error="As pocoes ocupam 2x2 e nao cabem nesta borda.";return false;}
    var cell=field.levelSquares[y*field.maxCols+x];
    if(cell.block==SquareTypes.NONE||cell.blocks.Any(b=>b.squareType!=SquareTypes.EmptySquare&&b.squareType!=SquareTypes.SugarSquare)||cell.obstacle!=SquareTypes.NONE||cell.isEnterTeleport||cell.teleportCoordinatesLinked.x>=0||cell.teleportCoordinatesLinkedBack.x>=0)
    {error="As pocoes precisam de quatro celulas livres, sem buracos, teleportes ou outro obstaculo.";return false;}
   }
   for(int y=row;y<row+2;y++)for(int x=col;x<col+2;x++)
   {
    var cell=field.levelSquares[y*field.maxCols+x];
    if(cell.blocks.Count==0)cell.blocks.Add(new SquareTypeLayer{squareType=cell.block});
    // Each flask receives hits on its own cell; all four share one visual owner at runtime.
    cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.RoutePotions,originalPos=new Vector2Int(col,row),size=Vector2Int.one,anotherSquare=false});
    cell.obstacle=SquareTypes.RoutePotions;cell.obstacleLayer=1;cell.startWithoutItem=true;
   }
   field.layers=Mathf.Max(field.layers,field.levelSquares.Max(c=>c.blocks.Count));return true;
  }
  public static void Erase(FieldData field,int col,int row)
  {
   if(!Has(field,col,row))return;var origin=field.levelSquares[row*field.maxCols+col].blocks.First(b=>b.squareType==SquareTypes.RoutePotions).originalPos;
   for(int y=origin.y;y<origin.y+2;y++)for(int x=origin.x;x<origin.x+2;x++)
   {
    if(!SafeFootprint.Inside(field,x,y))continue;var cell=field.levelSquares[y*field.maxCols+x];
    cell.blocks.RemoveAll(b=>b.squareType==SquareTypes.RoutePotions&&b.originalPos==origin);if(cell.obstacle==SquareTypes.RoutePotions)cell.obstacle=SquareTypes.NONE;
    cell.startWithoutItem=false;if(cell.blocks.Count==0)cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
   }
  }
  public static bool Validate(FieldData field,out string error)
  {
   error=null;
   foreach(var cell in field.levelSquares)foreach(var layer in cell.blocks.Where(b=>b.squareType==SquareTypes.RoutePotions))
   {
    var p=layer.originalPos;
    if(cell.position.x<p.x||cell.position.x>p.x+1||cell.position.y<p.y||cell.position.y>p.y+1){error="Referencia de pocoes fora do grupo 2x2.";return false;}
    for(int y=p.y;y<p.y+2;y++)for(int x=p.x;x<p.x+2;x++)
    {
     if(!Has(field,x,y)||!field.levelSquares[y*field.maxCols+x].blocks.Any(b=>b.squareType==SquareTypes.RoutePotions&&b.originalPos==p)||
        field.levelSquares[y*field.maxCols+x].blocks.Any(b=>b.squareType!=SquareTypes.EmptySquare&&b.squareType!=SquareTypes.SugarSquare&&b.squareType!=SquareTypes.RoutePotions))
     {error="Pocoes incompletas ou sobrepostas: repinte o grupo 2x2.";return false;}
    }
   }
   return true;
  }
 }
 public static class PendingElementCounts
 {
  public static int Potions(LevelData data)=>Count(data,SquareTypes.RoutePotions);
  public static int Signals(LevelData data)=>Count(data,SquareTypes.SortingSignals);
  static int Count(LevelData data,SquareTypes type)
  {
   int count=data?.fields?.Sum(f=>f.levelSquares.Count(c=>c.blocks.Count>0?c.blocks.Any(b=>b.squareType==type):c.block==type||c.obstacle==type))??0;
#if UNITY_EDITOR
   // Preserve existing isolated labs that still override SolidBlock, without changing production crates.
   if(count==0 && UnityEditor.SessionState.GetBool(type==SquareTypes.RoutePotions?RouteCentralLaboratory.EnabledKey:SortingSignalLaboratory.EnabledKey,false))
    count=data.fields.Sum(f=>f.levelSquares.Count(c=>c.blocks.Any(b=>b.squareType==SquareTypes.SolidBlock)));
#endif
   return count;
  }
 }
}

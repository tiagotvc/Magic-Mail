using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
namespace RoyalAves.Gameplay
{
    public static class PostalTapeAuthoring
    {
        public static bool Has(SquareBlocks cell)=>cell.blocks.Any(b=>b.squareType==SquareTypes.PostalTape) || cell.obstacle==SquareTypes.PostalTape || cell.block==SquareTypes.PostalTape;
        public static bool CompatibleItem(ItemsTypes type)=>type==ItemsTypes.NONE || type==ItemsTypes.HORIZONTAL_STRIPED || type==ItemsTypes.VERTICAL_STRIPED || type==ItemsTypes.PACKAGE || type==ItemsTypes.MARMALADE || type==ItemsTypes.MULTICOLOR;
        public static bool TryPaint(SquareBlocks cell,out string error)
        {
            error=null;
            var types=cell.blocks.Count>0?cell.blocks.Select(b=>b.squareType):new[]{cell.block,cell.obstacle}.Where(t=>t!=SquareTypes.NONE);
            if(types.Any(t=>t!=SquareTypes.EmptySquare && t!=SquareTypes.SugarSquare && t!=SquareTypes.PostalTape) || cell.block==SquareTypes.NONE)
            {error="A fita fica sobre pecas, sem gelo ou outro obstaculo.";return false;}
            if(cell.item!=null && !CompatibleItem(cell.item.ItemType)){error="Use a fita sobre uma peca comum ou power-up.";return false;}
            if(cell.blocks.Count==0){cell.blocks.Add(new SquareTypeLayer{squareType=cell.block});if(cell.obstacle!=SquareTypes.NONE && cell.obstacle!=SquareTypes.PostalTape)cell.blocks.Add(new SquareTypeLayer{squareType=cell.obstacle});}
            if(!cell.blocks.Any(b=>b.squareType==SquareTypes.PostalTape))cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.PostalTape});
            cell.obstacle=SquareTypes.PostalTape;cell.obstacleLayer=1;cell.startWithoutItem=false;return true;
        }
        public static void Erase(SquareBlocks cell)
        {
            cell.blocks.RemoveAll(b=>b.squareType==SquareTypes.PostalTape);
            if(cell.obstacle==SquareTypes.PostalTape){cell.obstacle=SquareTypes.NONE;cell.obstacleLayer=1;}
            if(cell.block==SquareTypes.PostalTape){cell.block=SquareTypes.EmptySquare;cell.blockLayer=1;}
        }
    }
}
